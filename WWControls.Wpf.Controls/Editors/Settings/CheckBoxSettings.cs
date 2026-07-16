using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace WWControls.Wpf.Controls.Editors.Settings
{
    /// <summary>
    /// Checkbox editor for boolean columns. The display CheckBox is interactive (toggles on
    /// click via TwoWay binding), so a checkbox cell never actually needs to enter edit mode —
    /// both the cell template and the editing template render the same control. The default
    /// editor for bool columns. Read-only handling (block clicks + visual dim) lives in the
    /// keyed DisplayCheckBox style's Style.Triggers — see Themes/EditSettings.xaml.
    /// </summary>
    public class CheckBoxSettings : BaseEditorSettings
    {
        public override DataTemplate CreateDisplayTemplate(IEditorColumn column)
            => BuildCheckBoxTemplate(column, isDisplay: true);

        public override DataTemplate CreateEditTemplate(IEditorColumn column)
            => BuildCheckBoxTemplate(column, isDisplay: false);

        public override System.Collections.Generic.IEnumerable<Core.SearchType> GetSupportedFilterSearchTypes(Core.ColumnDataType columnDataType, bool isNullable)
            // Boolean columns only have two distinct values plus optional null — Equals is
            // the only meaningful operator. The selector is collapsed in the ColumnFilterControl
            // template for checkbox columns anyway (the cycle button drives the filter
            // implicitly), so this set is only consulted when a consumer surfaces the selector
            // explicitly. IsNull / IsNotNull flow in automatically when isNullable.
            => WithNullability(new[] { Core.SearchType.Equals }, isNullable);

        public override UIElement CreateFilterEditor(IFilterEditorHost host)
        {
            // Tri-state WWCheckBox bound to FilterCheckboxState. The checkbox-cycle logic handles
            // the IsChecked transitions (null ↔ true ↔ false); the editor just publishes the state.
            var editor = new WWCheckBox
            {
                IsThreeState = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            BindingOperations.SetBinding(editor, WWCheckBox.IsCheckedProperty, new Binding(nameof(IFilterEditorHost.FilterCheckboxState))
            {
                Source = host,
                Mode = BindingMode.TwoWay,
            });
            return editor;
        }

        private DataTemplate BuildCheckBoxTemplate(IEditorColumn column, bool isDisplay)
        {
            var factory = new FrameworkElementFactory(typeof(WWCheckBox));

            // Both display and edit host the same interactive WWCheckBox — the checkbox toggles via
            // its two-way binding without going through edit mode, so there's no visually distinct
            // edit surface. The themed checkbox look + the cell read-only gate live in WWCheckBox's
            // hosted CheckBox style (EditorThemeKeys.DisplayCheckBox).
            //
            // Commit on toggle, not on focus loss: the box is live in both display and edit, and in a
            // hostless row (property grid) its inner CheckBox is non-focusable, so a click that
            // toggles the box never raises LostFocus and the bound value would stay stale.
            // PropertyChanged pushes each toggle straight through.
            var valueBinding = CreateValueBinding(column);
            valueBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            factory.SetBinding(WWCheckBox.IsCheckedProperty, valueBinding);
            SuppressValidationErrorAdorner(factory);

            if (!isDisplay)
            {
                if (column?.Host != null)
                {
                    // Grid cell: focus the editor when the user tabs into the cell so Space toggles
                    // immediately (WWCheckBox forwards focus to its checkbox).
                    AutoFocusOnLoad(factory);

                    // CheckBox doesn't consume arrow keys and DataGrid skips arrow navigation while a
                    // cell is editing — so commit + re-raise on the parent grid for any arrow, letting
                    // the standard cell-arrow-navigation run. (Editor-specific coupling wired by the
                    // adapter, the bridge; the control stays grid-agnostic.)
                    factory.AddHandler(UIElement.PreviewKeyDownEvent,
                        new KeyEventHandler((s, e) =>
                        {
                            if (s is not WWCheckBox editor) return;
                            if (e.Key == Key.Left || e.Key == Key.Right
                                || e.Key == Key.Up || e.Key == Key.Down)
                            {
                                e.Handled = true;
                                ExitCellViaArrow(editor, e);
                            }
                        }));
                }
                else
                {
                    // Hostless (property grid): no cell owns the tab stop, so the checkbox is one
                    // itself. Its inner CheckBox is non-focusable (theme), so focus rests on the
                    // WWCheckBox, which toggles on Space (WWCheckBox.OnKeyDown). Arrows are left
                    // untouched — there's no cell to navigate.
                    factory.SetValue(WWCheckBox.IsTabStopProperty, true);
                }
            }

            return new DataTemplate { VisualTree = factory };
        }
    }
}
