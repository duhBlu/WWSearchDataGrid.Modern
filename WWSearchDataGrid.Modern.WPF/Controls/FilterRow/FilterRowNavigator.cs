using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Cross-cell keyboard navigation for the AutoFilterRow. Steps Tab/Shift+Tab/Left/Right
    /// between adjacent <see cref="ColumnFilterControl"/> hosts in
    /// <see cref="System.Windows.Controls.DataGridColumn.DisplayIndex"/> order (distinct from
    /// the <see cref="FilterRowPanel"/>'s column-insertion child order); Down hands off to
    /// the column's first data row; Tab off the rightmost cell hands off to the first data cell.
    /// </summary>
    /// <remarks>
    /// Tab is routed at bubble so the editor's tunneling commit-on-Tab fires first — otherwise
    /// in-flight filter text is dropped when focus leaves a non-empty editor. Up is not
    /// handled (nothing sits above the filter row); the cell template's
    /// <c>DirectionalNavigation="None"</c> prevents Up from walking into the header chrome.
    /// </remarks>
    internal static class FilterRowNavigator
    {
        /// <summary>
        /// Attempts to move filter-cell focus per <paramref name="e"/>. Returns
        /// <c>true</c> and marks the event Handled when focus moved; returns <c>false</c>
        /// otherwise so the caller can fall back to the host's base handler or to default
        /// editor behavior.
        /// </summary>
        public static bool TryNavigate(ColumnFilterControl source, KeyEventArgs e)
        {
            if (source == null || e == null || e.Handled) return false;
            if (source.SourceDataGrid == null || source.CurrentColumn == null) return false;

            // An open descendant popup owns keyboard input — skip the navigator entirely.
            if (HasOpenPopup(source)) return false;

            // Modified arrows belong to the editor (Ctrl+arrow region/segment nav, Shift+arrow
            // selection). Tab keeps Shift as its direction modifier, so it's exempt.
            if (e.Key != Key.Tab &&
                (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0)
                return false;

            // Down hands off to the column's first data row before the in-row delta lookup.
            if (e.Key == Key.Down)
            {
                if (!source.SourceDataGrid.MoveCurrentCellToFirstDataRow(source.CurrentColumn))
                    return false;
                e.Handled = true;
                return true;
            }

            int delta;
            switch (e.Key)
            {
                case Key.Tab:
                    delta = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? -1 : 1;
                    break;
                case Key.Left:
                    delta = -1;
                    break;
                case Key.Right:
                    delta = 1;
                    break;
                default:
                    return false;
            }

            var ordered = source.SourceDataGrid.GetFilterControlsInDisplayOrder();
            if (ordered.Count == 0) return false;

            int index = -1;
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ReferenceEquals(ordered[i], source)) { index = i; break; }
            }
            if (index < 0) return false;

            int target = index + delta;
            if (target < 0 || target >= ordered.Count)
            {
                // Forward Tab off the rightmost cell continues into the data rows (filter +
                // data rows form one Tab sequence). Shift+Tab off the leftmost cell falls
                // through to WPF's default and escapes the grid. Left/Right at row edges
                // also fall through, which is a no-op given DirectionalNavigation="None".
                if (e.Key == Key.Tab && delta > 0)
                {
                    if (!source.SourceDataGrid.MoveCurrentCellToFirstDataCell()) return false;
                    e.Handled = true;
                    return true;
                }
                return false;
            }
            if (!ordered[target].Focus()) return false;

            e.Handled = true;
            return true;
        }

        /// <summary>
        /// Detects any open popup the cell owns. Visual-tree walk catches template-hosted
        /// popups (SearchTypeSelector, in-editor ComboBox/DatePicker); the rule-filter popup
        /// has no visual parent and is exposed via <see cref="ColumnFilterControl.HasOpenSubPopup"/>.
        /// </summary>
        private static bool HasOpenPopup(ColumnFilterControl source)
        {
            if (source.HasOpenSubPopup) return true;
            return HasOpenPopupInVisualTree(source);
        }

        private static bool HasOpenPopupInVisualTree(DependencyObject root)
        {
            if (root == null) return false;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is Popup p && p.IsOpen) return true;
                if (HasOpenPopupInVisualTree(child)) return true;
            }
            return false;
        }
    }
}
