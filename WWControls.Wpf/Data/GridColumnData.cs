using System.Windows;

namespace WWControls.Wpf
{
    /// <summary>
    /// Adds the <see cref="Column"/> slot exposing the originating <see cref="GridColumn"/>
    /// descriptor. Template authors bind to <c>{Binding Column.Header}</c>,
    /// <c>{Binding Column.FieldName}</c>, etc. to surface column metadata inside their
    /// filter editor.
    /// </summary>
    public abstract class GridColumnData : GridDataBase
    {
        public static readonly DependencyProperty ColumnProperty =
            DependencyProperty.Register(
                nameof(Column),
                typeof(GridColumn),
                typeof(GridColumnData),
                new PropertyMetadata(null));

        public GridColumn Column
        {
            get => (GridColumn)GetValue(ColumnProperty);
            set => SetValue(ColumnProperty, value);
        }
    }
}
