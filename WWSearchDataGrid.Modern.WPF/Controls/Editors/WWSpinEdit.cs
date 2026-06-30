using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Spinner numeric editor over <see cref="WWBaseEdit"/>: a right-aligned numeric TextBox in the
    /// content host plus up/down repeat buttons in the chrome's decoration-button slot. The base owns
    /// the border, so the spinner reads as one bordered input in the edit form and flat in a cell —
    /// retiring the old composite "outer border" workaround.
    /// </summary>
    /// <remarks>
    /// Grid-agnostic. It owns numeric concerns — digit filtering, Ctrl+Up/Down (and Ctrl+Shift for
    /// the large step) increment, button increment, Min/Max clamping — and exposes its TextBox via
    /// <see cref="IEditTextBoxProvider"/>. Bare arrow keys are left unhandled so the grid-side host
    /// can drive cell navigation; <see cref="WWBaseEdit.Value"/> carries the text.
    /// </remarks>
    public class WWSpinEdit : WWBaseEdit, IEditTextBoxProvider
    {
        private const string ChevronUp = "";   // Segoe Fluent Icons ChevronUp
        private const string ChevronDown = ""; // Segoe Fluent Icons ChevronDown

        private readonly TextBox _textBox;
        private readonly RepeatButton _upButton;
        private readonly RepeatButton _downButton;

        static WWSpinEdit()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWSpinEdit),
                new FrameworkPropertyMetadata(typeof(WWBaseEdit)));
        }

        public WWSpinEdit()
        {
            _textBox = new TextBox
            {
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                Padding = new Thickness(0),
                TextAlignment = TextAlignment.Right,
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            BindingOperations.SetBinding(_textBox, TextBox.TextProperty, new Binding(nameof(Value))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            });
            BindingOperations.SetBinding(_textBox, TextBox.IsReadOnlyProperty, new Binding(nameof(IsReadOnly))
            {
                Source = this,
                Mode = BindingMode.OneWay,
            });
            BindingOperations.SetBinding(_textBox, Control.ForegroundProperty, new Binding(nameof(Foreground))
            {
                Source = this,
                Mode = BindingMode.OneWay,
            });
            _textBox.PreviewTextInput += OnPreviewTextInput;
            _textBox.PreviewKeyDown += OnTextBoxPreviewKeyDown;
            EditContent = _textBox;

            _upButton = new RepeatButton { Content = ChevronUp, Focusable = false };
            _downButton = new RepeatButton { Content = ChevronDown, Focusable = false };
            _upButton.Click += (_, _) => Step(+1, large: false);
            _downButton.Click += (_, _) => Step(-1, large: false);

            var buttons = new Grid();
            buttons.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            buttons.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(_upButton, 0);
            Grid.SetRow(_downButton, 1);
            buttons.Children.Add(_upButton);
            buttons.Children.Add(_downButton);

            ButtonContent = buttons;
            ShowButtons = true;
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register(nameof(Minimum), typeof(double?), typeof(WWSpinEdit), new PropertyMetadata(null));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double?), typeof(WWSpinEdit), new PropertyMetadata(null));

        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register(nameof(Increment), typeof(double), typeof(WWSpinEdit), new PropertyMetadata(1.0));

        /// <summary>Step applied by the Ctrl+Shift+Up / Ctrl+Shift+Down gesture (the "large" jump).</summary>
        public static readonly DependencyProperty LargeIncrementProperty =
            DependencyProperty.Register(nameof(LargeIncrement), typeof(double), typeof(WWSpinEdit), new PropertyMetadata(10.0));

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
            if (TryFindResource(EditSettingsThemeKeys.SpinButton) is Style spinStyle)
            {
                _upButton.Style ??= spinStyle;
                _downButton.Style ??= spinStyle;
            }
        }

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
