namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// A column descriptor that defines how a column should be created and configured in a
    /// <see cref="SearchDataGrid"/>. Instead of manually creating <see cref="System.Windows.Controls.DataGridColumn"/>
    /// instances and setting attached properties, declare <see cref="GridColumn"/> descriptors
    /// inside <c>SearchDataGrid.GridColumns</c> and the grid will generate the internal WPF
    /// columns automatically.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="GridColumn"/> sits at the bottom of the column hierarchy:
    /// <see cref="ColumnDescriptorElement"/> → <see cref="ColumnLayoutBase"/> →
    /// <see cref="ColumnDataBase"/> → <see cref="GridColumn"/>. The base tiers carry layout, data
    /// identity, filtering, sorting, and editor concerns; this tier is reserved for the
    /// grid-specific surface (grouping, total summaries) that does not apply to other column
    /// hosts.
    /// </para>
    /// <para>
    /// The <see cref="ColumnDataBase.FieldName"/> property is the primary key: it drives <c>Binding</c>,
    /// <c>SortMemberPath</c>, and <c>FilterMemberPath</c> unless explicitly overridden.
    /// </para>
    /// </remarks>
    public class GridColumn : ColumnDataBase
    {
    }
}
