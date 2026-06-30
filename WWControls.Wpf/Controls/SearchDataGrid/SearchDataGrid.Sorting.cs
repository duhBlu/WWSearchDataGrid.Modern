using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace WWControls.Wpf
{
    public partial class SearchDataGrid
    {
        #region Sort Direction Enforcement

        /// <summary>
        /// Intercepts header-click sorting to gate the next direction by
        /// <see cref="ColumnDataBase.AllowedSortOrders"/> and seed the first click from
        /// <see cref="ColumnDataBase.DefaultSortOrder"/>. Columns that aren't backed by a
        /// <see cref="GridColumn"/> descriptor (e.g. manually added <see cref="DataGridColumn"/>
        /// instances) fall through to the WPF default behavior.
        /// </summary>
        protected override void OnSorting(DataGridSortingEventArgs eventArgs)
        {
            DataGridColumn col = eventArgs?.Column;
            GridColumn descriptor = col == null ? null : FindGridColumnDescriptor(col);
            if (descriptor == null)
            {
                base.OnSorting(eventArgs);
                return;
            }

            AllowedSortOrders allowed = descriptor.AllowedSortOrders;
            bool ascAllowed = (allowed & AllowedSortOrders.Ascending) != 0;
            bool descAllowed = (allowed & AllowedSortOrders.Descending) != 0;

            // AllowedSortOrders=None means the column is unsortable even though AllowSorting
            // may still be true. CanUserSort already reflects this through ActualAllowSorting,
            // so the header click usually wouldn't even reach OnSorting — guard anyway in case
            // a consumer drives the event directly.
            if (!ascAllowed && !descAllowed)
            {
                eventArgs.Handled = true;
                return;
            }

            ListSortDirection? next = ComputeNextSortDirection(col.SortDirection, descriptor.DefaultSortOrder, ascAllowed, descAllowed);
            bool multiColumn = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            ApplyColumnSort(col, next, multiColumn);
            eventArgs.Handled = true;
        }

        /// <summary>
        /// Cycle resolution for a header click. When the column is unsorted, seeds from
        /// <paramref name="defaultOrder"/> if that direction is allowed; otherwise falls back to
        /// the first allowed direction. When already sorted, advances to the other direction if
        /// allowed, else clears the column's sort.
        /// </summary>
        private static ListSortDirection? ComputeNextSortDirection(
            ListSortDirection? current,
            ColumnSortOrder defaultOrder,
            bool ascAllowed,
            bool descAllowed)
        {
            switch (current)
            {
                case null:
                    if (defaultOrder == ColumnSortOrder.Descending && descAllowed)
                        return ListSortDirection.Descending;
                    if (defaultOrder == ColumnSortOrder.Ascending && ascAllowed)
                        return ListSortDirection.Ascending;
                    return ascAllowed ? ListSortDirection.Ascending : ListSortDirection.Descending;

                case ListSortDirection.Ascending:
                    return descAllowed ? ListSortDirection.Descending : (ListSortDirection?)null;

                case ListSortDirection.Descending:
                    return ascAllowed ? ListSortDirection.Ascending : (ListSortDirection?)null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Applies <paramref name="direction"/> to <paramref name="col"/>, mutating
        /// <see cref="ItemCollection.SortDescriptions"/> and the column's <c>SortDirection</c>.
        /// <paramref name="multiColumn"/>=<c>true</c> appends/updates this column's entry,
        /// preserving other columns' sort state (Ctrl+click behavior). When false, clears
        /// existing descriptions first so this is the sole sort key. <paramref name="direction"/>
        /// =<c>null</c> removes the column from the sort.
        /// </summary>
        private void ApplyColumnSort(DataGridColumn col, ListSortDirection? direction, bool multiColumn)
        {
            // When grouped, order is shaped upstream of the displayed projection; route the header
            // click there instead of mutating the projection's SortDescriptions.
            if (_groupingActive) { ApplyGroupedColumnSort(col, direction, multiColumn); return; }

            // Ungrouped: sort the displayed view directly via its SortDescriptions.
            SortDescriptionCollection descriptions = Items?.SortDescriptions;
            if (descriptions == null) return;

            string path = col.SortMemberPath;
            if (string.IsNullOrEmpty(path)) return;

            if (!multiColumn)
            {
                descriptions.Clear();
                foreach (var other in Columns)
                {
                    if (other == col) continue;
                    other.SortDirection = null;
                }
            }
            else
            {
                // Drop any existing entry for this column's path so a re-click doesn't duplicate it.
                for (int i = descriptions.Count - 1; i >= 0; i--)
                {
                    if (descriptions[i].PropertyName == path)
                        descriptions.RemoveAt(i);
                }
            }

            if (direction == null)
            {
                col.SortDirection = null;
            }
            else
            {
                descriptions.Add(new SortDescription(path, direction.Value));
                col.SortDirection = direction.Value;
            }
        }

        #endregion

        #region Sort State Observation

        // Maps each managed descriptor to a cleanup delegate that unsubscribes the
        // SortDirection value-changed handler on its generated DataGridColumn. Stored on the
        // grid (not the descriptor) so the grid owns subscription lifetime and the descriptor
        // tier stays free of WPF DataGridColumn knowledge.
        private readonly Dictionary<GridColumn, Action> _sortHooksByDescriptor = new();
        private bool _sortDescriptionsHooked;
        private NotifyCollectionChangedEventHandler _sortDescriptionsHandler;
        private SortDescriptionCollection _hookedSortDescriptions;

        /// <summary>
        /// Subscribes to the generated <see cref="DataGridColumn.SortDirection"/> on
        /// <paramref name="descriptor"/> and mirrors the value into
        /// <see cref="ColumnDataBase.SortOrder"/>. Idempotent — subsequent calls for the same
        /// descriptor are no-ops while the existing hook is live. Also lazily hooks the
        /// grid-level <see cref="ItemCollection.SortDescriptions"/> so
        /// <see cref="ColumnDataBase.SortIndex"/> can be derived.
        /// </summary>
        internal void HookSortObservation(GridColumn descriptor)
        {
            if (descriptor?.InternalColumn == null) return;
            if (_sortHooksByDescriptor.ContainsKey(descriptor)) return;

            DataGridColumn col = descriptor.InternalColumn;
            DependencyPropertyDescriptor dpd =
                DependencyPropertyDescriptor.FromProperty(DataGridColumn.SortDirectionProperty, typeof(DataGridColumn));
            if (dpd == null) return;

            EventHandler handler = (s, e) =>
            {
                // Mirror the generated column's SortDirection onto the descriptor's SortOrder (the
                // property pill / header styles bind to). For grouped columns the projection sets
                // SortDirection directly (ApplyGroupSortDirections), so this just keeps SortOrder
                // in sync — there is no CollectionView resync to defend against.
                descriptor.SetSortOrder(MapSortDirection(col.SortDirection));
                RefreshAllSortIndices();
            };
            dpd.AddValueChanged(col, handler);
            _sortHooksByDescriptor[descriptor] = () => dpd.RemoveValueChanged(col, handler);

            // Seed the initial value so styles bound to SortOrder render correctly even when
            // a descriptor is attached after a sort is already applied (e.g. programmatic sort
            // followed by a runtime column add).
            descriptor.SetSortOrder(MapSortDirection(col.SortDirection));

            HookSortDescriptionsOnce();
            RefreshAllSortIndices();
        }

        /// <summary>
        /// Releases the SortDirection value-changed subscription for <paramref name="descriptor"/>.
        /// Safe to call when no hook is registered.
        /// </summary>
        internal void UnhookSortObservation(GridColumn descriptor)
        {
            if (descriptor == null) return;
            if (_sortHooksByDescriptor.TryGetValue(descriptor, out var cleanup))
            {
                cleanup();
                _sortHooksByDescriptor.Remove(descriptor);
                descriptor.SetSortOrder(ColumnSortOrder.None);
                descriptor.SetSortIndex(-1);
            }
        }

        private void HookSortDescriptionsOnce()
        {
            if (_sortDescriptionsHooked) return;
            var descriptions = Items?.SortDescriptions;
            if (descriptions == null) return;

            _sortDescriptionsHandler = (s, e) => RefreshAllSortIndices();
            ((INotifyCollectionChanged)descriptions).CollectionChanged += _sortDescriptionsHandler;
            _hookedSortDescriptions = descriptions;
            _sortDescriptionsHooked = true;
        }

        private void RefreshAllSortIndices()
        {
            // Build a path → ordinal lookup once per pass so each descriptor lookup is O(1).
            var pathToIndex = new Dictionary<string, int>(StringComparer.Ordinal);

            if (_groupingActive)
            {
                // Grouped mode keeps no sort on the displayed view: the effective order is the group
                // paths (in level order) followed by the user sorts. Mirror that ordinal sequence.
                int idx = 0;
                foreach (var grouped in _groupColumns)
                {
                    string path = grouped?.ResolveGroupPath();
                    if (!string.IsNullOrEmpty(path) && !pathToIndex.ContainsKey(path))
                        pathToIndex[path] = idx++;
                }
                foreach (var sort in _withinGroupSorts)
                {
                    if (!string.IsNullOrEmpty(sort.PropertyName) && !pathToIndex.ContainsKey(sort.PropertyName))
                        pathToIndex[sort.PropertyName] = idx++;
                }
            }
            else
            {
                var descriptions = Items?.SortDescriptions;
                if (descriptions == null) return;
                for (int i = 0; i < descriptions.Count; i++)
                {
                    string name = descriptions[i].PropertyName;
                    if (!string.IsNullOrEmpty(name) && !pathToIndex.ContainsKey(name))
                        pathToIndex[name] = i;
                }
            }

            foreach (var descriptor in _sortHooksByDescriptor.Keys)
            {
                string path = descriptor.SortMemberPath ?? descriptor.FieldName;
                if (!string.IsNullOrEmpty(path) && pathToIndex.TryGetValue(path, out int idx))
                    descriptor.SetSortIndex(idx);
                else
                    descriptor.SetSortIndex(-1);
            }
        }

        private static ColumnSortOrder MapSortDirection(ListSortDirection? dir) => dir switch
        {
            ListSortDirection.Ascending => ColumnSortOrder.Ascending,
            ListSortDirection.Descending => ColumnSortOrder.Descending,
            _ => ColumnSortOrder.None,
        };

        #endregion
    }
}
