using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core.Display;

namespace WWSearchDataGrid.Modern.WPF.Behaviors
{
    /// <summary>
    /// Attached behavior that adds mask input handling to any TextBox.
    /// When the Mask attached property is set, keystrokes are validated against the mask pattern,
    /// the caret auto-advances past literals, and Tab cycles between editable regions.
    ///
    /// Usage: behaviors:MaskInputBehavior.Mask="0+\.00" on a TextBox element.
    /// </summary>
    public static class MaskInputBehavior
    {
        #region State Tracking

        private class MaskState
        {
            public IMaskFormatter Formatter { get; set; }
            public bool RegionEditMode { get; set; }
            public bool IsUpdating { get; set; }

            /// <summary>
            /// Set by <see cref="OnTextChanged"/> when it placed the caret deliberately (e.g.
            /// after seed-character routing). The next <see cref="OnGotFocus"/> consumes and
            /// clears the flag, skipping its first-region selection so the caret stays where
            /// the seed-char path put it. Prevents the type-to-edit "(5__) ___-____ with
            /// '5__' selected" bug where OnGotFocus runs after the seed and reselects.
            /// </summary>
            public bool SuppressNextRegionSelect { get; set; }
        }

        private static readonly Dictionary<TextBox, MaskState> _states = new Dictionary<TextBox, MaskState>();

        #endregion

        #region Attached Properties

        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.RegisterAttached(
                "Mask",
                typeof(string),
                typeof(MaskInputBehavior),
                new PropertyMetadata(null, OnMaskChanged));

        public static string GetMask(DependencyObject obj) => (string)obj.GetValue(MaskProperty);
        public static void SetMask(DependencyObject obj, string value) => obj.SetValue(MaskProperty, value);

        /// <summary>
        /// How <see cref="MaskProperty"/> is interpreted. Defaults to
        /// <see cref="MaskType.Simple"/>. Setting this rebuilds the active formatter so the order
        /// in which <see cref="MaskProperty"/> and this DP are set on a factory does not matter.
        /// </summary>
        public static readonly DependencyProperty MaskTypeProperty =
            DependencyProperty.RegisterAttached(
                "MaskType",
                typeof(MaskType),
                typeof(MaskInputBehavior),
                new PropertyMetadata(MaskType.Simple, OnMaskTypeChanged));

        public static MaskType GetMaskType(DependencyObject obj) => (MaskType)obj.GetValue(MaskTypeProperty);
        public static void SetMaskType(DependencyObject obj, MaskType value) => obj.SetValue(MaskTypeProperty, value);

        private static void OnMaskTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Re-attach if a mask is already configured — picks up the new type. No-op when no
            // mask is set yet (Attach will run when MaskProperty changes and read the new type).
            if (!(d is TextBox textBox)) return;
            string mask = GetMask(textBox);
            if (string.IsNullOrEmpty(mask)) return;
            if (_states.ContainsKey(textBox)) Detach(textBox);
            Attach(textBox, mask);
        }

        public static readonly DependencyProperty PromptCharProperty =
            DependencyProperty.RegisterAttached(
                "PromptChar",
                typeof(char),
                typeof(MaskInputBehavior),
                new PropertyMetadata('_'));

        public static char GetPromptChar(DependencyObject obj) => (char)obj.GetValue(PromptCharProperty);
        public static void SetPromptChar(DependencyObject obj, char value) => obj.SetValue(PromptCharProperty, value);

        public static readonly DependencyProperty IsMaskCompleteProperty =
            DependencyProperty.RegisterAttached(
                "IsMaskComplete",
                typeof(bool),
                typeof(MaskInputBehavior),
                new PropertyMetadata(false));

        public static bool GetIsMaskComplete(DependencyObject obj) => (bool)obj.GetValue(IsMaskCompleteProperty);
        private static void SetIsMaskComplete(DependencyObject obj, bool value) => obj.SetValue(IsMaskCompleteProperty, value);

        public static readonly DependencyProperty UnmaskedValueProperty =
            DependencyProperty.RegisterAttached(
                "UnmaskedValue",
                typeof(string),
                typeof(MaskInputBehavior),
                new PropertyMetadata(string.Empty));

