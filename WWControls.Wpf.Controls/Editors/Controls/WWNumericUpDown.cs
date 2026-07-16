using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace WWControls.Wpf.Controls.Editors
{
    /// <summary>
    /// Spinner numeric editor. A lookless control whose template hosts a right-aligned
    /// <c>PART_TextBox</c> plus a <c>PART_UpButton</c> / <c>PART_DownButton</c> spinner column inside
    /// its own chrome (border, focus accent). <see cref="WWEditorBase.Value"/> carries the text (the
    /// inner TextBox two-way-binds to it in the template), and the spinner reads as one bordered
    /// input on a form, flat in a cell.
    /// </summary>
    /// <remarks>
    /// Grid-agnostic. It owns numeric concerns — digit filtering, Ctrl+Up/Down (and Ctrl+Shift for
    /// the large step) increment, button increment, Min/Max clamping — and exposes its TextBox via
    /// <see cref="IEditTextBoxProvider"/>. Bare arrow keys are left unhandled so the grid-side host
    /// can drive cell navigation; <see cref="WWEditorBase.Value"/> carries the text.
    /// </remarks>
    [TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PartUpButton, Type = typeof(RepeatButton))]
    [TemplatePart(Name = PartDownButton, Type = typeof(RepeatButton))]
    public class WWNumericUpDown : WWEditorBase, IEditTextBoxProvider
    {

        private const string PartTextBox = "PART_TextBox";
        private const string PartUpButton = "PART_UpButton";
        private const string PartDownButton = "PART_DownButton";

        private TextBox _textBox;
        private RepeatButton _upButton;
        private RepeatButton _downButton;

        static WWNumericUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWNumericUpDown),
                new FrameworkPropertyMetadata(typeof(WWNumericUpDown)));
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double?), typeof(WWNumericUpDown), new PropertyMetadata(null));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double?), typeof(WWNumericUpDown), new PropertyMetadata(null));

        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register(nameof(Increment), typeof(double), typeof(WWNumericUpDown), new PropertyMetadata(1.0));

        /// <summary>Step applied by the Ctrl+Shift+Up / Ctrl+Shift+Down gesture (the "large" jump).</summary>
        public static readonly DependencyProperty LargeIncrementProperty =
            DependencyProperty.Register(nameof(LargeIncrement), typeof(double), typeof(WWNumericUpDown), new PropertyMetadata(10.0));

        public double? Minimum
        {
            get => (double?)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public double? Maximum
        {
            get => (double?)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double Increment
        {
            get => (double)GetValue(IncrementProperty);
            set => SetValue(IncrementProperty, value);
        }

        public double LargeIncrement
        {
            get => (double)GetValue(LargeIncrementProperty);
            set => SetValue(LargeIncrementProperty, value);
        }

        /// <inheritdoc />
        public TextBox EditTextBox => _textBox;

        /// <inheritdoc />
        protected override System.Windows.IInputElement FocusTarget => _textBox;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_textBox != null)
            {
                _textBox.PreviewTextInput -= OnPreviewTextInput;
                _textBox.PreviewKeyDown -= OnTextBoxPreviewKeyDown;
            }
            if (_upButton != null) _upButton.Click -= OnUpButtonClick;
            if (_downButton != null) _downButton.Click -= OnDownButtonClick;

            _textBox = GetTemplateChild(PartTextBox) as TextBox;
            _upButton = GetTemplateChild(PartUpButton) as RepeatButton;
            _downButton = GetTemplateChild(PartDownButton) as RepeatButton;

            if (_textBox != null)
            {
                _textBox.PreviewTextInput += OnPreviewTextInput;
                _textBox.PreviewKeyDown += OnTextBoxPreviewKeyDown;
            }

            if (_upButton != null) _upButton.Click += OnUpButtonClick;
            if (_downButton != null) _downButton.Click += OnDownButtonClick;
        }

        private void OnUpButtonClick(object sender, RoutedEventArgs e) => Step(+1, large: false);

        private void OnDownButtonClick(object sender, RoutedEventArgs e) => Step(-1, large: false);

        // Numeric-only entry: digits, decimal point / group separator, sign. Binding-time conversion
        // does the actual parse against the column's runtime type (int rejects "1.5", etc.).
        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (var ch in e.Text)
            {
                if (!char.IsDigit(ch) && ch != '.' && ch != ',' && ch != '-' && ch != '+')
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        // Increment requires Ctrl so bare Up/Down stay free for the grid host's row navigation;
        // Ctrl+Shift uses the large step. Anything else falls through unhandled.
        private void OnTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Up && e.Key != Key.Down) return;
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control) return;

            bool large = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
            Step(e.Key == Key.Up ? +1 : -1, large);
            e.Handled = true;
        }

        private void Step(double sign, bool large)
        {
            var delta = (large ? LargeIncrement : Increment) * sign;
            if (!double.TryParse(_textBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var current))
                current = 0;

            var next = current + delta;
            if (Minimum.HasValue) next = Math.Max(Minimum.Value, next);
            if (Maximum.HasValue) next = Math.Min(Maximum.Value, next);

            _textBox.Text = next.ToString(CultureInfo.CurrentCulture);
            _textBox.SelectAll();
            _textBox.Focus();
        }
    }
}