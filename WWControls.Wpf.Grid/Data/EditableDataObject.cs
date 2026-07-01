using System.Windows;

namespace WWControls.Wpf
{
    /// <summary>
    /// Adds the editable <see cref="Value"/> slot template authors bind to. In filter-row
    /// context, <see cref="ColumnFilterControl"/> two-way-binds this property to its own
    /// <c>SearchValue</c> (or <c>SearchText</c> for text editors), so the template's editor
    /// surface drives the column filter without the template author needing to know which
    /// host DP backs the value.
    /// </summary>
    public abstract class EditableDataObject : DataObjectBase
    {
        /// <summary>
        /// Two-way binding target for the template's editor. Default
        /// <see cref="FrameworkPropertyMetadataOptions.BindsTwoWayByDefault"/> matches the
        /// DevExpress convention so <c>{Binding Value}</c> works without an explicit Mode
        /// in user templates.
        /// </summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(object),
                typeof(EditableDataObject),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }
}
