using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF.Converters
{
    /// <summary>
    /// Resolves the visibility of the per-cell clear (X) button in the filter row from
    /// three inputs: the grid-wide <see cref="FilterClearButtonMode"/>, whether the
    /// editor itself holds a value the X can clear (<see cref="ColumnFilterControl.HasEditorInputValue"/>),
    /// and whether the cell is currently in the edit surface (i.e. focus is inside the
    /// <see cref="ColumnFilterControl"/>). The display / edit split mirrors the cell
    /// editor's display/edit state machine — same DP pair the user-supplied templates can
    /// observe.
    /// </summary>
    /// <remarks>
    /// The second input is the editor-scoped signal rather than the broader
    /// <see cref="ColumnFilterControl.HasActiveFilter"/> because the X's command
    /// (<see cref="ColumnFilterControl.ClearSearchTextCommand"/>) only clears the editor's
    /// pending text/value, the in-flight temporary template, and the checkbox cycle — it
    /// does not touch permanent rule-filter commits made via the popup editor. Binding to
    /// HasActiveFilter would surface the button for filters it cannot remove.
    /// </remarks>
    public class ClearButtonModeToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3)
                return Visibility.Collapsed;

            var mode = values[0] is FilterClearButtonMode m
                ? m
                : FilterClearButtonMode.Always;
            bool hasEditorInput = values[1] is bool editorInput && editorInput;
            bool isEditing = values[2] is bool editing && editing;

            switch (mode)
            {
                case FilterClearButtonMode.Never:
                    return Visibility.Collapsed;

                case FilterClearButtonMode.Always:
                    return hasEditorInput ? Visibility.Visible : Visibility.Collapsed;

                case FilterClearButtonMode.Display:
                    return hasEditorInput && !isEditing ? Visibility.Visible : Visibility.Collapsed;

                case FilterClearButtonMode.Edit:
                    return hasEditorInput && isEditing ? Visibility.Visible : Visibility.Collapsed;

                default:
                    return Visibility.Collapsed;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
