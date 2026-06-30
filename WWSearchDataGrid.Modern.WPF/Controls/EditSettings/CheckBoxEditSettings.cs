using System.Windows;
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
            // Tri-state WWCheckEdit bound to FilterCheckboxState. The checkbox-cycle logic handles
            // the IsChecked transitions (null ↔ true ↔ false); the editor just publishes the state.
            var editor = new WWCheckEdit
            {
                IsThreeState = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            BindingOperations.SetBinding(editor, WWCheckEdit.IsCheckedProperty, new Binding(nameof(IColumnFilterHost.FilterCheckboxState))
            {
                Source = host,
                Mode = BindingMode.TwoWay,
            });
            return editor;
        }

        private DataTemplate BuildCheckBoxTemplate(ColumnDataBase column, bool isDisplay)
        {
            var factory = new FrameworkElementFactory(typeof(WWCheckEdit));

            // Both display and edit host the same interactive WWCheckEdit — the checkbox toggles via
            // its two-way binding without going through edit mode, so there's no visually distinct
            // edit surface. The themed checkbox look + the cell read-only gate live in WWCheckEdit's
            // hosted CheckBox style (EditSettingsThemeKeys.DisplayCheckBox).
            factory.SetBinding(WWCheckEdit.IsCheckedProperty, CreateValueBinding(column));
            SuppressValidationErrorAdorner(factory);

            if (!isDisplay)
            {
                // Edit-template only: focus the editor when the user tabs into the cell so Space
                // toggles immediately (WWCheckEdit forwards focus to its checkbox). Display-mode
                // clicks toggle the interactive checkbox directly without entering edit mode.
                AutoFocusOnLoad(factory);

                // CheckBox doesn't consume arrow keys and DataGrid skips arrow navigation while a
                // cell is editing — so commit + re-raise on the parent grid for any arrow, letting
                // the standard cell-arrow-navigation run. (Editor-specific coupling wired by the
                // adapter, the bridge; the control stays grid-agnostic.)
                factory.AddHandler(UIElement.PreviewKeyDownEvent,
                    new KeyEventHandler((s, e) =>
                    {
                        if (s is not WWCheckEdit editor) return;
                        if (e.Key == Key.Left || e.Key == Key.Right
                            || e.Key == Key.Up || e.Key == Key.Down)
                        {
                            e.Handled = true;
                            ExitCellViaArrow(editor, e);
                        }
                    }));
            }

            return new DataTemplate { VisualTree = factory };
        }
    }
}
