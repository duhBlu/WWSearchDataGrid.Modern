using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf
{
    public partial class SearchDataGrid
    {
        #region Column State Observation

        // Per-descriptor cleanup delegates for ActualWidth / DisplayIndex / Visibility
        // value-changed subscriptions on the generated DataGridColumn. Kept on the grid so
        // ColumnLayoutBase stays free of WPF DataGridColumn knowledge.
        private readonly Dictionary<GridColumn, Action> _columnStateHooksByDescriptor = new();
        private bool _columnsCollectionHooked;

        /// <summary>
        /// Subscribes to layout-affecting properties on the generated <see cref="DataGridColumn"/>
        /// — <see cref="DataGridColumn.ActualWidth"/>, <see cref="DataGridColumn.DisplayIndex"/>,
        /// <see cref="UIElement.Visibility"/> — and pushes resolved values into the descriptor's
        /// read-only <c>Actual*</c> DPs. Lazily hooks <see cref="ItemsControl.Columns"/>'s
        /// <see cref="INotifyCollectionChanged"/> on first call so add/remove of any column
        /// triggers a position recompute.
        /// </summary>
        internal void HookColumnStateObservation(GridColumn descriptor)
        {
            if (descriptor?.InternalColumn == null) return;
            if (_columnStateHooksByDescriptor.ContainsKey(descriptor)) return;

            DataGridColumn col = descriptor.InternalColumn;

            DependencyPropertyDescriptor widthDpd =
                DependencyPropertyDescriptor.FromProperty(DataGridColumn.ActualWidthProperty, typeof(DataGridColumn));
            DependencyPropertyDescriptor displayIdxDpd =
                DependencyPropertyDescriptor.FromProperty(DataGridColumn.DisplayIndexProperty, typeof(DataGridColumn));
            DependencyPropertyDescriptor visDpd =
                DependencyPropertyDescriptor.FromProperty(DataGridColumn.VisibilityProperty, typeof(DataGridColumn));

            EventHandler widthHandler = (s, e) =>
            {
                descriptor.SetActualWidth(col.ActualWidth);
                descriptor.SetActualHeaderWidth(col.ActualWidth);
            };
            EventHandler positionHandler = (s, e) => RefreshAllColumnPositions();

            widthDpd?.AddValueChanged(col, widthHandler);
            displayIdxDpd?.AddValueChanged(col, positionHandler);
            visDpd?.AddValueChanged(col, positionHandler);

            _columnStateHooksByDescriptor[descriptor] = () =>
            {
                widthDpd?.RemoveValueChanged(col, widthHandler);
                displayIdxDpd?.RemoveValueChanged(col, positionHandler);
                visDpd?.RemoveValueChanged(col, positionHandler);
            };

            // Seed the current value so bindings see the resolved width immediately rather
            // than after the next layout pass.
            descriptor.SetActualWidth(col.ActualWidth);
            descriptor.SetActualHeaderWidth(col.ActualWidth);

            HookColumnsCollectionOnce();
            RefreshAllColumnPositions();
        }

        /// <summary>
        /// Releases the layout-property subscriptions for <paramref name="descriptor"/> and
        /// resets its <c>Actual*</c> DPs to detached defaults.
        /// </summary>
        internal void UnhookColumnStateObservation(GridColumn descriptor)
        {
            if (descriptor == null) return;
            if (_columnStateHooksByDescriptor.TryGetValue(descriptor, out var cleanup))
            {
                cleanup();
                _columnStateHooksByDescriptor.Remove(descriptor);
                descriptor.SetActualWidth(0);
                descriptor.SetActualHeaderWidth(0);
                descriptor.SetActualVisibleIndex(-1);
                descriptor.SetActualCollectionIndex(-1);
                descriptor.SetColumnPosition(ColumnPositionKind.None);
            }
        }

        private void HookColumnsCollectionOnce()
        {
            if (_columnsCollectionHooked) return;
            // DataGrid.Columns is ObservableCollection<DataGridColumn>; cast through INCC so we
            // don't take an implicit dependency on the concrete collection type.
            ((INotifyCollectionChanged)Columns).CollectionChanged += (s, e) => RefreshAllColumnPositions();
            _columnsCollectionHooked = true;
        }

        /// <summary>
        /// Walks <see cref="ItemsControl.Columns"/> and pushes resolved values to each tracked
        /// descriptor: <see cref="ColumnDataBase.ActualCollectionIndex"/> (insertion order),
        /// <see cref="ColumnDataBase.ActualVisibleIndex"/> (rank among visible columns ordered by
        /// <see cref="DataGridColumn.DisplayIndex"/>), and <see cref="ColumnDataBase.ColumnPosition"/>
        /// (First/Middle/Last/Single derived from the visible rank). Columns with no descriptor
        /// (manually added <see cref="DataGridColumn"/>s) are skipped.
        /// </summary>
        private void RefreshAllColumnPositions()
        {
            // Insertion-order index (independent of visibility / user reordering).
            for (int i = 0; i < Columns.Count; i++)
            {
                var col = Columns[i];
                var descriptor = FindGridColumnDescriptor(col);
                descriptor?.SetActualCollectionIndex(i);
            }

            // Pre-reset visible state — columns we don't visit below stay defaulted.
            foreach (var col in Columns)
            {
                var descriptor = FindGridColumnDescriptor(col);
                if (descriptor == null) continue;
                descriptor.SetActualVisibleIndex(-1);
                descriptor.SetColumnPosition(ColumnPositionKind.None);
            }

            // Display-ordered visible columns drive ActualVisibleIndex and ColumnPosition.
            var visible = Columns
                .Where(c => c.Visibility == Visibility.Visible)
                .OrderBy(c => c.DisplayIndex)
                .ToList();
            int count = visible.Count;

            for (int i = 0; i < count; i++)
            {
                var descriptor = FindGridColumnDescriptor(visible[i]);
                if (descriptor == null) continue;
                descriptor.SetActualVisibleIndex(i);

                ColumnPositionKind pos;
                if (count == 1) pos = ColumnPositionKind.Single;
                else if (i == 0) pos = ColumnPositionKind.First;
                else if (i == count - 1) pos = ColumnPositionKind.Last;
                else pos = ColumnPositionKind.Middle;

                descriptor.SetColumnPosition(pos);
            }

            // Aligned group-summary layers lay their cells out by display order + visibility —
            // rebuild every realized header's cells so they track the change (widths track via
            // binding). Group footer rows lay out the same way.
            InvalidateGroupSummaryPresenters();
            InvalidateGroupFooterPresenters();
        }

        #endregion
    }
}
