using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// A custom TextBox with watermark text and clear button functionality
    /// </summary>
    [TemplatePart(Name = PART_TextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PART_ClearButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_ToggleButton, Type = typeof(Button))]
    [TemplatePart(Name = PART_Popup, Type = typeof(Popup))]
    [TemplatePart(Name = PART_ListBox, Type = typeof(ListBox))]
    public class SearchTextBox : Control
    {
        private const string PART_TextBox = "PART_TextBox";
        private const string PART_ClearButton = "PART_ClearButton";
        private const string PART_ToggleButton = "PART_ToggleButton";
        private const string PART_Popup = "PART_Popup";
        private const string PART_ListBox = "PART_ListBox";

        private TextBox _textBox;
        private Button _clearButton;
        private Button _toggleButton;
        private Popup _popup;
        private ListBox _listBox;
        private bool _isNavigating;
        private Timer _textChangeTimer;
        private string _pendingText;

        static SearchTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchTextBox), new FrameworkPropertyMetadata(typeof(SearchTextBox)));
        }

        public SearchTextBox()
        {
            Unloaded += OnControlUnloaded;
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

        /// <summary>
        /// The items source for searchable dropdown
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(SearchTextBox),
                new PropertyMetadata(null));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }


        /// <summary>
        /// Currently selected item from ItemsSource
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(
                nameof(SelectedItem),
                typeof(object),
                typeof(SearchTextBox),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedItemChanged));

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        /// <summary>
        /// Whether to maintain text when ItemsSource changes
        /// </summary>
        public static readonly DependencyProperty PreserveValueOnSourceChangeProperty =
            DependencyProperty.Register(
                nameof(PreserveValueOnSourceChange),
                typeof(bool),
                typeof(SearchTextBox),
                new PropertyMetadata(true));

        public bool PreserveValueOnSourceChange
        {
            get => (bool)GetValue(PreserveValueOnSourceChangeProperty);
            set => SetValue(PreserveValueOnSourceChangeProperty, value);
        }


        /// <summary>
        /// Whether to show the search/dropdown toggle button
        /// </summary>
        public static readonly DependencyProperty ShowToggleButtonProperty =
            DependencyProperty.Register(
                nameof(ShowToggleButton),
                typeof(bool),
                typeof(SearchTextBox),
                new PropertyMetadata(false));

        public bool ShowToggleButton
        {
            get => (bool)GetValue(ShowToggleButtonProperty);
            set => SetValue(ShowToggleButtonProperty, value);
        }

        /// <summary>
        /// Whether the popup is currently open
        /// </summary>
        public static readonly DependencyProperty IsPopupOpenProperty =
            DependencyProperty.Register(
                nameof(IsPopupOpen),
                typeof(bool),
                typeof(SearchTextBox),
                new PropertyMetadata(false, OnIsPopupOpenChanged));

        public bool IsPopupOpen
        {
            get => (bool)GetValue(IsPopupOpenProperty);
            set => SetValue(IsPopupOpenProperty, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to clear the text
        /// </summary>
        public ICommand ClearCommand => new RelayCommand(_ => ClearText());

        /// <summary>
        /// Command to toggle search mode
        /// </summary>
        public ICommand ToggleSearchCommand => new RelayCommand(_ => ToggleSearchMode(), _ => CanToggleSearch());

        private void ClearText()
        {
            // Stop timer and clear pending text
            _textChangeTimer?.Stop();
            _pendingText = string.Empty;

            Text = string.Empty;
            _textBox?.Focus();
        }

        private void ToggleSearchMode()
        {
            IsPopupOpen = !IsPopupOpen;
            _textBox?.Focus();
        }

        private bool CanToggleSearch()
        {
            return ItemsSource != null && ShowToggleButton;
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
                _textBox.KeyDown -= OnTextBoxKeyDown;
            }

            if (_clearButton != null)
            {
                _clearButton.Click -= OnClearButtonClick;
            }

            if (_toggleButton != null)
            {
                _toggleButton.Click -= OnToggleButtonClick;
            }

            if (_listBox != null)
            {
                _listBox.SelectionChanged -= OnListBoxSelectionChanged;
            }

            if (_popup != null)
            {
                _popup.LostFocus -= OnPopupLostFocus;
                _popup.PreviewKeyDown -= OnPopupPreviewKeyDown;
            }

            // Clean up timer when template is re-applied
            if (_textChangeTimer != null)
            {
                _textChangeTimer.Stop();
                _textChangeTimer.Elapsed -= OnTextChangeTimerElapsed;
                _textChangeTimer.Dispose();
                _textChangeTimer = null;
            }

            // Get template parts
            _textBox = GetTemplateChild(PART_TextBox) as TextBox;
            _clearButton = GetTemplateChild(PART_ClearButton) as Button;
            _toggleButton = GetTemplateChild(PART_ToggleButton) as Button;
            _popup = GetTemplateChild(PART_Popup) as Popup;
            _listBox = GetTemplateChild(PART_ListBox) as ListBox;

            // Hook up new events
            if (_textBox != null)
            {
                _textBox.TextChanged += OnTextBoxTextChanged;
                _textBox.GotFocus += OnTextBoxGotFocus;
                _textBox.LostFocus += OnTextBoxLostFocus;
                _textBox.PreviewKeyDown += OnTextBoxKeyDown;

                // Sync the text
                _textBox.Text = Text ?? string.Empty;
                UpdateHasText();
            }

            if (_clearButton != null)
            {
                _clearButton.Click += OnClearButtonClick;
            }

            if (_toggleButton != null)
            {
                _toggleButton.Click += OnToggleButtonClick;
            }

            if (_listBox != null)
            {
                _listBox.SelectionChanged += OnListBoxSelectionChanged;
            }

            if (_popup != null)
            {
                _popup.LostFocus += OnPopupLostFocus;
                _popup.PreviewKeyDown += OnPopupPreviewKeyDown;
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

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SearchTextBox)d;
            control.OnSelectedItemChanged(e.NewValue);
        }

        private static void OnIsPopupOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SearchTextBox)d;
            control.OnIsPopupOpenChanged((bool)e.NewValue);
        }

        private void OnTextChanged(string newText)
        {
            if (_textBox != null && _textBox.Text != newText)
            {
                // Stop timer for programmatic updates
                _textChangeTimer?.Stop();
                _pendingText = newText ?? string.Empty;
                _textBox.Text = newText ?? string.Empty;
            }

            UpdateHasText();

            // Raise the routed event
            RaiseEvent(new TextChangedEventArgs(TextChangedEvent, UndoAction.None));
        }

        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_textBox != null && !_isNavigating)
            {
                _pendingText = _textBox.Text;
                StartOrResetTextChangeTimer();
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
        }

        private void OnSelectedItemChanged(object selectedItem)
        {
            if (!IsPopupOpen && selectedItem != null)
            {
                SetCurrentValue(TextProperty, selectedItem?.ToString() ?? string.Empty);
            }
        }

        private void OnIsPopupOpenChanged(bool isOpen)
        {
            if (_popup != null)
            {
                _popup.IsOpen = isOpen;
            }
        }

        private void OnToggleButtonClick(object sender, RoutedEventArgs e)
        {
            IsPopupOpen = !IsPopupOpen;
            _textBox?.Focus();
        }

        private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (IsPopupOpen && _listBox?.SelectedItem != null)
                    {
                        SelectItem(_listBox.SelectedItem);
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    if (IsPopupOpen)
                    {
                        // Just close popup without reverting text
                        _listBox.SelectedIndex = -1;
                        IsPopupOpen = false;
                        e.Handled = true;
                    }
                    break;

                case Key.Tab:
                    if (IsPopupOpen)
                    {
                        IsPopupOpen = false;
                    }
                    break;

                case Key.Down:
                    // Auto-open popup if closed and there are items
                    if (!IsPopupOpen && ItemsSource != null && !string.IsNullOrEmpty(_textBox?.Text))
                    {
                        IsPopupOpen = true;
                        e.Handled = true;
                        return;
                    }

                    if (IsPopupOpen && _listBox != null && _listBox.Items.Count > 0)
                    {
                        _isNavigating = true;
                        var nextIndex = FindNextMatchingItem(_listBox.SelectedIndex, true);
                        if (nextIndex >= 0)
                        {
                            _listBox.SelectedIndex = nextIndex;
                            _listBox.ScrollIntoView(_listBox.SelectedItem);
                        }
                        _isNavigating = false;
                        e.Handled = true;
                    }
                    break;

                case Key.Up:
                    // Auto-open popup if closed and there are items
                    if (!IsPopupOpen && ItemsSource != null && !string.IsNullOrEmpty(_textBox?.Text))
                    {
                        IsPopupOpen = true;
                        e.Handled = true;
                        return;
                    }

                    if (IsPopupOpen && _listBox != null && _listBox.Items.Count > 0)
                    {
                        _isNavigating = true;
                        var prevIndex = FindNextMatchingItem(_listBox.SelectedIndex, false);
                        if (prevIndex >= 0)
                        {
                            _listBox.SelectedIndex = prevIndex;
                            _listBox.ScrollIntoView(_listBox.SelectedItem);
                        }
                        _isNavigating = false;
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only select item if we're not in navigation mode
            if (_listBox?.SelectedItem != null && e.AddedItems?.Count > 0 && !_isNavigating)
            {
                SelectItem(_listBox.SelectedItem);
            }
        }

        private void OnPopupLostFocus(object sender, RoutedEventArgs e)
        {
            if (!IsKeyboardFocusWithin)
            {
                IsPopupOpen = false;
            }
        }

        private void OnPopupPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                IsPopupOpen = false;
                e.Handled = true;
            }
        }

        #endregion

        #region Private Methods

        private void UpdateHasText()
        {
            HasSearchText = !string.IsNullOrEmpty(Text);
        }

        private void SelectItem(object item)
        {
            SetCurrentValue(SelectedItemProperty, item);
            SetCurrentValue(TextProperty, item?.ToString() ?? string.Empty);
            IsPopupOpen = false;

            _textBox?.Focus();
        }

        /// <summary>
        /// Finds the next or previous item that matches the current search text
        /// </summary>
        /// <param name="currentIndex">Current selected index</param>
        /// <param name="searchForward">True to search forward, false to search backward</param>
        /// <returns>Index of next matching item, or -1 if none found</returns>
        private int FindNextMatchingItem(int currentIndex, bool searchForward)
        {
            if (_listBox?.Items == null || _listBox.Items.Count == 0 || string.IsNullOrEmpty(_textBox?.Text))
                return -1;

            var searchText = _textBox.Text.ToLowerInvariant();
            var itemCount = _listBox.Items.Count;

            // Start from the next/previous position
            var startIndex = searchForward
                ? (currentIndex + 1) % itemCount
                : (currentIndex - 1 + itemCount) % itemCount;

            // If no current selection, start from beginning/end
            if (currentIndex < 0)
            {
                startIndex = searchForward ? 0 : itemCount - 1;
            }

            // Search through all items
            for (int i = 0; i < itemCount; i++)
            {
                var index = searchForward
                    ? (startIndex + i) % itemCount
                    : (startIndex - i + itemCount) % itemCount;

                var item = _listBox.Items[index];
                var itemText = item?.ToString()?.ToLowerInvariant() ?? string.Empty;

                if (itemText.Contains(searchText))
                {
                    return index;
                }
            }

            return -1; // No matching item found
        }

        #endregion

        #region Timer Methods

        /// <summary>
        /// Event handler for when control is unloaded - cleanup timer
        /// </summary>
        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            // Clean up timer when control is unloaded
            if (_textChangeTimer != null)
            {
                _textChangeTimer.Stop();
                _textChangeTimer.Elapsed -= OnTextChangeTimerElapsed;
                _textChangeTimer.Dispose();
                _textChangeTimer = null;
            }
        }

        /// <summary>
        /// Starts or restarts the debounce timer for text changes
        /// </summary>
        private void StartOrResetTextChangeTimer()
        {
            if (_textChangeTimer == null)
            {
                _textChangeTimer = new Timer(250) // 250ms delay matching ColumnSearchBox
                {
                    AutoReset = false
                };
                _textChangeTimer.Elapsed += OnTextChangeTimerElapsed;
            }

            _textChangeTimer.Stop();
            _textChangeTimer.Start();
        }

        /// <summary>
        /// Timer elapsed event handler - commits pending text to bound property
        /// </summary>
        private void OnTextChangeTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // Execute on UI thread
            Dispatcher.Invoke(() =>
            {
                try
                {
                    if (_pendingText != Text)
                    {
                        Text = _pendingText;
                    }
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in OnTextChangeTimerElapsed: {ex.Message}");
                }
            });
        }

        #endregion
    }
}