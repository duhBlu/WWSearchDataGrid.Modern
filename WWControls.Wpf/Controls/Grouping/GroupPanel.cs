using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf
{
    /// <summary>
    /// The strip above the column headers that hosts one pill per grouped column. Items are the
    /// <see cref="GridColumn"/> descriptors in the owning grid's
    /// <see cref="SearchDataGrid.GroupedColumns"/> collection, ordered by
    /// <see cref="GridColumn.GroupLevel"/>. The pill template (Click → toggle sort, right-click
    /// → per-column menu, drag target for column-header drops) is supplied by the theme.
    /// </summary>
    /// <remarks>
    /// Visual layout is a stair-step: pills stack vertically (one per row), and each pill
    /// indents by <see cref="SearchDataGrid.GroupIndentWidth"/> × <see cref="GridColumn.GroupLevel"/>
    /// so a nested column visibly nests beneath its parent. The panel is a drop target — see
    /// <see cref="OnDrop"/> — for column-header drags that should add a column to the grouping.
    /// </remarks>
    public class GroupPanel : ItemsControl
    {
        static GroupPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(GroupPanel),
                new FrameworkPropertyMetadata(typeof(GroupPanel)));
        }

        public GroupPanel()
        {
            // Drag-from-header source raises drop on this panel; see SearchDataGridColumnHeader's
            // drag initiation. AllowDrop is set in the theme but mirrored here so a consumer
            // applying a custom Style without copying the AllowDrop setter still receives drops.
            AllowDrop = true;
            Loaded += (_, _) => SetValue(OwnerGridPropertyKey, VisualTreeHelperMethods.FindVisualAncestor<SearchDataGrid>(this));
        }

        private static readonly DependencyPropertyKey OwnerGridPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(OwnerGrid),
                typeof(SearchDataGrid),
                typeof(GroupPanel),
                new PropertyMetadata(null));

        /// <summary>Identifies the read-only <see cref="OwnerGrid"/> dependency property.</summary>
        public static readonly DependencyProperty OwnerGridProperty = OwnerGridPropertyKey.DependencyProperty;

        /// <summary>
        /// The owning <see cref="SearchDataGrid"/>, resolved via visual-tree ancestor walk when
        /// this panel is loaded. Exposed for the panel context menu so menu items can bind
        /// <c>{Binding PlacementTarget.OwnerGrid, …}</c> instead of writing a converter that walks
        /// up from <c>PlacementTarget</c>.
        /// </summary>
        public SearchDataGrid OwnerGrid => (SearchDataGrid)GetValue(OwnerGridProperty);

        /// <summary>
        /// Mime/data-format key used for the column-header → group-panel drag payload. The payload
        /// itself is the dragged <see cref="GridColumn"/> descriptor.
        /// </summary>
        public const string DragDataFormat = "WWControls.GroupPanel.ColumnDescriptor";

        /// <inheritdoc/>
        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            HandleDragOver(e);
        }

        /// <inheritdoc/>
        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            HandleDragOver(e);
        }

        private static void HandleDragOver(DragEventArgs e)
        {
            // Accept only drags that carry our descriptor payload. WPF's built-in column-reorder
            // drag uses its own internal data format and is invisible to this handler — those
            // drags bubble through without a Move effect and the standard DataGrid reorder
            // behavior continues unaffected.
            if (e.Data?.GetDataPresent(DragDataFormat) == true && e.Data.GetData(DragDataFormat) is GridColumn col && col.ActualAllowGrouping)
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        /// <inheritdoc/>
        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            if (e.Data?.GetData(DragDataFormat) is not GridColumn column) return;
            if (column.View == null) return;
            column.View.GroupBy(column);
            e.Handled = true;
        }
    }
}
