using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WWControls.Wpf.Editors.Settings
{
    /// <summary>
    /// Drives the <see cref="UIElement.Visibility"/> of an editor's decoration buttons (combo
    /// toggle, spinner up/down, calendar dropdown) based on
    /// <see cref="EditorButtonShowMode"/> plus the cell / row / edit-state of the surrounding
    /// <see cref="System.Windows.Controls.DataGridCell"/>.
    /// <para>
    /// Bound as a <see cref="MultiBinding"/> with values, in order:
    /// <list type="number">
    ///   <item>Editor's <see cref="BaseEditorSettings.EditorButtonShowMode"/>.</item>
    ///   <item>Grid's <see cref="SearchDataGrid.EditorButtonShowMode"/> (fallback when editor is Default).</item>
    ///   <item><see cref="System.Windows.Controls.DataGridCell.IsKeyboardFocusWithin"/>.</item>
    ///   <item><see cref="System.Windows.Controls.DataGridCell.IsEditing"/>.</item>
    ///   <item><see cref="System.Windows.Controls.DataGridRow.IsSelected"/>.</item>
    ///   <item><see cref="System.Windows.Controls.DataGridCell.IsReadOnly"/>.</item>
    ///   <item><see cref="System.Windows.Controls.DataGrid.IsReadOnly"/>.</item>
    /// </list>
    /// Both modes are bound (rather than resolved at template-build time) so toggling the grid's
    /// mode at runtime propagates to existing cells without rebuilding templates. Read-only state
    /// is checked at both the cell and grid level — same OR-both-flags pattern used by the
    /// DisplayCheckBox style — because <c>DataGridCell.IsReadOnly</c> coercion doesn't propagate
    /// reliably from <c>DataGrid.IsReadOnly</c> when cells render through a
    /// <c>DataGridTemplateColumn</c>. If either flag is true, the decoration button is hidden:
    /// the user can't act on it anyway, so showing it is just visual noise.
    /// </para>
    /// </summary>
    public class EditorButtonVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 5) return Visibility.Collapsed;

            var settingsMode = values[0] is EditorButtonShowMode m1 ? m1 : EditorButtonShowMode.Default;
            var gridMode = values[1] is EditorButtonShowMode m2 ? m2 : EditorButtonShowMode.ShowOnlyInEditor;
            bool cellFocused = values[2] is bool b1 && b1;
            bool isEditing = values[3] is bool b2 && b2;
            bool rowSelected = values[4] is bool b3 && b3;
            // Read-only flags are optional in the value array (positions 5 and 6) so older
            // bindings that didn't carry them still resolve. Treat missing as false.
            bool cellReadOnly = values.Length > 5 && values[5] is bool b4 && b4;
            bool gridReadOnly = values.Length > 6 && values[6] is bool b5 && b5;

            // Read-only short-circuit: a decoration button on a non-editable cell is misleading
            // (clicking it can't reach an edit state) so collapse regardless of show-mode.
            if (cellReadOnly || gridReadOnly)
                return Visibility.Collapsed;

            // Editor-level Default falls through to grid; grid-level Default equals ShowOnlyInEditor.
            var mode = settingsMode != EditorButtonShowMode.Default ? settingsMode : gridMode;
            if (mode == EditorButtonShowMode.Default)
                mode = EditorButtonShowMode.ShowOnlyInEditor;

            bool show = mode switch
            {
                EditorButtonShowMode.ShowAlways => true,
                EditorButtonShowMode.ShowForFocusedRow => rowSelected || cellFocused || isEditing,
                EditorButtonShowMode.ShowForFocusedCell => cellFocused || isEditing,
                EditorButtonShowMode.ShowOnlyInEditor => isEditing,
                _ => isEditing,
            };

            return show ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException("EditorButtonVisibilityConverter is one-way.");
    }
}
