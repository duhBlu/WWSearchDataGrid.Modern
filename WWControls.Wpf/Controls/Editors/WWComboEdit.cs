using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace WWControls.Wpf
{
    /// <summary>
    /// Dropdown editor over <see cref="WWBaseEdit"/>. It wraps a real, flat <see cref="ComboBox"/> in
    /// the content host — so WPF keeps doing selection, popup, and keyboard work — while the base owns
    /// the border (the inner combo never draws its own). The combo essentials are surfaced as
    /// forwarding DPs, and <see cref="IsDropDownOpen"/> plus <see cref="ComboBox"/> are exposed so the
    /// grid-side adapter can drive cell interaction without the control referencing the grid.
    /// </summary>
    public class WWComboEdit : WWBaseEdit
    {
        private readonly ComboBox _comboBox;

        static WWComboEdit()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWComboEdit),
                new FrameworkPropertyMetadata(typeof(WWBaseEdit)));
        }

        public WWComboEdit()
        {
            _comboBox = new ComboBox
            {
                // Transparent + borderless so only WWBaseEdit's chrome shows; the inner combo's own
                // border is suppressed via the (still-live) host-context flag the other editors use.
                Background = Brushes.Transparent,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Left,
            };
            BaseEditSettings.SetShowEditorBorder(_comboBox, false);

            BindingOperations.SetBinding(_comboBox, ItemsControl.ItemsSourceProperty,
                new Binding(nameof(ItemsSource)) { Source = this, Mode = BindingMode.OneWay });
            BindingOperations.SetBinding(_comboBox, Selector.SelectedValuePathProperty,
                new Binding(nameof(SelectedValuePath)) { Source = this, Mode = BindingMode.OneWay });
            BindingOperations.SetBinding(_comboBox, Selector.SelectedValueProperty,
                new Binding(nameof(SelectedValue)) { Source = this, Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(_comboBox, Selector.SelectedItemProperty,
                new Binding(nameof(SelectedItem)) { Source = this, Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(_comboBox, ComboBox.IsDropDownOpenProperty,
                new Binding(nameof(IsDropDownOpen)) { Source = this, Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(_comboBox, ComboBox.IsEditableProperty,
                new Binding(nameof(IsEditable)) { Source = this, Mode = BindingMode.OneWay });

            EditContent = _comboBox;
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(WWComboEdit), new PropertyMetadata(null));

        /// <summary>
        /// The item property used for display. Materialized as an <c>ItemTemplate</c> on the inner
        /// combo (not <c>DisplayMemberPath</c>): the flat combo template hand-places its selection
        /// ContentPresenter, so DisplayMemberPath's auto-walk never fires and the closed-state text
        /// would otherwise fall back to <c>ToString()</c>.
        /// </summary>
        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(WWComboEdit),
                new PropertyMetadata(null, OnDisplayMemberPathChanged));

        public static readonly DependencyProperty SelectedValuePathProperty =
            DependencyProperty.Register(nameof(SelectedValuePath), typeof(string), typeof(WWComboEdit), new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register(nameof(SelectedValue), typeof(object), typeof(WWComboEdit),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(WWComboEdit),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(WWComboEdit),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(WWComboEdit), new PropertyMetadata(false));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public string DisplayMemberPath
        {
            get => (string)GetValue(DisplayMemberPathProperty);
            set => SetValue(DisplayMemberPathProperty, value);
        }

        public string SelectedValuePath
        {
            get => (string)GetValue(SelectedValuePathProperty);
            set => SetValue(SelectedValuePathProperty, value);
        }

        public object SelectedValue
        {
            get => GetValue(SelectedValueProperty);
            set => SetValue(SelectedValueProperty, value);
        }

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        public bool IsEditable
        {
            get => (bool)GetValue(IsEditableProperty);
            set => SetValue(IsEditableProperty, value);
        }

        /// <summary>The inner combo — exposed so the adapter can drive grid-cell keyboard interaction.</summary>
        public ComboBox ComboBox => _comboBox;

        /// <inheritdoc />
        protected override System.Windows.IInputElement FocusTarget => _comboBox;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            // Reuse the library's flat combo look (border / chevron / popup / item container style).
            if (_comboBox.Style == null && TryFindResource(EditSettingsThemeKeys.EditComboBox) is Style style)
                _comboBox.Style = style;
        }

        private static void OnDisplayMemberPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (WWComboEdit)d;
            var path = e.NewValue as string;
            if (string.IsNullOrEmpty(path))
            {
                self._comboBox.ItemTemplate = null;
                return;
            }

            var template = new DataTemplate();
            var text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetBinding(TextBlock.TextProperty, new Binding(path));
            template.VisualTree = text;
            self._comboBox.ItemTemplate = template;
        }
    }
}
