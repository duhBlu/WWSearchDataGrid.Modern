using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Cross-cell keyboard navigation for the AutoFilterRow. Maps Tab / Shift+Tab and
    /// arrow Left / Right / Down onto adjacent <see cref="ColumnFilterControl"/> hosts in
    /// <see cref="System.Windows.Controls.DataGridColumn.DisplayIndex"/> order (which is
    /// independent from the <see cref="FilterRowPanel"/>'s column-insertion child order),
    /// and onto the data area at boundaries: Down hands off to the same column's first
    /// data row, Tab off the rightmost cell hands off to the first cell of the first
    /// data row.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Tab is routed at the bubbling stage by the host so the editor's tunneling
    /// <c>PreviewKeyDown</c> commit-on-Tab path runs first (see
    /// <c>OnSearchTextBoxPreviewKeyDown</c> / <c>OnFilterEditorPreviewKeyDown</c> in
    /// <c>ColumnFilterControlTextFilter.cs</c>) — without that ordering, the in-flight
    /// filter text would be dropped when focus leaves a non-empty editor.
    /// </para>
    /// <para>
    /// Up arrow is intentionally not handled: the filter row is the top band of the grid,
    /// so there is nothing above it to step to. The cell-template
    /// <c>KeyboardNavigation.DirectionalNavigation="None"</c> setting prevents Up from
    /// accidentally walking out of the cell into the column-header chrome.
    /// </para>
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

            // A descendant popup owns keyboard input while it's open (SearchTypeSelector
            // dropdown, rule-filter editor popup, or any in-editor popup such as a
            // ComboBox dropdown). Skip the navigator entirely so the popup's own
            // navigation works as expected.
            if (HasOpenPopup(source)) return false;

            // Modified arrows belong to the editor: Ctrl+Up/Down spins the active segment
            // in SegmentedDateTimeEditor, Ctrl+Left/Right does region nav / word-jump,
            // Shift+Arrow extends selection. Tab keeps Shift as its direction modifier
            // (Shift+Tab steps backward), so it's exempt.
            if (e.Key != Key.Tab &&
                (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0)
                return false;

            // Down hands off to the same column's first data row regardless of cell index;
            // handled before the in-row delta lookup.
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
                // Forward Tab off the rightmost filter cell hands off to the first data
                // cell (decision locked with user: filter row + data rows form one
                // continuous Tab sequence). Shift+Tab off the leftmost cell escapes the
                // grid via WPF's default Tab traversal — nothing in the data area sits
                // "before" the filter row in user mental order. Left / Right at the row
                // edges also fall through to default, which combined with
                // DirectionalNavigation="None" on the cell template is a no-op.
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
        /// Walks <paramref name="root"/>'s visual subtree for any open <see cref="Popup"/>.
        /// Catches the SearchTypeSelector dropdown (in the cell template) and any ComboBox /
        /// DatePicker calendar popup inside a column's editor. The standalone rule-filter
        /// popup is not in the visual tree (it's created via <c>new Popup()</c> with no
        /// parent in <c>ShowFilterPopup</c>), so the host exposes
        /// <see cref="ColumnFilterControl.HasOpenSubPopup"/> separately — checked here too.
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
