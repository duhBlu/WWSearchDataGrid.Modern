using System.Windows;
using System.Windows.Markup;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// A non-rendering descriptor that groups a run of columns — or nested bands — under a shared
    /// caption row above the column headers. Declared inside <c>SearchDataGrid.Bands</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A band's <see cref="Children"/> may hold <see cref="GridColumn"/> leaves and/or nested
    /// <see cref="GridColumnBand"/>s; the nesting depth becomes the number of stacked caption rows
    /// rendered above the real column headers (one row per level — this is not DevExpress's
    /// multi-row-within-a-band model).
    /// </para>
    /// <para>
    /// <see cref="GridColumnBand"/> only describes header chrome. At load the owning
    /// <see cref="SearchDataGrid"/> flattens the band tree into
    /// <see cref="SearchDataGrid.GridColumns"/> (depth-first, in declaration order) so the normal
    /// column-generation, layout, filtering, and sorting pipeline runs unchanged.
    /// </para>
    /// </remarks>
    [ContentProperty(nameof(Children))]
    public class GridColumnBand : ColumnDescriptorElement
    {
        public GridColumnBand()
        {
            // Seed the collection at construction so XAML content syntax
            // (`<sdg:GridColumnBand><sdg:GridColumn .../></sdg:GridColumnBand>`) can add entries —
            // the XAML reader calls GetValue to find the target list and fails when it's null
            // (same convention as SearchDataGrid.GridColumns / GridColumn.TotalSummaries).
            SetValue(ChildrenPropertyKey, new FreezableCollection<ColumnDescriptorElement>());
        }

        /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(object), typeof(GridColumnBand),
                new PropertyMetadata(null));

        /// <summary>Caption shown in the band's header cell.</summary>
        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>Identifies the <see cref="HeaderTemplate"/> dependency property.</summary>
        public static readonly DependencyProperty HeaderTemplateProperty =
            DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(GridColumnBand),
                new PropertyMetadata(null));

        /// <summary>
        /// Optional template for <see cref="Header"/>. When null the caption renders as text via
        /// the band header cell's default style.
        /// </summary>
        public DataTemplate HeaderTemplate
        {
            get => (DataTemplate)GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        /// <summary>Read-only key for <see cref="Children"/>.</summary>
        private static readonly DependencyPropertyKey ChildrenPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(Children),
                typeof(FreezableCollection<ColumnDescriptorElement>),
                typeof(GridColumnBand),
                new FrameworkPropertyMetadata(null));

        /// <summary>Identifies the <see cref="Children"/> dependency property.</summary>
        public static readonly DependencyProperty ChildrenProperty = ChildrenPropertyKey.DependencyProperty;

        /// <summary>
        /// Columns and/or nested bands under this band, in display order. Content property, so
        /// child elements declared inside the band land here.
        /// </summary>
        public FreezableCollection<ColumnDescriptorElement> Children =>
            (FreezableCollection<ColumnDescriptorElement>)GetValue(ChildrenProperty);
    }
}
