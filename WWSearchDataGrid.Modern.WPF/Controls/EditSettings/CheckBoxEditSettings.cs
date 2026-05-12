using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Checkbox editor for boolean columns. The display CheckBox is interactive (toggles on
    /// click via TwoWay binding), so a checkbox cell never actually needs to enter edit mode —
    /// both the cell template and the editing template render the same control. The default
    /// editor for bool columns. Read-only handling (block clicks + visual dim) lives in the
    /// keyed DisplayCheckBox style's Style.Triggers — see Themes/EditSettings.xaml.
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
            // Both display and edit templates use DisplayCheckBox: the display checkbox is already
            // interactive (toggles via TwoWay binding without going through edit mode), so the
            // edit template doesn't need a separate visually-distinct style.
            ApplyDisplayStyle(factory, EditSettingsThemeKeys.DisplayCheckBox);
            ApplyTextAlignment(factory, column);

            factory.SetBinding(ToggleButton.IsCheckedProperty, CreateValueBinding(column));

            if (!isDisplay)
            {
                // Edit-template only: ensure the CheckBox receives focus when the user tabs
                // into the cell, so Space toggles immediately. Display-mode clicks toggle via
                // the now-interactive display CheckBox (IsHitTestVisible=True) without needing
                // edit mode at all — first click on the cell hits the CheckBox and toggles it.
                AutoFocusOnLoad(factory);

                // CheckBox doesn't consume arrow keys, and DataGrid skips arrow navigation
                // while a cell is editing — so without this, arrow keys would do nothing
                // in CheckBox edit mode. Commit + re-raise on the parent grid so the
                // standard cell-arrow-navigation runs.
                factory.AddHandler(UIElement.PreviewKeyDownEvent,
                    new KeyEventHandler((s, e) =>
                    {
                        if (s is not CheckBox cb) return;
                        if (e.Key == Key.Left || e.Key == Key.Right
                            || e.Key == Key.Up || e.Key == Key.Down)
                        {
                            e.Handled = true;
                            ExitCellViaArrow(cb, e);
                        }
                    }));
            }

            return new DataTemplate { VisualTree = factory };
        }
    }
}
