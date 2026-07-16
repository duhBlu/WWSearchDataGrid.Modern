using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WWControls.Wpf.Controls.Editors
{
    /// <summary>
    /// Boolean / three-state checkbox editor. A lookless control whose template hosts a
    /// <c>PART_CheckBox</c> — interactive in both display and edit: it toggles directly via its
    /// two-way binding, so a checkbox cell never has to enter edit mode. It renders flat with a
    /// transparent background (a checkbox is a glyph, not a bordered input), so the cell's own
    /// background / selection shows through and <see cref="WWEditorBase.ShowBorder"/> has no visual
    /// effect. Derives <see cref="WWEditorBase"/> for a uniform editor base type and focus-forwarding.
    /// </summary>
    /// <remarks>Grid-agnostic: it exposes its <see cref="CheckBox"/> and raises normal events; the
    /// adapter layers on cell interaction (arrow-exit, focus-on-edit).</remarks>
    [TemplatePart(Name = PartCheckBox, Type = typeof(CheckBox))]
    public class WWCheckBox : WWEditorBase
    {
        private const string PartCheckBox = "PART_CheckBox";

        private CheckBox _checkBox;

        static WWCheckBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWCheckBox),
                new FrameworkPropertyMetadata(typeof(WWCheckBox)));
        }

        /// <summary>The checkbox state. Nullable to carry the indeterminate (three-state) value.</summary>
        public static readonly DependencyProperty IsCheckedProperty =
            DependencyProperty.Register(nameof(IsChecked), typeof(bool?), typeof(WWCheckBox),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>Whether the checkbox cycles through the indeterminate state.</summary>
        public static readonly DependencyProperty IsThreeStateProperty =
            DependencyProperty.Register(nameof(IsThreeState), typeof(bool), typeof(WWCheckBox),
                new PropertyMetadata(false));

        /// <summary>When the inner checkbox registers a click: on mouse release (default), press, or hover.</summary>
        public static readonly DependencyProperty ClickModeProperty =
            DependencyProperty.Register(nameof(ClickMode), typeof(ClickMode), typeof(WWCheckBox),
                new PropertyMetadata(ClickMode.Release, OnEffectiveClickModeInputChanged));

        /// <summary>
        /// Modifier key(s) that must be held for <see cref="ClickMode.Hover"/> to toggle.
        /// <see cref="ModifierKeys.None"/> (default) hovers unguarded. When set, an unmodified
        /// hover does nothing — the control toggles when the pointer is over it with the modifier
        /// held (once per hover), and a normal click still toggles. Ignored outside Hover mode.
        /// </summary>
        public static readonly DependencyProperty HoverModifierProperty =
            DependencyProperty.Register(nameof(HoverModifier), typeof(ModifierKeys), typeof(WWCheckBox),
                new PropertyMetadata(ModifierKeys.None, OnEffectiveClickModeInputChanged));

        private static readonly DependencyPropertyKey EffectiveClickModePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(EffectiveClickMode), typeof(ClickMode), typeof(WWCheckBox),
                new PropertyMetadata(ClickMode.Release));

        /// <summary>
        /// The click mode the inner checkbox actually runs — <see cref="ClickMode"/>, except
        /// modifier-gated hover drops to <see cref="ClickMode.Release"/> so the native
        /// hover-toggle stays off and the control applies the gated toggle itself.
        /// </summary>
        public static readonly DependencyProperty EffectiveClickModeProperty =
            EffectiveClickModePropertyKey.DependencyProperty;

        private static void OnEffectiveClickModeInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var checkBox = (WWCheckBox)d;
            var mode = checkBox.ClickMode;
            if (mode == ClickMode.Hover && checkBox.HoverModifier != ModifierKeys.None)
                mode = ClickMode.Release;
            checkBox.SetValue(EffectiveClickModePropertyKey, mode);
        }

        public bool? IsChecked
        {
            get => (bool?)GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        /// <summary>The edited value is the checkbox state, carried on <see cref="IsChecked"/>.</summary>
        public override object EditedValue => IsChecked;

        /// <summary>Pushes the <see cref="IsChecked"/> binding to source (the checkbox's value binding).</summary>
        public override void CommitValueToSource()
            => GetBindingExpression(IsCheckedProperty)?.UpdateSource();

        public bool IsThreeState
        {
            get => (bool)GetValue(IsThreeStateProperty);
            set => SetValue(IsThreeStateProperty, value);
        }

        public ClickMode ClickMode
        {
            get => (ClickMode)GetValue(ClickModeProperty);
            set => SetValue(ClickModeProperty, value);
        }

        public ModifierKeys HoverModifier
        {
            get => (ModifierKeys)GetValue(HoverModifierProperty);
            set => SetValue(HoverModifierProperty, value);
        }

        public ClickMode EffectiveClickMode => (ClickMode)GetValue(EffectiveClickModeProperty);

        /// <summary>The inner checkbox — the focus target and toggle element (null before template).</summary>
        public CheckBox CheckBox => _checkBox;

        /// <inheritdoc />
        protected override System.Windows.IInputElement FocusTarget => _checkBox;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _checkBox = GetTemplateChild(PartCheckBox) as CheckBox;
        }

        /// <summary>
        /// Advances <see cref="IsChecked"/> one step — the same cycle as
        /// <see cref="System.Windows.Controls.Primitives.ToggleButton"/>: false / indeterminate →
        /// true, true → indeterminate (three-state) or false. No-op while read-only or disabled.
        /// </summary>
        internal void Toggle()
        {
            if (IsReadOnly || !IsEnabled)
                return;
            IsChecked = IsChecked == true ? (IsThreeState ? (bool?)null : false)
                      : IsChecked == null ? false
                      : true;
        }

        /// <summary>
        /// Space toggles the box when the control itself holds keyboard focus — the keyboard path
        /// for a hostless editor (e.g. a property-grid row) where the WWCheckBox is the tab stop and
        /// its inner CheckBox is non-focusable. In a grid cell the cell owns focus and the key event
        /// never routes through the WWCheckBox, so this leaves grid behavior untouched.
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled && e.Key == Key.Space)
            {
                Toggle();
                e.Handled = true;
            }
        }

        // ── Modifier-gated hover ────────────────────────────────────────────
        // With HoverModifier set, the inner checkbox runs Release (EffectiveClickMode), so plain
        // hovering is inert; the control itself toggles once per hover while the modifier is held.
        // MouseMove (not just MouseEnter) participates so pressing the modifier after the pointer
        // is already over the box still registers on the next pointer movement.

        /// <summary>True once the current hover has toggled; re-arms when the pointer leaves.</summary>
        private bool _hoverToggled;

        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            TryHoverToggle();
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);
            TryHoverToggle();
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverToggled = false;
        }

        private void TryHoverToggle()
        {
            if (_hoverToggled || ClickMode != ClickMode.Hover || HoverModifier == ModifierKeys.None)
                return;
            if (IsReadOnly || !IsEnabled)
                return;
            if ((Keyboard.Modifiers & HoverModifier) != HoverModifier)
                return;

            _hoverToggled = true;
            Toggle();
        }
    }
}
