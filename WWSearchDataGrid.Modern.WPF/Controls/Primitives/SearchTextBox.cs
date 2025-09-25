using System.Collections;
using System.ComponentModel;
using System.Linq;
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
        private string _originalText;

        static SearchTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchTextBox), new FrameworkPropertyMetadata(typeof(SearchTextBox)));
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
                new PropertyMetadata(null, OnItemsSourceChanged));

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
                _listBox.MouseDoubleClick -= OnListBoxMouseDoubleClick;
            }

            if (_popup != null)
            {
                _popup.LostFocus -= OnPopupLostFocus;
                _popup.PreviewKeyDown -= OnPopupPreviewKeyDown;
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
                _textBox.KeyDown += OnTextBoxKeyDown;

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
                _listBox.MouseDoubleClick += OnListBoxMouseDoubleClick;
                UpdateFilteredItems();
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

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SearchTextBox)d;
            control.OnItemsSourceChanged();
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

                if (IsPopupOpen)
                {
                    UpdateFilteredItems();
                }
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

        private void OnItemsSourceChanged()
        {
            UpdateFilteredItems();
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

            if (isOpen)
            {
                UpdateFilteredItems();
            }
        }

        private void OnToggleButtonClick(object sender, RoutedEventArgs e)
        {
            IsPopupOpen = !IsPopupOpen;
        }

        private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsPopupOpen) return;

            switch (e.Key)
            {
                case Key.Enter:
                    if (_listBox?.SelectedItem != null)
                    {
                        SelectItem(_listBox.SelectedItem);
                        e.Handled = true;
                    }
                    break;

                case Key.Escape:
                    IsPopupOpen = false;
                    e.Handled = true;
                    break;

                case Key.Tab:
                    IsPopupOpen = false;
                    break;

                case Key.Down:
                    if (_listBox != null && _listBox.Items.Count > 0)
                    {
                        if (_listBox.SelectedIndex < _listBox.Items.Count - 1)
                            _listBox.SelectedIndex++;
                        else
                            _listBox.SelectedIndex = 0;
                        _listBox.ScrollIntoView(_listBox.SelectedItem);
                        e.Handled = true;
                    }
                    break;

                case Key.Up:
                    if (_listBox != null && _listBox.Items.Count > 0)
                    {
                        if (_listBox.SelectedIndex > 0)
                            _listBox.SelectedIndex--;
                        else
                            _listBox.SelectedIndex = _listBox.Items.Count - 1;
                        _listBox.ScrollIntoView(_listBox.SelectedItem);
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void OnListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_listBox?.SelectedItem != null && e.AddedItems?.Count > 0)
            {
                SelectItem(_listBox.SelectedItem);
            }
        }

        private void OnListBoxMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_listBox?.SelectedItem != null)
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

        private void UpdateFilteredItems()
        {
            if (_listBox == null || ItemsSource == null)
                return;

            var view = CollectionViewSource.GetDefaultView(ItemsSource);

            if (IsPopupOpen && !string.IsNullOrEmpty(_textBox?.Text))
            {
                var searchText = _textBox.Text.ToLowerInvariant();
                view.Filter = item => item?.ToString()?.ToLowerInvariant().Contains(searchText) == true;
            }
            else
            {
                view.Filter = null;
            }

            _listBox.ItemsSource = view;
        }

        private void SelectItem(object item)
        {
            SetCurrentValue(SelectedItemProperty, item);
            SetCurrentValue(TextProperty, item?.ToString() ?? string.Empty);
            IsPopupOpen = false;

            _textBox?.Focus();
        }

        #endregion
    }
}