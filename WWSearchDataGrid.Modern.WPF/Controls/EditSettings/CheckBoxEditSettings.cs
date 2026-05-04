using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Checkbox editor for boolean columns. Display mode is a hit-test-disabled CheckBox; edit
    /// mode is interactive. The default editor for bool columns.
    /// </summary>
    public class CheckBoxEditSettings : BaseEditSettings
    {
        public override DataTemplate CreateDisplayTemplate(GridColumn column)
            => BuildCheckBoxTemplate(column, isDisplay: true);

        public override DataTemplate CreateEditTemplate(GridColumn column)
            => BuildCheckBoxTemplate(column, isDisplay: false);

        private DataTemplate BuildCheckBoxTemplate(GridColumn column, bool isDisplay)
        {
            var factory = new FrameworkElementFactory(typeof(CheckBox));
            // Style FIRST — FrameworkElementFactory requires StyleProperty before other setters.
            if (isDisplay)
                ApplyDisplayStyle(factory, "SdgDisplayCheckBoxStyle");
            else
                ApplyEditorStyle(factory, "SdgEditCheckBoxStyle");

            factory.SetBinding(ToggleButton.IsCheckedProperty, CreateValueBinding(column));
            return new DataTemplate { VisualTree = factory };
        }
    }
}
