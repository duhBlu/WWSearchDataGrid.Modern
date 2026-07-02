using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// Dropdown editor. A lookless control whose template hosts a flat, real <see cref="ComboBox"/>
    /// (<c>PART_ComboBox</c>) inside its own chrome — so WPF keeps doing selection, popup, and
    /// keyboard work while the chrome owns the border (the inner combo never draws its own). The
    /// combo essentials are surfaced as forwarding DPs (bound to the inner combo in the template),
    /// and <see cref="IsDropDownOpen"/> plus <see cref="ComboBox"/> are exposed so the grid-side
    /// adapter can drive cell interaction without the control referencing the grid.
    /// </summary>
    [TemplatePart(Name = PartComboBox, Type = typeof(ComboBox))]
    public class WWComboBox : WWEditorBase
    {
        private const string PartComboBox = "PART_ComboBox";

        private ComboBox _comboBox;

        static WWComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWComboBox),
                new FrameworkPropertyMetadata(typeof(WWComboBox)));
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(WWComboBox), new PropertyMetadata(null));

        /// <summary>
        /// The item property used for display. Materialized as an <c>ItemTemplate</c> on the inner
        /// combo (not <c>DisplayMemberPath</c>): the flat combo template hand-places its selection
        /// ContentPresenter, so DisplayMemberPath's auto-walk never fires and the closed-state text
        /// would otherwise fall back to <c>ToString()</c>.
        /// </summary>
        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register(nameof(DisplayMemberPath), typeof(string), typeof(WWComboBox),
                new PropertyMetadata(null, OnDisplayMemberPathChanged));

        public static readonly DependencyProperty SelectedValuePathProperty =
            DependencyProperty.Register(nameof(SelectedValuePath), typeof(string), typeof(WWComboBox), new PropertyMetadata(null));

        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register(nameof(SelectedValue), typeof(object), typeof(WWComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(WWComboBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(WWComboBox),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register(nameof(IsEditable), typeof(bool), typeof(WWComboBox), new PropertyMetadata(false));

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
            _comboBox = GetTemplateChild(PartComboBox) as ComboBox;
            if (_comboBox == null) return;

            // Reuse the library's flat combo look (chevron / popup / item container style). Applied
            // explicitly and unconditionally so the inner combo can't inherit an ambient implicit
            // ComboBox style from the host app (which would draw a second border inside the chrome) —
            // an applied implicit style leaves Style non-null, so a "Style == null" guard would skip
            // ours. Resolved here (once the control is in the tree) so the ComponentResourceKey walks
            // through to the theme dictionary. The combo stays borderless; the chrome draws the border.
            if (TryFindResource(EditorThemeKeys.EditComboBox) is Style style)
                _comboBox.Style = style;

            UpdateItemTemplate();
        }

        private static void OnDisplayMemberPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((WWComboBox)d).UpdateItemTemplate();

        private void UpdateItemTemplate()
        {
            if (_comboBox == null) return;

            var path = DisplayMemberPath;
            if (string.IsNullOrEmpty(path))
            {
                _comboBox.ItemTemplate = null;
                return;
            }

            var template = new DataTemplate();
            var text = new FrameworkElementFactory(typeof(TextBlock));
            text.SetBinding(TextBlock.TextProperty, new Binding(path));
            template.VisualTree = text;
            _comboBox.ItemTemplate = template;
        }
    }
}
