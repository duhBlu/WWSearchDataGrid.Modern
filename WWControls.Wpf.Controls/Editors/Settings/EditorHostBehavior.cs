using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace WWControls.Wpf.Controls.Editors.Settings
{
    /// <summary>
    /// Grid-side glue that teaches a <see cref="WWEditorBase"/>-derived editor how to behave inside a
    /// <see cref="DataGridCell"/>, without the editor itself carrying any grid coupling. An
    /// <c>EditSettings</c> adapter sets <see cref="HostInCellProperty"/> on the editor it builds for
    /// a cell; this behavior then drives the three cell-interaction concerns that used to live in the
    /// editor templates:
    /// <list type="number">
    ///   <item><b>Arrow-key cell exit</b> — an unmodified arrow at a text boundary commits the edit
    ///   and steps to the adjacent cell (via <see cref="BaseEditorSettings.ExitCellViaArrow"/>), instead
    ///   of moving the caret.</item>
    ///   <item><b>Mouse-click caret</b> — when edit mode was opened by a mouse click, the caret lands
    ///   at the clicked character (via <see cref="SearchDataGrid.TryConsumeMouseEditPoint"/>); keyboard
    ///   entry selects all instead.</item>
    ///   <item><b>Focus-on-edit</b> — the editor's text element grabs keyboard focus the moment it
    ///   materializes, so the user types on the same click that opened the cell.</item>
    /// </list>
    /// The editor only exposes its text element through <see cref="IEditTextBoxProvider"/> and raises
    /// normal input events; all of the above is layered on from here. Decoration-button visibility
    /// (the third historical coupling) is wired by the adapter directly, since it is a pure binding
    /// over column / cell state and needs no live-element interaction.
    /// </summary>
    public static class EditorHostBehavior
    {
        /// <summary>
        /// Set true by an <c>EditSettings</c> adapter on a <see cref="WWEditorBase"/>-derived editor it
        /// builds for a grid cell's edit template. Wires arrow-exit, mouse-click caret, and
        /// focus-on-edit against the editor's <see cref="IEditTextBoxProvider.EditTextBox"/>.
        /// </summary>
        public static readonly DependencyProperty HostInCellProperty =
            DependencyProperty.RegisterAttached(
                "HostInCell",
                typeof(bool),
                typeof(EditorHostBehavior),
                new PropertyMetadata(false, OnHostInCellChanged));

        public static void SetHostInCell(DependencyObject element, bool value)
            => element.SetValue(HostInCellProperty, value);

        public static bool GetHostInCell(DependencyObject element)
            => (bool)element.GetValue(HostInCellProperty);

        // Marks a TextBox whose interaction handlers are already attached, so a recycled editor that
        // raises Loaded again only re-runs focus-on-edit rather than stacking duplicate handlers.
        private static readonly DependencyProperty IsWiredProperty =
            DependencyProperty.RegisterAttached(
                "IsWired",
                typeof(bool),
                typeof(EditorHostBehavior),
                new PropertyMetadata(false));

        private static void OnHostInCellChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement editor) return;

            if ((bool)e.NewValue)
            {
                editor.Loaded += OnEditorLoaded;
                if (editor.IsLoaded)
                    WireEditor(editor);
            }
            else
            {
                editor.Loaded -= OnEditorLoaded;
            }
        }

        private static void OnEditorLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement editor)
                WireEditor(editor);
        }

        private static void WireEditor(FrameworkElement editor)
        {
            var tb = (editor as IEditTextBoxProvider)?.EditTextBox;
            if (tb == null) return;

            if (!(bool)tb.GetValue(IsWiredProperty))
            {
                tb.SetValue(IsWiredProperty, true);
                tb.GotKeyboardFocus += OnEditTextBoxGotKeyboardFocus;
                tb.PreviewKeyDown += OnEditTextBoxPreviewKeyDown;
            }

            FocusOnEdit(tb);
        }

        /// <summary>
        /// Defers a focus call until WPF's input/focus pipeline has finished routing the click that
        /// opened edit mode — focusing synchronously inside Loaded can race that pipeline and strand
        /// focus on the cell.
        /// </summary>
        private static void FocusOnEdit(TextBox tb)
        {
            tb.Dispatcher.BeginInvoke(new Action(() => Keyboard.Focus(tb)), DispatcherPriority.Input);
        }

        private static void OnEditTextBoxGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is not TextBox tb) return;

            // A masked editor runs its own focus handler (selects the first editable region); leave it
            // alone so the two don't clobber each other.
            if (!string.IsNullOrEmpty(MaskInputBehavior.GetMask(tb))) return;

            // Mouse-opened edit → caret at the clicked character; keyboard-opened edit → select all.
            if (TryApplyMouseClickCaret(tb)) return;
            tb.SelectAll();
        }

        private static void OnEditTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            var tb = (e.OriginalSource as TextBox) ?? (sender as TextBox);
            if (tb == null) return;
            if (e.Key != Key.Left && e.Key != Key.Right && e.Key != Key.Up && e.Key != Key.Down) return;

            // Modified arrows defer to the TextBox default (caret jump / extend selection).
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0) return;

            if (!BaseEditorSettings.ShouldExitCellOnArrow(tb, e.Key)) return;

            e.Handled = true;
            BaseEditorSettings.ExitCellViaArrow(tb, e);
        }

        /// <summary>
        /// Mouse-driven edit entry: pull the click point the <see cref="SearchDataGrid"/> stashed for
        /// this cell, project it into <paramref name="tb"/>'s coordinate space, and set the caret at
        /// the character there. Returns false (making no change) when there's no pending point — the
        /// caller falls back to select-all.
        /// </summary>
        private static bool TryApplyMouseClickCaret(TextBox tb)
        {
            var cell = VisualTreeHelperMethods.FindVisualAncestor<DataGridCell>(tb);
            if (cell == null) return false;
            var grid = VisualTreeHelperMethods.FindVisualAncestor<IEditingGridHost>(cell);
            if (grid == null) return false;
            if (!grid.TryConsumeMouseEditPoint(cell, out var cellPoint)) return false;

            try
            {
                var pointInTb = cell.TranslatePoint(cellPoint, tb);
                int idx = tb.GetCharacterIndexFromPoint(pointInTb, snapToText: true);
                if (idx < 0) return false;

                // GetCharacterIndexFromPoint(snapToText:true) returns the closest character; a click
                // past the trailing edge of the last character lands one slot short of the end. If the
                // click X is at or past the post-text caret rect, place the caret after the last char.
                int textLen = tb.Text?.Length ?? 0;
                if (textLen > 0)
                {
                    var endRect = tb.GetRectFromCharacterIndex(textLen, false);
                    if (!endRect.IsEmpty && pointInTb.X >= endRect.X)
                        idx = textLen;
                }

                tb.CaretIndex = idx;
                tb.SelectionLength = 0;
                return true;
            }
            catch
            {
                // TranslatePoint throws if the elements aren't in a common visual tree (e.g. the cell
                // was unloaded between BeginEdit and focus). Fall back to select-all.
                return false;
            }
        }
    }
}
