using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace WWControls.Wpf
{
    /// <summary>
    /// Tree-style keyboard navigation for the flattened grouping engine. Group-header sentinels are
    /// focusable, focus-highlighted rows that participate in vertical arrow navigation alongside
    /// data rows, and use TreeView-like horizontal semantics:
    /// <list type="bullet">
    ///   <item><description><c>Right</c> on a collapsed header expands it; on an expanded header it
    ///     steps into the first child row.</description></item>
    ///   <item><description><c>Left</c> on an expanded header collapses it; on an already-collapsed
    ///     header it jumps focus up to the parent group header.</description></item>
    ///   <item><description><c>Up</c>/<c>Down</c> move focus to the previous/next row (header or
    ///     data); <c>Space</c>/<c>Enter</c> toggle.</description></item>
    /// </list>
    /// Headers are focus-only — they never enter the selection (see
    /// <see cref="ScrubHeaderSelection"/>); the highlight is keyboard focus.
    /// </summary>
    public partial class SearchDataGrid
    {
        /// <summary>
        /// Column to re-focus when stepping back into a data row after passing through one or more
        /// header rows, so vertical navigation keeps the user in the same column.
        /// </summary>
        private DataGridColumn _navPreferredColumn;

        /// <summary>
        /// Keyboard handler for a focused group-header row (called from
        /// <see cref="SearchDataGridRow.OnKeyDown"/>). Returns <c>true</c> when the key was consumed.
        /// </summary>
        internal bool HandleHeaderNavigationKey(SearchDataGridRow headerRow, Key key)
        {
            if (!_groupingActive || headerRow?.GroupHeader == null) return false;

            var header = headerRow.GroupHeader;
            int index = Items.IndexOf(header);
            if (index < 0) return false;

            switch (key)
            {
                case Key.Up:
                    return MoveFocusToRow(index - 1);

                case Key.Down:
                    return MoveFocusToRow(index + 1);

                case Key.Right:
                    if (!header.IsExpanded)
                    {
                        ToggleGroup(header);          // expand
                        RefocusHeader(header.Node?.PathKey, index);
                    }
                    else
                    {
                        MoveFocusToRow(index + 1);     // into first child
                    }
                    return true;

                case Key.Left:
                    if (header.IsExpanded)
                    {
                        ToggleGroup(header);          // collapse
                        RefocusHeader(header.Node?.PathKey, index);
                    }
                    else
                    {
                        // Already collapsed: jump to the parent group header, if any.
                        int parent = FindParentHeaderIndex(index, header.Level);
                        if (parent >= 0) MoveFocusToRow(parent);
                    }
                    return true;

                case Key.Space:
                case Key.Enter:
                    ToggleGroup(header);
                    RefocusHeader(header.Node?.PathKey, index);
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Crossing handler for a focused data cell: when an unmodified <c>Up</c>/<c>Down</c> would
        /// step onto a header row, move focus to that header instead of stalling on the cell-less
        /// sentinel. Returns <c>true</c> when it took over; <c>false</c> lets the DataGrid's native
        /// data→data navigation run. Called from <see cref="OnGridPreviewKeyDown"/>.
        /// </summary>
        internal bool TryHandleDataCellHeaderCrossing(DataGridCell cell, bool down)
        {
            if (!_groupingActive || cell?.DataContext == null) return false;

            int index = Items.IndexOf(cell.DataContext);
            if (index < 0) return false;

            int neighbor = down ? index + 1 : index - 1;
            if (neighbor < 0 || neighbor >= Items.Count) return false;
            if (!(Items[neighbor] is GroupHeaderRow)) return false; // data→data: let native handle

            // Remember the column so returning to data lands in the same place.
            _navPreferredColumn = cell.Column;
            return MoveFocusToRow(neighbor);
        }

        /// <summary>
        /// Moves keyboard focus to the flat row at <paramref name="index"/>: a header row gets row
        /// focus (and clears any data selection so the highlight is the header alone); a data row
        /// gets cell focus in the preferred column and becomes the sole selection. Returns
        /// <c>false</c> when the index is out of range.
        /// </summary>
        private bool MoveFocusToRow(int index)
        {
            if (index < 0 || index >= Items.Count) return false;

            object item = Items[index];
            ScrollIntoView(item);

            // Container may need a layout pass after ScrollIntoView (virtualization); defer focus.
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new System.Action(() =>
            {
                if (ItemContainerGenerator.ContainerFromItem(item) is not DataGridRow container) return;

                if (item is GroupHeaderRow)
                {
                    UnselectAll();          // header focus is not a selection
                    container.Focus();
                }
                else
                {
                    var column = ResolvePreferredColumn();
                    CurrentCell = new DataGridCellInfo(item, column);
                    SelectSingleDataRow(item, column);
                    GetCellForColumn(container, column)?.Focus();
                }
            }));

            return true;
        }

        /// <summary>Re-focuses a header after an expand/collapse reflatten rebuilt the row list.</summary>
        private void RefocusHeader(string pathKey, int fallbackIndex)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Input, new System.Action(() =>
            {
                int index = fallbackIndex;
                if (!string.IsNullOrEmpty(pathKey))
                {
                    for (int i = 0; i < Items.Count; i++)
                    {
                        if (Items[i] is GroupHeaderRow h && h.Node?.PathKey == pathKey) { index = i; break; }
                    }
                }
                if (index < 0 || index >= Items.Count) return;
                ScrollIntoView(Items[index]);
                if (ItemContainerGenerator.ContainerFromItem(Items[index]) is DataGridRow container)
                    container.Focus();
            }));
        }

        /// <summary>
        /// Index of the nearest preceding header whose <see cref="GroupHeaderRow.Level"/> is shallower
        /// than <paramref name="level"/> — the parent group of the header at <paramref name="fromIndex"/>.
        /// Returns -1 for a top-level header.
        /// </summary>
        private int FindParentHeaderIndex(int fromIndex, int level)
        {
            for (int i = fromIndex - 1; i >= 0; i--)
            {
                if (Items[i] is GroupHeaderRow h && h.Level < level) return i;
            }
            return -1;
        }

        /// <summary>Preferred data column for vertical nav, falling back to the first visible column.</summary>
        private DataGridColumn ResolvePreferredColumn()
        {
            if (_navPreferredColumn != null && _navPreferredColumn.Visibility == System.Windows.Visibility.Visible)
                return _navPreferredColumn;

            return Columns
                .Where(c => c.Visibility == System.Windows.Visibility.Visible)
                .OrderBy(c => c.DisplayIndex)
                .FirstOrDefault();
        }

        /// <summary>Makes <paramref name="item"/> the sole selection for plain-arrow navigation.</summary>
        private void SelectSingleDataRow(object item, DataGridColumn column)
        {
            if (SelectionUnit == DataGridSelectionUnit.FullRow)
            {
                SelectedItem = item;
            }
            else
            {
                UnselectAllCells();
                if (column != null)
                {
                    try { SelectedCells.Add(new DataGridCellInfo(item, column)); }
                    catch (System.NotSupportedException) { }
                }
            }
        }

        private static DataGridCell GetCellForColumn(DataGridRow row, DataGridColumn column)
        {
            if (row == null || column == null) return null;
            foreach (var cell in VisualTreeHelperMethods.FindVisualDescendants<DataGridCell>(row))
            {
                if (cell.Column == column) return cell;
            }
            return null;
        }
    }
}
