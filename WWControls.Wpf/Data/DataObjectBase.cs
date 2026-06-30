using System.Windows;

namespace WWControls.Wpf
{
    /// <summary>
    /// Root of the data-context hierarchy XAML template authors bind to when supplying
    /// <see cref="GridColumn.FilterRowDisplayTemplate"/> or
    /// <see cref="GridColumn.FilterRowEditTemplate"/>. Models DevExpress's
    /// <c>DataObjectBase</c>: the type itself carries no payload — the layered subclasses
    /// (<see cref="EditableDataObject"/>, <see cref="GridDataBase"/>,
    /// <see cref="GridColumnData"/>, <see cref="GridCellData"/>, <see cref="EditGridCellData"/>)
    /// progressively add <c>Value</c>, <c>Data</c>, <c>Column</c>, and row/cell context.
    /// </summary>
    /// <remarks>
    /// Lives in the WPF assembly because <see cref="DependencyObject"/> is WPF-specific and
    /// the templates that consume this hierarchy are XAML <see cref="DataTemplate"/>s — there
    /// is no Core-side abstraction to mirror. See architectural decision D5 in the
    /// FilterRow Spec-Conformance plan.
    /// </remarks>
    public abstract class DataObjectBase : DependencyObject
    {
    }
}
