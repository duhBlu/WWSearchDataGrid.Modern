using System.Windows;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

namespace WWSearchDataGrid.Modern.SampleApp.Controls
{
    /// <summary>
    /// Bridges <see cref="TextEditor"/> CLR-only properties to bindable attached DPs. AvalonEdit
    /// exposes <c>Text</c> and <c>SyntaxHighlighting</c> as CLR properties (their DPs are
    /// <c>Document</c> and <c>SyntaxHighlightingProperty</c> with non-bindable shapes), so
    /// switching highlighting at runtime via a binding needs this indirection.
    /// </summary>
    public static class AvalonEditHelper
    {
        public static readonly DependencyProperty BoundTextProperty = DependencyProperty.RegisterAttached(
            "BoundText",
            typeof(string),
            typeof(AvalonEditHelper),
            new PropertyMetadata(string.Empty, OnBoundTextChanged));

        public static void SetBoundText(DependencyObject obj, string value) =>
            obj.SetValue(BoundTextProperty, value);

        public static string GetBoundText(DependencyObject obj) =>
            (string)obj.GetValue(BoundTextProperty);

        private static void OnBoundTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextEditor editor) return;
            var text = e.NewValue as string ?? string.Empty;
            if (editor.Text != text)
                editor.Text = text;
        }

        /// <summary>
        /// Binds a syntax-highlighting definition by name (<c>"XML"</c>, <c>"C#"</c>, etc.).
        /// Resolves through <see cref="HighlightingManager.Instance"/>; an unknown name clears
        /// highlighting (plain text).
        /// </summary>
        public static readonly DependencyProperty BoundSyntaxNameProperty = DependencyProperty.RegisterAttached(
            "BoundSyntaxName",
            typeof(string),
            typeof(AvalonEditHelper),
            new PropertyMetadata(null, OnBoundSyntaxNameChanged));

        public static void SetBoundSyntaxName(DependencyObject obj, string value) =>
            obj.SetValue(BoundSyntaxNameProperty, value);

        public static string GetBoundSyntaxName(DependencyObject obj) =>
            (string)obj.GetValue(BoundSyntaxNameProperty);

        private static void OnBoundSyntaxNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextEditor editor) return;
            var name = e.NewValue as string;
            editor.SyntaxHighlighting = string.IsNullOrEmpty(name)
                ? null
                : HighlightingManager.Instance.GetDefinition(name);
        }
    }
}
