using System.Windows;
using System.Windows.Input;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;

namespace WWControls.SampleApp.Grid.Controls
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

        /// <summary>
        /// Overrides the mouse cursor shown over the editor. AvalonEdit's SelectionMouseHandler
        /// forces <see cref="Cursors.IBeam"/> via the TextArea's <c>QueryCursor</c> event and marks
        /// it handled, so setting <see cref="UIElement.Cursor"/> normally has no effect. This hooks
        /// <c>QueryCursor</c> with <c>handledEventsToo</c> so it runs after AvalonEdit and wins.
        /// </summary>
        public static readonly DependencyProperty OverrideCursorProperty = DependencyProperty.RegisterAttached(
            "OverrideCursor",
            typeof(Cursor),
            typeof(AvalonEditHelper),
            new PropertyMetadata(null, OnOverrideCursorChanged));

        public static void SetOverrideCursor(DependencyObject obj, Cursor value) =>
            obj.SetValue(OverrideCursorProperty, value);

        public static Cursor GetOverrideCursor(DependencyObject obj) =>
            (Cursor)obj.GetValue(OverrideCursorProperty);

        private static void OnOverrideCursorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextEditor editor) return;

            // Idempotent: drop any prior hook before deciding whether to re-add.
            editor.RemoveHandler(Mouse.QueryCursorEvent, (QueryCursorEventHandler)OnEditorQueryCursor);
            if (e.NewValue is Cursor)
                editor.AddHandler(Mouse.QueryCursorEvent, (QueryCursorEventHandler)OnEditorQueryCursor, handledEventsToo: true);
        }

        private static void OnEditorQueryCursor(object sender, QueryCursorEventArgs e)
        {
            if (sender is not DependencyObject d) return;
            var cursor = GetOverrideCursor(d);
            if (cursor != null)
            {
                e.Cursor = cursor;
                e.Handled = true;
            }
        }
    }
}
