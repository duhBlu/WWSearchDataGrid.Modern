using System.Windows;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// Host-context signal for editor chrome. <see cref="ShowEditorBorderProperty"/> is an
    /// inheritable attached flag marking that an editor is hosted somewhere it should render its
    /// own border — set on the edit-form presenter so every editor it hosts shows a border, while
    /// the same editor templates stay flat (borderless) in a grid cell or the filter row, where it
    /// defaults to <c>false</c>. The <c>WWxxxEdit</c> controls bind <see cref="WWBaseEdit.ShowBorder"/>
    /// to this flag (chrome is owned once by <see cref="WWBaseEdit"/>); the <c>EditTextBox</c> style
    /// keyed on it still drives the inner TextBox of <see cref="SegmentedDateTimeEditor"/>. Checkbox
    /// and read-only display editors carry no such trigger and stay flat regardless.
    /// </summary>
    /// <remarks>
    /// Lives in the Editors assembly (not on the grid-side <c>BaseEditSettings</c> adapter) so the
    /// editor controls can reference it without depending on the grid: the flag is an editor concern,
    /// the grid adapters and the edit-form presenter merely set / bind it as the host.
    /// </remarks>
    public static class EditorChrome
    {
        /// <inheritdoc cref="EditorChrome"/>
        public static readonly DependencyProperty ShowEditorBorderProperty =
            DependencyProperty.RegisterAttached(
                "ShowEditorBorder",
                typeof(bool),
                typeof(EditorChrome),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>Sets <see cref="ShowEditorBorderProperty"/> on <paramref name="element"/>.</summary>
        public static void SetShowEditorBorder(DependencyObject element, bool value)
            => element.SetValue(ShowEditorBorderProperty, value);

        /// <summary>Reads <see cref="ShowEditorBorderProperty"/> from <paramref name="element"/>.</summary>
        public static bool GetShowEditorBorder(DependencyObject element)
            => (bool)element.GetValue(ShowEditorBorderProperty);
    }
}
