using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// A custom TextBox with watermark text and clear button functionality
    /// </summary>
    [TemplatePart(Name = PART_TextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PART_ClearButton, Type = typeof(Button))]
    public class SearchTextBox : Control
    {
        private const string PART_TextBox = "PART_TextBox";
        private const string PART_ClearButton = "PART_ClearButton";

        private TextBox _textBox;
        private Button _clearButton;

        static SearchTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchTextBox), new FrameworkPropertyMetadata(typeof(SearchTextBox)));
        }

        public SearchTextBox()
        {
            ClearCommand = new RelayCommand(_ => ClearText());
        }

        #region Dependency Properties

        /// <summary>
        /// The text content of the SearchTextBox
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(SearchTextBox),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnTextChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// The watermark text to display when the TextBox is empty
        /// </summary>
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register(
                nameof(Watermark),
                typeof(string),
                typeof(SearchTextBox),
                new PropertyMetadata(string.Empty));

        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        /// <summary>
        /// Whether to show the clear button
        /// </summary>
        public static readonly DependencyProperty ShowClearButtonProperty =
            DependencyProperty.Register(
                nameof(ShowClearButton),
                typeof(bool),
                typeof(SearchTextBox),
                new PropertyMetadata(true));

        public bool ShowClearButton
        {
            get => (bool)GetValue(ShowClearButtonProperty);
            set => SetValue(ShowClearButtonProperty, value);
        }

        /// <summary>
        /// Whether to show the search icon
        /// </summary>
        public static readonly DependencyProperty ShowSearchIconProperty =
            DependencyProperty.Register(
                nameof(ShowSearchIcon),
                typeof(bool),
                typeof(SearchTextBox),
                new PropertyMetadata(true));

        public bool ShowSearchIcon
        {
            get => (bool)GetValue(ShowSearchIconProperty);
            set => SetValue(ShowSearchIconProperty, value);
        }

        /// <summary>
        /// The corner radius of the control
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(SearchTextBox),
                new PropertyMetadata(new CornerRadius(4)));

        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Whether the search control has text
        /// </summary>
        public static readonly DependencyProperty HasSearchTextProperty =
            DependencyProperty.Register(
                nameof(HasSearchText),
                typeof(bool),
                typeof(SearchTextBox),
                new PropertyMetadata(false));

        public bool HasSearchText
        {
            get => (bool)GetValue(HasSearchTextProperty);
            set => SetValue(HasSearchTextProperty, value);
        }

        /// <summary>
        /// Whether the search control is focused
        /// </summary>
        public static readonly DependencyProperty IsSearchFocusedProperty =
            DependencyProperty.Register(
                nameof(IsSearchFocused),
                typeof(bool),
                typeof(SearchTextBox),
                new PropertyMetadata(false));

        public bool IsSearchFocused
        {
            get => (bool)GetValue(IsSearchFocusedProperty);
            set => SetValue(IsSearchFocusedProperty, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to clear the text
        /// </summary>
        public ICommand ClearCommand { get; }

        private void ClearText()
        {
            Text = string.Empty;
            _textBox?.Focus();
        }

        private bool CanClearText()
        {
            return HasSearchText;
        }

        #endregion

        #region Events

        /// <summary>
        /// Routed event for text changes
        /// </summary>
        public static readonly RoutedEvent TextChangedEvent = EventManager.RegisterRoutedEvent(
            "TextChanged", RoutingStrategy.Bubble, typeof(TextChangedEventHandler), typeof(SearchTextBox));

        /// <summary>
        /// Occurs when the text changes
        /// </summary>
        public event TextChangedEventHandler TextChanged
        {
            add { AddHandler(TextChangedEvent, value); }
            remove { RemoveHandler(TextChangedEvent, value); }
        }

        /// <summary>
        /// Occurs when the clear button is clicked
        /// </summary>
        public event RoutedEventHandler Cleared;

        #endregion

        #region Overrides

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Unhook old events
            if (_textBox != null)
            {
                _textBox.TextChanged -= OnTextBoxTextChanged;
                _textBox.GotFocus -= OnTextBoxGotFocus;
                _textBox.LostFocus -= OnTextBoxLostFocus;
            }

            if (_clearButton != null)
            {
                _clearButton.Click -= OnClearButtonClick;
            }

            // Get template parts
            _textBox = GetTemplateChild(PART_TextBox) as TextBox;
            _clearButton = GetTemplateChild(PART_ClearButton) as Button;

            // Hook up new events
            if (_textBox != null)
            {
                _textBox.TextChanged += OnTextBoxTextChanged;
                _textBox.GotFocus += OnTextBoxGotFocus;
                _textBox.LostFocus += OnTextBoxLostFocus;

                // Sync the text
                _textBox.Text = Text ?? string.Empty;
                UpdateHasText();
            }

            if (_clearButton != null)
            {
                _clearButton.Click += OnClearButtonClick;
            }
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            // Only focus the TextBox if no child element already has focus
            if (_textBox != null && !IsKeyboardFocusWithin)
            {
                _textBox.Focus();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.Escape && !string.IsNullOrEmpty(Text))
            {
                ClearText();
                e.Handled = true;
            }
        }

        #endregion

        #region Event Handlers

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SearchTextBox)d;
            control.OnTextChanged((string)e.NewValue);
        }

        private void OnTextChanged(string newText)
        {
            if (_textBox != null && _textBox.Text != newText)
            {
                _textBox.Text = newText ?? string.Empty;
            }

            UpdateHasText();

            // Raise the routed event
            RaiseEvent(new TextChangedEventArgs(TextChangedEvent, UndoAction.None));
        }

        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_textBox != null)
            {
                Text = _textBox.Text;
            }

            // Forward the event args from the internal TextBox
            RaiseEvent(new TextChangedEventArgs(TextChangedEvent, UndoAction.None)
            {
                Source = this
            });
        }

        private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            IsSearchFocused = true;
        }

        private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            IsSearchFocused = false;
        }

        private void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            ClearText();
            Cleared?.Invoke(this, e);
        }

        #endregion

        #region Private Methods

        private void UpdateHasText()
        {
            HasSearchText = !string.IsNullOrEmpty(Text);
        }

        #endregion
    }
}