        public static string GetUnmaskedValue(DependencyObject obj) => (string)obj.GetValue(UnmaskedValueProperty);
        private static void SetUnmaskedValue(DependencyObject obj, string value) => obj.SetValue(UnmaskedValueProperty, value);

        #endregion

        #region Attach / Detach

        private static void OnMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is TextBox textBox)) return;

            string newMask = e.NewValue as string;

            // Detach from old mask
            if (_states.ContainsKey(textBox))
            {
                Detach(textBox);
            }

            // Attach with new mask
            if (!string.IsNullOrEmpty(newMask))
            {
                Attach(textBox, newMask);
            }
        }

        private static void Attach(TextBox textBox, string mask)
        {
            char promptChar = GetPromptChar(textBox);
            MaskType maskType = GetMaskType(textBox);
            IMaskFormatter formatter = MaskFormatterFactory.Create(maskType, mask, promptChar);

            _states[textBox] = new MaskState
            {
                Formatter = formatter,
                RegionEditMode = false,
                IsUpdating = false
            };

            textBox.PreviewTextInput += OnPreviewTextInput;
            textBox.PreviewKeyDown += OnPreviewKeyDown;
            textBox.GotFocus += OnGotFocus;
            textBox.LostFocus += OnLostFocus;
            textBox.TextChanged += OnTextChanged;
            DataObject.AddPastingHandler(textBox, OnPasting);
            textBox.Unloaded += OnUnloaded;

            // If the textbox already has text, format it through the mask
            if (!string.IsNullOrEmpty(textBox.Text))
            {
                var state = _states[textBox];
                state.IsUpdating = true;
                string formatted = formatter.Format(textBox.Text);
                textBox.Text = formatted;
                UpdateAttachedState(textBox, formatter);
                state.IsUpdating = false;
            }
        }

        private static void Detach(TextBox textBox)
        {
            textBox.PreviewTextInput -= OnPreviewTextInput;
            textBox.PreviewKeyDown -= OnPreviewKeyDown;
            textBox.GotFocus -= OnGotFocus;
            textBox.LostFocus -= OnLostFocus;
            textBox.TextChanged -= OnTextChanged;
            DataObject.RemovePastingHandler(textBox, OnPasting);
            textBox.Unloaded -= OnUnloaded;
            _states.Remove(textBox);
        }

        private static void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && _states.ContainsKey(textBox))
                Detach(textBox);
        }

        #endregion

        #region Event Handlers

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!(sender is TextBox textBox) || !_states.TryGetValue(textBox, out var state))
                return;

            e.Handled = true; // Always suppress default input when mask is active
            state.RegionEditMode = true;

            if (string.IsNullOrEmpty(e.Text)) return;

            // Handle selection: clear selected text first
            if (textBox.SelectionLength > 0)
            {
                state.Formatter.ClearSelection(textBox.SelectionStart, textBox.SelectionLength);
            }

            char c = e.Text[0];
            var (displayText, newCaret) = state.Formatter.InsertChar(c, textBox.SelectionStart);

            state.IsUpdating = true;
            textBox.Text = displayText;
            textBox.CaretIndex = Math.Min(newCaret, displayText.Length);
            UpdateAttachedState(textBox, state.Formatter);
            state.IsUpdating = false;
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!(sender is TextBox textBox) || !_states.TryGetValue(textBox, out var state))
                return;

            var formatter = state.Formatter;

            switch (e.Key)
            {
                case Key.Back:
                    e.Handled = true;
                    if (textBox.SelectionLength > 0)
                    {
                        int selStart = textBox.SelectionStart;
                        formatter.ClearSelection(textBox.SelectionStart, textBox.SelectionLength);
                        state.IsUpdating = true;
                        textBox.Text = formatter.BuildDisplayText();
                        textBox.CaretIndex = selStart;
                        UpdateAttachedState(textBox, formatter);
                        state.IsUpdating = false;
                    }
                    else
                    {
                        var (text, caret) = formatter.DeleteChar(textBox.CaretIndex, forward: false);
                        state.IsUpdating = true;
                        textBox.Text = text;
                        textBox.CaretIndex = Math.Min(caret, text.Length);
                        UpdateAttachedState(textBox, formatter);
                        state.IsUpdating = false;
                    }
                    break;

                case Key.Delete:
                    e.Handled = true;
                    if (textBox.SelectionLength > 0)
                    {
                        int selStart = textBox.SelectionStart;
                        formatter.ClearSelection(textBox.SelectionStart, textBox.SelectionLength);
                        state.IsUpdating = true;
                        textBox.Text = formatter.BuildDisplayText();
                        textBox.CaretIndex = selStart;
                        UpdateAttachedState(textBox, formatter);
                        state.IsUpdating = false;
                    }
                    else
                    {
                        var (text, caret) = formatter.DeleteChar(textBox.CaretIndex, forward: true);
                        state.IsUpdating = true;
                        textBox.Text = text;
                        textBox.CaretIndex = Math.Min(caret, text.Length);
                        UpdateAttachedState(textBox, formatter);
                        state.IsUpdating = false;
                    }
                    break;

                case Key.Right when (Keyboard.Modifiers & ModifierKeys.Control) != 0:
                case Key.Left  when (Keyboard.Modifiers & ModifierKeys.Control) != 0:
                {
                    // Ctrl+Arrow cycles between editable regions. Tab is left alone so the grid
                    // can use it for cell-to-cell navigation. AddTextBoxCaretAwareArrowExit
                    // explicitly defers when Ctrl is held, so it won't try to exit the cell here.
                    if (!state.RegionEditMode) break;

                    var (regionIdx, _) = formatter.GetRegionAtCaret(textBox.CaretIndex);
                    int targetStart = e.Key == Key.Left
                        ? formatter.GetPrevEditableRegionStart(regionIdx)
                        : formatter.GetNextEditableRegionStart(regionIdx);

                    if (targetStart < 0)
                        break; // No more regions in that direction — let key pass.

                    e.Handled = true;

                    var (targetRegionIdx, _2) = formatter.GetRegionAtCaret(targetStart);
                    var (rStart, rLength) = formatter.GetEditableRegionBounds(targetRegionIdx);

                    textBox.SelectionStart = rStart;
                    textBox.SelectionLength = Math.Max(rLength, 1);
                    break;
                }

                case Key.Escape:
                {
                    // Custom-binding cells don't auto-revert on Esc — the SearchDataGrid's
                    // CancelEdit doesn't know about our edit-template's Binding. Pull the source
                    // value back into textbox.Text manually so the LostFocus auto-commit pushes
                    // the original value rather than the user's typed (and now-cancelled) edit.
                    var be = BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty);
                    if (be != null)
                    {
                        state.IsUpdating = true;
                        be.UpdateTarget();
                        // Resync formatter state from the reverted text so OnLostFocus's
                        // Finalize/UnmaskedValue check matches reality.
                        state.Formatter.Format(textBox.Text ?? string.Empty);
                        UpdateAttachedState(textBox, state.Formatter);
                        state.IsUpdating = false;
                    }
                    // Don't Handle — let the grid see Esc to exit edit mode.
                    break;
                }
            }
        }

        private static void OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (!(sender is TextBox textBox) || !_states.TryGetValue(textBox, out var state))
                return;

            var formatter = state.Formatter;
            state.IsUpdating = true;

            // Refresh formatter state from current text. For an empty cell, fall back to the
            // mask placeholder ((___) ___-____ etc.) so the user can see the expected format
            // while editing. OnLostFocus still commits an empty source when UnmaskedValue is
            // empty, so the placeholder doesn't pollute the bound value.
            string formatted = formatter.Format(textBox.Text ?? string.Empty);
            if (string.IsNullOrEmpty(formatted))
                formatted = formatter.BuildDisplayText();
            if (textBox.Text != formatted)
                textBox.Text = formatted;
            UpdateAttachedState(textBox, formatter);
            state.IsUpdating = false;

            if (state.SuppressNextRegionSelect)
            {
                // Seed-char path already placed the caret right after the typed character.
                // Don't override — that would clobber the caret and let the user's next
                // keystroke replace the seed.
                state.RegionEditMode = true;
            }
            else
            {
                // SelectAll matches the standard textbox "tab in → entire content highlighted"
                // UX. The seed-char path in SearchDataGrid.OnGridPreviewTextInput collapses
                // SelectionLength to 1 before raising the synthetic PreviewTextInput, so the
                // "overwrite just the first character of the first region" behavior is
                // preserved for type-to-edit; the explicit SelectAll only affects tab-into-edit
                // and click-into-edit paths where the user has had a chance to see and decide
                // what to do with the existing value.
                textBox.SelectAll();
                state.RegionEditMode = true;
            }

            state.SuppressNextRegionSelect = false;
        }

        private static void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!(sender is TextBox textBox) || !_states.TryGetValue(textBox, out var state))
                return;

            state.RegionEditMode = false;

            state.IsUpdating = true;
            string finalized = state.Formatter.Finalize();

            // No data entered → commit a clean empty string to the bound source rather than
            // the prompt-character skeleton ((___) ___-____ etc.) or the legacy default-filled
            // form ((000) 000-0000). Lets nullable / optional fields stay blank.
            if (string.IsNullOrEmpty(state.Formatter.UnmaskedValue))
                finalized = string.Empty;

            if (textBox.Text != finalized)
                textBox.Text = finalized;
            UpdateAttachedState(textBox, state.Formatter);
            state.IsUpdating = false;
        }

        /// <summary>
        /// Catches Text mutations that didn't originate from this behavior — most importantly the
        /// SearchDataGrid type-to-edit "seed character" path, which sets <c>textBox.Text = c</c>
        /// directly when the user types on a display-mode cell. Without this hook, the seed char
        /// sits in the textbox untouched while the formatter state remains empty, so the next
        /// keystroke goes through <see cref="OnPreviewTextInput"/> at caret-end and the seed
        /// effectively disappears.
        /// </summary>
        private static void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is TextBox textBox) || !_states.TryGetValue(textBox, out var state))
                return;
            if (state.IsUpdating) return;

            string text = textBox.Text ?? string.Empty;

            state.IsUpdating = true;
            try
            {
                // Single-char in, formatter still empty → almost certainly the grid's type-to-edit
                // seed pattern. Route through InsertChar so caret lands after the typed digit
                // (matching what would have happened if PreviewTextInput had received it).
                if (text.Length == 1 && string.IsNullOrEmpty(state.Formatter.UnmaskedValue))
                {
                    state.Formatter.Format(string.Empty);
                    var (display, caret) = state.Formatter.InsertChar(text[0], 0);
                    if (textBox.Text != display)
                        textBox.Text = display;
                    textBox.CaretIndex = Math.Min(caret, display.Length);
                    // OnGotFocus may run after this (focus arrives after the grid's seed-char
                    // assignment) and would normally reselect the first editable region, which
                    // would clobber our caret placement. Tell it to skip just this once.
                    state.SuppressNextRegionSelect = true;
                }
                else
                {
                    // Generic external mutation (binding push, code-driven Text assignment).
                    // Re-format and park the caret at the end — non-typing scenarios don't care
                    // about caret placement, and this avoids stranding it past the new length.
                    string formatted = state.Formatter.Format(text);
                    if (textBox.Text != formatted)
                        textBox.Text = formatted;
                    textBox.CaretIndex = formatted.Length;
                }
                UpdateAttachedState(textBox, state.Formatter);
            }
            finally
            {
                state.IsUpdating = false;
            }
        }

        private static void OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!(sender is TextBox textBox) || !_states.TryGetValue(textBox, out var state))
                return;

            if (!e.DataObject.GetDataPresent(DataFormats.UnicodeText))
            {
                e.CancelCommand();
                return;
            }

            string pastedText = e.DataObject.GetData(DataFormats.UnicodeText) as string;
            if (string.IsNullOrEmpty(pastedText))
            {
                e.CancelCommand();
                return;
            }

            e.CancelCommand(); // Suppress default paste

            var (displayText, newCaret) = state.Formatter.Paste(
                pastedText, textBox.SelectionStart, textBox.SelectionLength);

            state.IsUpdating = true;
            textBox.Text = displayText;
            textBox.CaretIndex = Math.Min(newCaret, displayText.Length);
            UpdateAttachedState(textBox, state.Formatter);
            state.IsUpdating = false;
        }

        #endregion

        #region Helpers

        private static void UpdateAttachedState(TextBox textBox, IMaskFormatter formatter)
        {
            SetIsMaskComplete(textBox, formatter.IsMaskComplete);
            SetUnmaskedValue(textBox, formatter.UnmaskedValue);
        }

        #endregion
    }
}
