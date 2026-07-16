using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WWControls.Core.Display;
using WWControls.Wpf.Behaviors;

namespace WWControls.Wpf.Editors
{
    /// <summary>Which side of the text a <see cref="WWTextBox"/> renders its optional <see cref="WWTextBox.Glyph"/> on.</summary>
    public enum GlyphPlacement
    {
        /// <summary>Leading edge (left of the text in a left-to-right layout).</summary>
        Left,

        /// <summary>Trailing edge (right of the text, before the clear button).</summary>
        Right
    }

    /// <summary>
    /// Exposes an editor's underlying <see cref="TextBox"/> so a host can drive the caret or read
    /// the selection without reaching into the control's visual tree. The grid-side editor host
    /// uses this for mouse-click caret placement; the editor itself stays unaware of the host.
    /// </summary>
    public interface IEditTextBoxProvider
    {
        /// <summary>The control's text-input element, or null before its template/content is realized.</summary>
        TextBox EditTextBox { get; }
    }

    /// <summary>
    /// Plain text editor. A lookless control whose template hosts a <c>PART_TextBox</c> inside its
    /// own chrome (border, background, focus accent). <see cref="WWEditorBase.Value"/> carries the
    /// text — the inner TextBox two-way-binds to it (wired in code so <see cref="UpdateDelay"/> can
    /// debounce the push). An optional <see cref="Mask"/> / <see cref="MaskType"/> routes keystrokes
    /// through <see cref="MaskInputBehavior"/> for keystroke validation, region-aware Tab navigation,
    /// mask-aware paste, and finalize-on-blur.
    /// </summary>
    /// <remarks>
    /// On top of the raw text surface it layers a set of inline affordances, each self-hiding when
    /// unconfigured: a <see cref="WWEditorBase.Watermark"/> shown while empty, an optional
    /// <see cref="WWEditorBase.ShowClearButton"/> clear button shown while non-empty, and an optional
    /// <see cref="Glyph"/> icon on either edge (<see cref="GlyphPlacement"/>). When a mask is active
    /// it surfaces the masked-input state: a settable <see cref="PromptChar"/> and the read-only
    /// <see cref="UnmaskedValue"/> / <see cref="IsMaskComplete"/> mirrored from the mask engine. The
    /// control has no knowledge of any grid: it raises normal input events and surfaces its text
    /// element via <see cref="IEditTextBoxProvider"/>.
    /// </remarks>
    [TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PartClearButton, Type = typeof(Button))]
    public class WWTextBox : WWEditorBase, IEditTextBoxProvider
    {
        private const string PartTextBox = "PART_TextBox";
        private const string PartClearButton = "PART_ClearButton";

        // Observe the mask engine's attached-property writes on the inner TextBox so the control can
        // mirror them onto its own read-only UnmaskedValue / IsMaskComplete DPs.
        private static readonly DependencyPropertyDescriptor UnmaskedValueDescriptor =
            DependencyPropertyDescriptor.FromProperty(MaskInputBehavior.UnmaskedValueProperty, typeof(TextBox));
        private static readonly DependencyPropertyDescriptor MaskCompleteDescriptor =
            DependencyPropertyDescriptor.FromProperty(MaskInputBehavior.IsMaskCompleteProperty, typeof(TextBox));

        private TextBox _textBox;
        private Button _clearButton;

        static WWTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWTextBox),
                new FrameworkPropertyMetadata(typeof(WWTextBox)));
        }

        public WWTextBox()
        {
            // Re-assert the mask once the control is in the tree — matches the mask behavior's
            // expectation of a realized TextBox; idempotent with the OnApplyTemplate application.
            Loaded += (_, _) => ApplyMask();
        }

        /// <summary>
        /// Optional mask pattern. When set, the inner TextBox is wired through
        /// <see cref="MaskInputBehavior"/>; the pattern grammar is determined by <see cref="MaskType"/>.
        /// </summary>
        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register(nameof(Mask), typeof(string), typeof(WWTextBox),
                new PropertyMetadata(null, OnMaskChanged));

        /// <summary>How the <see cref="Mask"/> pattern is interpreted. Default <see cref="MaskType.Simple"/>.</summary>
        public static readonly DependencyProperty MaskTypeProperty =
            DependencyProperty.Register(nameof(MaskType), typeof(MaskType), typeof(WWTextBox),
                new PropertyMetadata(MaskType.Simple, OnMaskChanged));

        /// <summary>
        /// Character shown for empty required slots while a <see cref="Mask"/> is active (the
        /// placeholder in <c>(___) ___-____</c>). Default <c>'_'</c>. Changing it rebuilds the mask.
        /// </summary>
        public static readonly DependencyProperty PromptCharProperty =
            DependencyProperty.Register(nameof(PromptChar), typeof(char), typeof(WWTextBox),
                new PropertyMetadata('_', OnMaskChanged));

        /// <summary>Horizontal alignment of the text within the editor.</summary>
        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(WWTextBox),
                new PropertyMetadata(TextAlignment.Left));

        /// <summary>
        /// Maximum number of characters the editor accepts. Propagated to the inner TextBox; <c>0</c>
        /// (the default) means unlimited, matching <see cref="TextBox.MaxLength"/>. Intended for
        /// unmasked use — a <see cref="Mask"/> governs its own length.
        /// </summary>
        public static readonly DependencyProperty MaxLengthProperty =
            DependencyProperty.Register(nameof(MaxLength), typeof(int), typeof(WWTextBox),
                new PropertyMetadata(0));

        /// <summary>
        /// Optional monochrome icon shown inside the editor on the <see cref="GlyphPlacement"/> edge.
        /// Feeds <c>sdg:Icon.IconSource</c>, so any <c>IconKeys</c> drawing (or other
        /// <see cref="ImageSource"/>) works; it is tinted to the editor's foreground. Null hides it.
        /// </summary>
        public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register(nameof(Glyph), typeof(ImageSource), typeof(WWTextBox),
                new PropertyMetadata(null));

        /// <summary>Which edge <see cref="Glyph"/> renders on. Default <see cref="GlyphPlacement.Left"/>.</summary>
        public static readonly DependencyProperty GlyphPlacementProperty =
            DependencyProperty.Register(nameof(GlyphPlacement), typeof(GlyphPlacement), typeof(WWTextBox),
                new PropertyMetadata(GlyphPlacement.Left));

        /// <summary>
        /// Debounce, in milliseconds, applied to pushing typed text into <see cref="WWEditorBase.Value"/>
        /// (and thus to any consumer binding on Value). <c>0</c> (the default) pushes on every
        /// keystroke. Implemented via <see cref="Binding.Delay"/> on the inner Value binding, so the
        /// mask keystroke handling is unaffected; leave it at 0 when a <see cref="Mask"/> is set.
        /// </summary>
        public static readonly DependencyProperty UpdateDelayProperty =
            DependencyProperty.Register(nameof(UpdateDelay), typeof(int), typeof(WWTextBox),
                new PropertyMetadata(0, OnUpdateDelayChanged));

        /// <summary>
        /// The mask engine's raw value — the typed content with the mask literals and prompt
        /// characters stripped (e.g. <c>5551234567</c> for a <c>(000) 000-0000</c> phone mask, or the
        /// underlying number for a Numeric mask). Empty when no mask is set carries the plain text.
        /// Read-only; mirrored live from the mask engine.
        /// </summary>
        private static readonly DependencyPropertyKey UnmaskedValuePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(UnmaskedValue), typeof(string), typeof(WWTextBox),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty UnmaskedValueProperty = UnmaskedValuePropertyKey.DependencyProperty;

        /// <summary>
        /// Whether every required slot of the active <see cref="Mask"/> is filled. <c>true</c> when no
        /// mask is set. Read-only; mirrored live from the mask engine — useful as a completion gate.
        /// </summary>
        private static readonly DependencyPropertyKey IsMaskCompletePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsMaskComplete), typeof(bool), typeof(WWTextBox),
                new PropertyMetadata(true));

        public static readonly DependencyProperty IsMaskCompleteProperty = IsMaskCompletePropertyKey.DependencyProperty;

        public string Mask
        {
            get => (string)GetValue(MaskProperty);
            set => SetValue(MaskProperty, value);
        }

        public MaskType MaskType
        {
            get => (MaskType)GetValue(MaskTypeProperty);
            set => SetValue(MaskTypeProperty, value);
        }

        public char PromptChar
        {
            get => (char)GetValue(PromptCharProperty);
            set => SetValue(PromptCharProperty, value);
        }

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public ImageSource Glyph
        {
            get => (ImageSource)GetValue(GlyphProperty);
            set => SetValue(GlyphProperty, value);
        }

        public GlyphPlacement GlyphPlacement
        {
            get => (GlyphPlacement)GetValue(GlyphPlacementProperty);
            set => SetValue(GlyphPlacementProperty, value);
        }

        public int UpdateDelay
        {
            get => (int)GetValue(UpdateDelayProperty);
            set => SetValue(UpdateDelayProperty, value);
        }

        public string UnmaskedValue => (string)GetValue(UnmaskedValueProperty);

        public bool IsMaskComplete => (bool)GetValue(IsMaskCompleteProperty);

        /// <inheritdoc />
        public TextBox EditTextBox => _textBox;

        /// <inheritdoc />
        protected override System.Windows.IInputElement FocusTarget => _textBox;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_clearButton != null)
                _clearButton.Click -= OnClearButtonClick;
            if (_textBox != null)
            {
                UnmaskedValueDescriptor?.RemoveValueChanged(_textBox, OnMaskStateChanged);
                MaskCompleteDescriptor?.RemoveValueChanged(_textBox, OnMaskStateChanged);
            }

            _textBox = GetTemplateChild(PartTextBox) as TextBox;
            _clearButton = GetTemplateChild(PartClearButton) as Button;

            if (_clearButton != null)
                _clearButton.Click += OnClearButtonClick;
            if (_textBox != null)
            {
                UnmaskedValueDescriptor?.AddValueChanged(_textBox, OnMaskStateChanged);
                MaskCompleteDescriptor?.AddValueChanged(_textBox, OnMaskStateChanged);
            }

            // Bind the inner TextBox to Value from code so UpdateDelay can attach Binding.Delay —
            // Delay isn't settable through a TemplateBinding. Must run before ApplyMask so the mask
            // behavior attaches against a TextBox already carrying the current value.
            ApplyValueBinding();
            ApplyMask();
        }

        private static void OnMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((WWTextBox)d).ApplyMask();

        private static void OnUpdateDelayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((WWTextBox)d).ApplyValueBinding();

        private void OnMaskStateChanged(object sender, EventArgs e) => UpdateMaskReadbacks();

        /// <summary>
        /// (Re)binds <c>PART_TextBox.Text</c> to <see cref="WWEditorBase.Value"/> two-way, attaching
        /// <see cref="Binding.Delay"/> when <see cref="UpdateDelay"/> is positive. With a zero delay
        /// this is behaviorally identical to a plain PropertyChanged binding. No-op until the
        /// template's <c>PART_TextBox</c> is realized.
        /// </summary>
        private void ApplyValueBinding()
        {
            if (_textBox == null) return;

            var binding = new Binding
            {
                Source = this,
                Path = new PropertyPath(ValueProperty),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            if (UpdateDelay > 0)
                binding.Delay = UpdateDelay;

            _textBox.SetBinding(TextBox.TextProperty, binding);
        }

        // SetCurrentValue so a consumer's two-way binding still receives the cleared value and
        // isn't clobbered by a hard local set. Value is text here, so empty string (not null) keeps
        // the bound property's type stable.
        private void OnClearButtonClick(object sender, RoutedEventArgs e)
        {
            SetCurrentValue(ValueProperty, string.Empty);
            _textBox?.Focus();
        }

        /// <summary>
        /// Attaches (or detaches) <see cref="MaskInputBehavior"/> on the inner TextBox to match the
        /// current <see cref="Mask"/> / <see cref="MaskType"/> / <see cref="PromptChar"/>. The mask is
        /// cleared first so a MaskType or PromptChar change forces the formatter to rebuild — resetting
        /// the same Mask string wouldn't re-fire the attached-property callback on its own. MaskType and
        /// PromptChar are set before Mask so the behavior builds the formatter against the right engine
        /// and placeholder. No-op until the template's <c>PART_TextBox</c> is realized.
        /// </summary>
        private void ApplyMask()
        {
            if (_textBox == null) return;

            if (!string.IsNullOrEmpty(MaskInputBehavior.GetMask(_textBox)))
                MaskInputBehavior.SetMask(_textBox, null);

            if (!string.IsNullOrEmpty(Mask))
            {
                MaskFormatterFactory.EnsureSupported(MaskType);
                MaskInputBehavior.SetPromptChar(_textBox, PromptChar);
                MaskInputBehavior.SetMaskType(_textBox, MaskType);
                MaskInputBehavior.SetMask(_textBox, Mask);
            }

            UpdateMaskReadbacks();
        }

        // Mirror the mask engine's raw-value / completion state onto the read-only DPs. With no mask
        // the "unmasked" value is just the plain text and the field counts as complete.
        private void UpdateMaskReadbacks()
        {
            if (_textBox == null) return;

            if (string.IsNullOrEmpty(Mask))
            {
                SetValue(UnmaskedValuePropertyKey, _textBox.Text ?? string.Empty);
                SetValue(IsMaskCompletePropertyKey, true);
                return;
            }

            SetValue(UnmaskedValuePropertyKey, MaskInputBehavior.GetUnmaskedValue(_textBox) ?? string.Empty);
            SetValue(IsMaskCompletePropertyKey, MaskInputBehavior.GetIsMaskComplete(_textBox));
        }
    }
}
