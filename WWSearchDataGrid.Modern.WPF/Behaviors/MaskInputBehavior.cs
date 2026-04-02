using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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
            public MaskFormatter Formatter { get; set; }
            public bool RegionEditMode { get; set; }
            public bool IsUpdating { get; set; }
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
            var formatter = new MaskFormatter(mask, promptChar);

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

                case Key.Tab:
                    if (!state.RegionEditMode) break; // Let Tab pass through

                    bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
                    var (regionIdx, _) = formatter.GetRegionAtCaret(textBox.CaretIndex);

                    int targetStart = shift
                        ? formatter.GetPrevEditableRegionStart(regionIdx)
                        : formatter.GetNextEditableRegionStart(regionIdx);

                    if (targetStart < 0)
                    {
                        // No more regions in that direction - let Tab exit the control
                        state.RegionEditMode = false;
                        break;
                    }

                    e.Handled = true;

                    // Find the region index at targetStart to get its bounds
                    var (targetRegionIdx, _2) = formatter.GetRegionAtCaret(targetStart);
                    var (rStart, rLength) = formatter.GetEditableRegionBounds(targetRegionIdx);

                    textBox.SelectionStart = rStart;
                    textBox.SelectionLength = Math.Max(rLength, 1);
                    break;
            }
        }

        private static void OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (!(sender is TextBox textBox) || !_states.TryGetValue(textBox, out var state))
                return;

            var formatter = state.Formatter;
            state.IsUpdating = true;

            if (string.IsNullOrEmpty(textBox.Text))
            {
                // Show the mask structure with prompt chars when empty
                string display = formatter.BuildDisplayText();
                textBox.Text = display;
            }
            else
            {
                // Re-format existing text through the mask
                string formatted = formatter.Format(textBox.Text);
                if (textBox.Text != formatted)
                    textBox.Text = formatted;
            }

            UpdateAttachedState(textBox, formatter);
            state.IsUpdating = false;

            // Select the first editable region
            int firstIdx = formatter.GetFirstEditableRegionIndex();
            if (firstIdx >= 0)
            {
                var (start, length) = formatter.GetEditableRegionBounds(firstIdx);
                textBox.SelectionStart = start;
                textBox.SelectionLength = Math.Max(length, 1);
                state.RegionEditMode = true;
            }
        }

        private static void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!(sender is TextBox textBox) || !_states.TryGetValue(textBox, out var state))
                return;

            state.RegionEditMode = false;

            // Finalize: fill empty required slots with defaults
            state.IsUpdating = true;
            string finalized = state.Formatter.Finalize();
            if (textBox.Text != finalized)
                textBox.Text = finalized;
            UpdateAttachedState(textBox, state.Formatter);
            state.IsUpdating = false;
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

        private static void UpdateAttachedState(TextBox textBox, MaskFormatter formatter)
        {
            SetIsMaskComplete(textBox, formatter.IsMaskComplete);
            SetUnmaskedValue(textBox, formatter.UnmaskedValue);
        }

        // Expose BuildDisplayText publicly for the behavior since it needs to call it
        // after ClearSelection. MaskFormatter.BuildDisplayText is internal, but we need it.
        // The Finalize method calls it internally, and Format/Parse also call it.
        // For ClearSelection, we chain with Format to rebuild.

        #endregion
    }
}
