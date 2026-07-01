using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// Boolean / three-state checkbox editor. A lookless control whose template hosts a
    /// <c>PART_CheckBox</c> — interactive in both display and edit: it toggles directly via its
    /// two-way binding, so a checkbox cell never has to enter edit mode. It renders flat with a
    /// transparent background (a checkbox is a glyph, not a bordered input), so the cell's own
    /// background / selection shows through and <see cref="WWBaseEdit.ShowBorder"/> has no visual
    /// effect. Derives <see cref="WWBaseEdit"/> for a uniform editor base type and focus-forwarding.
    /// </summary>
    /// <remarks>Grid-agnostic: it exposes its <see cref="CheckBox"/> and raises normal events; the
    /// adapter layers on cell interaction (arrow-exit, focus-on-edit).</remarks>
    [TemplatePart(Name = PartCheckBox, Type = typeof(CheckBox))]
    public class WWCheckEdit : WWBaseEdit
    {
        private const string PartCheckBox = "PART_CheckBox";

        private CheckBox _checkBox;

        static WWCheckEdit()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWCheckEdit),
                new FrameworkPropertyMetadata(typeof(WWCheckEdit)));
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

        /// <summary>The inner checkbox — the focus target and toggle element (null before template).</summary>
        public CheckBox CheckBox => _checkBox;

        /// <inheritdoc />
        protected override System.Windows.IInputElement FocusTarget => _checkBox;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _checkBox = GetTemplateChild(PartCheckBox) as CheckBox;

            // Reuse the library's themed checkbox look (box + check/indeterminate glyphs + the
            // DataGridCell read-only gate). Applied explicitly and unconditionally so the inner box
            // can't inherit an ambient implicit CheckBox style from the host app — an applied
            // implicit style leaves Style non-null, so a "Style == null" guard would skip ours.
            if (_checkBox != null && TryFindResource(EditorThemeKeys.DisplayCheckBox) is Style style)
                _checkBox.Style = style;
        }
    }
}
