using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
    /// Plain text editor built over <see cref="WWBaseEdit"/>. The base owns the chrome; this type
    /// owns the <see cref="TextBox"/> that lives inside it. <see cref="WWBaseEdit.Value"/> carries
    /// the text. An optional <see cref="Mask"/> / <see cref="MaskType"/> routes keystrokes through
    /// <see cref="MaskInputBehavior"/> for keystroke validation, region-aware Tab navigation,
    /// mask-aware paste, and finalize-on-blur.
    /// </summary>
    /// <remarks>
    /// The control has no knowledge of any grid: it raises normal input events and surfaces its
    /// text element via <see cref="IEditTextBoxProvider"/>. Cell-interaction behavior (arrow-key
    /// cell exit, mouse-click caret, decoration-button visibility) is layered on by the grid-side
    /// editor host, not by this control.
    /// </remarks>
    public class WWTextEdit : WWBaseEdit, IEditTextBoxProvider
    {
        private readonly TextBox _textBox;

        static WWTextEdit()
        {
            // Reuse WWBaseEdit's one chrome template rather than carrying a second one.
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWTextEdit),
                new FrameworkPropertyMetadata(typeof(WWBaseEdit)));
        }

        public WWTextEdit()
        {
            _textBox = new TextBox
            {
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent,
                Padding = new Thickness(0),
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            // Text is the editor's Value. The host that owns this editor binds Value to its data
            // source; this inner two-way binding keeps the TextBox and Value in lockstep, with
            // Value updating on each keystroke so a mask / outer commit sees the live text.
            BindingOperations.SetBinding(_textBox, TextBox.TextProperty, new Binding(nameof(Value))
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            });
            BindingOperations.SetBinding(_textBox, TextBox.IsReadOnlyProperty, new Binding(nameof(IsReadOnly))
            {
                Source = this,
                Mode = BindingMode.OneWay,
            });
            BindingOperations.SetBinding(_textBox, TextBox.TextAlignmentProperty, new Binding(nameof(TextAlignment))
            {
                Source = this,
                Mode = BindingMode.OneWay,
            });
            BindingOperations.SetBinding(_textBox, Control.ForegroundProperty, new Binding(nameof(Foreground))
            {
                Source = this,
                Mode = BindingMode.OneWay,
            });

            EditContent = _textBox;
            Loaded += OnLoaded;
        }

        /// <summary>
        /// Optional mask pattern. When set, the inner TextBox is wired through
        /// <see cref="MaskInputBehavior"/>; the pattern grammar is determined by <see cref="MaskType"/>.
        /// </summary>
        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register(nameof(Mask), typeof(string), typeof(WWTextEdit),
                new PropertyMetadata(null, OnMaskChanged));

        /// <summary>How the <see cref="Mask"/> pattern is interpreted. Default <see cref="MaskType.Simple"/>.</summary>
        public static readonly DependencyProperty MaskTypeProperty =
            DependencyProperty.Register(nameof(MaskType), typeof(MaskType), typeof(WWTextEdit),
                new PropertyMetadata(MaskType.Simple, OnMaskChanged));

        /// <summary>Horizontal alignment of the text within the editor.</summary>
        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(WWTextEdit),
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

        private static void OnMaskChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((WWTextEdit)d).ApplyMask();

        private void OnLoaded(object sender, RoutedEventArgs e) => ApplyMask();

        /// <summary>
        /// Attaches (or detaches) <see cref="MaskInputBehavior"/> on the inner TextBox to match the
        /// current <see cref="Mask"/> / <see cref="MaskType"/>. MaskType is set before Mask so the
        /// behavior's mask-changed callback builds the formatter against the right engine — setting
        /// Mask first would let it construct a Simple formatter against (e.g.) a "C2" numeric pattern.
        /// </summary>
        private void ApplyMask()
        {
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
