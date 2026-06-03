using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
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
        public override DataTemplate CreateDisplayTemplate(ColumnDataBase column)
            => BuildCheckBoxTemplate(column, isDisplay: true);

        public override DataTemplate CreateEditTemplate(ColumnDataBase column)
            => BuildCheckBoxTemplate(column, isDisplay: false);

        public override System.Collections.Generic.IEnumerable<Core.SearchType> GetSupportedFilterSearchTypes(Core.ColumnDataType columnDataType, bool isNullable)
            // Boolean columns only have two distinct values plus optional null — Equals is
            // the only meaningful operator. The selector is collapsed in the ColumnFilterControl
            // template for checkbox columns anyway (the cycle button drives the filter
            // implicitly), so this set is only consulted when a consumer surfaces the selector
            // explicitly. IsNull / IsNotNull flow in automatically when isNullable.
            => WithNullability(new[] { Core.SearchType.Equals }, isNullable);

        public override UIElement CreateFilterEditor(IColumnFilterHost host)
        {
            // Tri-state checkbox bound to FilterCheckboxState. The control's checkbox-cycle
            // logic handles the IsChecked transitions (null ↔ true ↔ false); the editor
            // itself just publishes the current state.
            var cb = new CheckBox
            {
                IsThreeState = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var style = Application.Current?.TryFindResource(EditSettingsThemeKeys.DisplayCheckBox) as Style;
            if (style != null) cb.Style = style;

            BindingOperations.SetBinding(cb, ToggleButton.IsCheckedProperty, new Binding(nameof(IColumnFilterHost.FilterCheckboxState))
            {
                Source = host,
                Mode = BindingMode.TwoWay,
            });
            return cb;
        }

        private DataTemplate BuildCheckBoxTemplate(ColumnDataBase column, bool isDisplay)
        {
            var factory = new FrameworkElementFactory(typeof(CheckBox));
            // Style FIRST — FrameworkElementFactory requires StyleProperty before other setters.
            // Both display and edit templates use DisplayCheckBox: the display checkbox is already
            // interactive (toggles via TwoWay binding without going through edit mode), so the
            // edit template doesn't need a separate visually-distinct style.
            ApplyDisplayStyle(factory, EditSettingsThemeKeys.DisplayCheckBox);
            ApplyTextAlignment(factory, column);

            factory.SetBinding(ToggleButton.IsCheckedProperty, CreateValueBinding(column));
            SuppressValidationErrorAdorner(factory);

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
