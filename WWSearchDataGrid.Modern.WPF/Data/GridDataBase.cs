using System.Windows;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Adds the <see cref="Data"/> slot for grid-wide payload — typically the bound row
    /// object in cell-edit contexts. In filter-row context this stays <c>null</c>;
    /// templates that expect to also be reusable in cell editors can still bind to
    /// <c>{Binding Data}</c> safely.
    /// </summary>
    public abstract class GridDataBase : EditableDataObject
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(object),
                typeof(GridDataBase),
                new PropertyMetadata(null));

        public object Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
    }
}
