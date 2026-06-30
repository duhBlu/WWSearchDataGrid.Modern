using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Boolean / three-state checkbox editor over <see cref="WWBaseEdit"/>. The checkbox is
    /// interactive in both display and edit — it toggles directly via its two-way binding, so a
    /// checkbox cell never has to enter edit mode. It derives <see cref="WWBaseEdit"/> for a uniform
    /// editor base type and focus-forwarding, but renders flat with a transparent background (a
    /// checkbox is a glyph, not a bordered input) so the cell's own background / selection shows
    /// through.
    /// </summary>
    /// <remarks>Grid-agnostic: it exposes its <see cref="CheckBox"/> and raises normal events; the
    /// adapter layers on cell interaction (arrow-exit, focus-on-edit).</remarks>
    public class WWCheckEdit : WWBaseEdit
    {
        private readonly CheckBox _checkBox;

        static WWCheckEdit()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWCheckEdit),
                new FrameworkPropertyMetadata(typeof(WWCheckEdit)));
        }

        public WWCheckEdit()
        {
            _checkBox = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            BindingOperations.SetBinding(_checkBox, ToggleButton.IsCheckedProperty, new Binding(nameof(IsChecked))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
            });
            BindingOperations.SetBinding(_checkBox, CheckBox.IsThreeStateProperty, new Binding(nameof(IsThreeState))
            {
                Source = this,
                Mode = BindingMode.OneWay,
            });
            EditContent = _checkBox;
        }

        /// <summary>The checkbox state. Nullable to carry the indeterminate (three-state) value.</summary>
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool?), typeof(WWCheckEdit),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>Whether the checkbox cycles through the indeterminate state.</summary>
        public static readonly DependencyProperty IsThreeStateProperty =
            DependencyProperty.Register(nameof(IsThreeState), typeof(bool), typeof(WWCheckEdit),
                new PropertyMetadata(false));

        public bool? IsChecked
        {
            get => (bool?)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        public bool IsThreeState
        {
            get => (bool)GetValue(IsThreeStateProperty);
            set => SetValue(IsThreeStateProperty, value);
        }

        /// <summary>The inner checkbox — the focus target and toggle element.</summary>
        public CheckBox CheckBox => _checkBox;

        /// <inheritdoc />
        protected override System.Windows.IInputElement FocusTarget => _checkBox;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            // Reuse the library's themed checkbox look (box + check/indeterminate glyphs + the
            // DataGridCell read-only gate). Resolved here — once the control is in the tree — so the
            // ComponentResourceKey walks through to the theme dictionary.
            if (_checkBox.Style == null && TryFindResource(EditSettingsThemeKeys.DisplayCheckBox) is Style style)
                _checkBox.Style = style;
        }
    }
}
