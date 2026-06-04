using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Cross-cell keyboard navigation for the FilterRow. Steps Tab/Shift+Tab/Left/Right
    /// between adjacent <see cref="ColumnFilterControl"/> hosts; Down hands off to the
    /// column's first data row; Tab off the trailing cell hands off to the first data cell.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Tab / Shift+Tab walk by <c>SearchDataGrid.GetColumnsInTabOrder</c> — honors
    /// <c>GridColumn.NavigationIndex</c> (custom Tab order, lower first) and skips cells
    /// whose descriptor has <c>ActualTabStop</c>=<c>false</c> or
    /// <c>ActualAllowFocus</c>=<c>false</c>.</item>
    /// <item>Left / Right walk by <c>DataGridColumn.DisplayIndex</c> (visual order) and
    /// skip only cells whose descriptor has <c>ActualAllowFocus</c>=<c>false</c>.
    /// <c>TabStop</c>=<c>false</c> alone keeps the cell reachable by arrow.</item>
    /// </list>
    /// <para>
    /// Tab is routed at bubble so the editor's tunneling commit-on-Tab fires first — otherwise
    /// in-flight filter text is dropped when focus leaves a non-empty editor. Up is not
    /// handled (nothing sits above the filter row); the cell template's
    /// <c>DirectionalNavigation="None"</c> prevents Up from walking into the header chrome.
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
            bool isTab;
            switch (e.Key)
            {
                case Key.Tab:
                    delta = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? -1 : 1;
                    isTab = true;
                    break;
                case Key.Left:
                    delta = -1;
                    isTab = false;
                    break;
                case Key.Right:
                    delta = 1;
                    isTab = false;
                    break;
                default:
                    return false;
            }

            var ordered = isTab
                ? GetFilterControlsInTabOrder(source.SourceDataGrid)
                : GetFilterControlsForArrowNav(source.SourceDataGrid);
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
                // Forward Tab off the trailing cell continues into the data rows (filter +
                // data rows form one Tab sequence). Shift+Tab off the leading cell falls
                // through to WPF's default and escapes the grid. Left/Right at row edges
                // also fall through, which is a no-op given DirectionalNavigation="None".
                if (isTab && delta > 0)
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
        /// Filter controls in Tab order — mirrors <c>SearchDataGrid.GetColumnsInTabOrder</c>,
        /// honoring NavigationIndex and skipping descriptors with ActualTabStop=false or
        /// ActualAllowFocus=false. Stale / unloaded filter controls are filtered out the
        /// same way <c>GetFilterControlsInDisplayOrder</c> does.
        /// </summary>
        private static IReadOnlyList<ColumnFilterControl> GetFilterControlsInTabOrder(SearchDataGrid grid)
        {
            var tabOrder = grid.GetColumnsInTabOrder();
            if (tabOrder.Columns == null || tabOrder.Columns.Count == 0)
                return System.Array.Empty<ColumnFilterControl>();

            var result = new List<ColumnFilterControl>(tabOrder.Columns.Count);
            foreach (var column in tabOrder.Columns)
            {
                if (grid.FindColumnFilterControl(column) is not ColumnFilterControl ctl) continue;
                if (!ctl.IsLoaded) continue;
                if (PresentationSource.FromVisual(ctl) == null) continue;
                result.Add(ctl);
            }
            return result;
        }

        /// <summary>
        /// Filter controls for arrow navigation — DisplayIndex order, skipping descriptors
        /// with ActualAllowFocus=false (but keeping those with ActualTabStop=false, which
        /// only blocks Tab traversal).
        /// </summary>
        private static IReadOnlyList<ColumnFilterControl> GetFilterControlsForArrowNav(SearchDataGrid grid)
        {
            var raw = grid.GetFilterControlsInDisplayOrder();
            if (raw.Count == 0) return raw;

            // Fast path: nothing has opted out → use the existing list unchanged.
            bool anyOptOut = false;
            foreach (var ctl in raw)
            {
                var descriptor = grid.FindGridColumnDescriptor(ctl.CurrentColumn);
                if (descriptor != null && !descriptor.ActualAllowFocus)
                {
                    anyOptOut = true;
                    break;
                }
            }
            if (!anyOptOut) return raw;

            return raw
                .Where(ctl =>
                {
                    var descriptor = grid.FindGridColumnDescriptor(ctl.CurrentColumn);
                    return descriptor?.ActualAllowFocus ?? true;
                })
                .ToList();
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
