using System.Windows;
using System.Windows.Controls;
using WWControls.Core.Display;
using WWControls.Wpf.Behaviors;

namespace WWControls.Wpf.Editors
{
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
    /// text — the inner TextBox two-way-binds to it in the template. An optional <see cref="Mask"/> /
    /// <see cref="MaskType"/> routes keystrokes through <see cref="MaskInputBehavior"/> for keystroke
    /// validation, region-aware Tab navigation, mask-aware paste, and finalize-on-blur.
    /// </summary>
    /// <remarks>
    /// The control has no knowledge of any grid: it raises normal input events and surfaces its
    /// text element via <see cref="IEditTextBoxProvider"/>. Cell-interaction behavior (arrow-key
    /// cell exit, mouse-click caret, decoration-button visibility) is layered on by the grid-side
    /// editor host, not by this control.
    /// </remarks>
    [TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
    public class WWTextBox : WWEditorBase, IEditTextBoxProvider
    {
        private const string PartTextBox = "PART_TextBox";

        private TextBox _textBox;

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

        /// <summary>Horizontal alignment of the text within the editor.</summary>
        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(WWTextBox),
                new PropertyMetadata(TextAlignment.Left));

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

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        /// <inheritdoc />
        public TextBox EditTextBox => _textBox;

        /// <inheritdoc />
        protected override System.Windows.IInputElement FocusTarget => _textBox;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _textBox = GetTemplateChild(PartTextBox) as TextBox;

            // Give the inner text box the library's flat editor style, set explicitly so it can't
            // pick up an ambient implicit TextBox style from the host app (which would draw a second
            // border inside this editor's chrome). It must be unconditional: an applied implicit
            // style leaves Style non-null, so a "Style == null" guard would skip ours and let the
            // host's win. The editor's chrome owns the border; the inner box stays flat.
            if (_textBox != null && TryFindResource(EditorThemeKeys.EditTextBox) is Style flatStyle)
                _textBox.Style = flatStyle;

            ApplyMask();
        }

        private static void OnMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((WWTextBox)d).ApplyMask();

        /// <summary>
        /// Attaches (or detaches) <see cref="MaskInputBehavior"/> on the inner TextBox to match the
        /// current <see cref="Mask"/> / <see cref="MaskType"/>. MaskType is set before Mask so the
        /// behavior's mask-changed callback builds the formatter against the right engine — setting
        /// Mask first would let it construct a Simple formatter against (e.g.) a "C2" numeric pattern.
        /// No-op until the template's <c>PART_TextBox</c> is realized.
        /// </summary>
        private void ApplyMask()
        {
            if (_textBox == null) return;

            if (string.IsNullOrEmpty(Mask))
            {
                if (!string.IsNullOrEmpty(MaskInputBehavior.GetMask(_textBox)))
                    MaskInputBehavior.SetMask(_textBox, null);
                return;
            }

            MaskFormatterFactory.EnsureSupported(MaskType);
            MaskInputBehavior.SetMaskType(_textBox, MaskType);
            MaskInputBehavior.SetMask(_textBox, Mask);
        }
    }
}
