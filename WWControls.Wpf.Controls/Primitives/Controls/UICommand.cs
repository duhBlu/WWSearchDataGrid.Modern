using System.Windows.Input;
using System.Windows.Media;
using WWControls.Core;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// A declarative description of one button — its caption, glyph, and behavior — that a
    /// <see cref="WWMessageBox"/> (or any command-list consumer) renders as a <see cref="WWButton"/>.
    /// Passing a list of these lets a message box offer arbitrary choices instead of the fixed
    /// Yes / No / OK / Cancel set, then report back the one the user picked.
    /// </summary>
    /// <remarks>
    /// <see cref="Id"/> is what the caller matches on after the dialog closes. <see cref="IsDefault"/>
    /// and <see cref="IsCancel"/> wire up Enter and Esc. When <see cref="Command"/> is an
    /// <see cref="IAsyncCommand"/>, set <see cref="AsyncDisplayMode"/> to show the wait wheel while it runs.
    /// </remarks>
    public class UICommand : ObservableObject
    {
        private object _id;
        private string _caption;
        private string _toolTip;
        private bool _isDefault;
        private bool _isCancel;
        private bool _isEnabled = true;
        private object _tag;
        private ICommand _command;
        private object _commandParameter;
        private ImageSource _glyph;
        private double _glyphSize = 16d;
        private double _glyphStrokeThickness = double.NaN;
        private AsyncDisplayMode _asyncDisplayMode = AsyncDisplayMode.None;

        /// <summary>The value that identifies this choice to the caller once the dialog closes.</summary>
        public object Id
        {
            get => _id;
            set => SetProperty(value, ref _id);
        }

        /// <summary>The button text. Prefix a letter with <c>_</c> to give it an access key (e.g. <c>_Yes</c>).</summary>
        public string Caption
        {
            get => _caption;
            set => SetProperty(value, ref _caption);
        }

        /// <summary>Tooltip shown on the rendered button.</summary>
        public string ToolTip
        {
            get => _toolTip;
            set => SetProperty(value, ref _toolTip);
        }

        /// <summary>Whether this is the default choice — pressing Enter picks it.</summary>
        public bool IsDefault
        {
            get => _isDefault;
            set => SetProperty(value, ref _isDefault);
        }

        /// <summary>Whether this is the cancel choice — pressing Esc (or closing the window) picks it.</summary>
        public bool IsCancel
        {
            get => _isCancel;
            set => SetProperty(value, ref _isCancel);
        }

        /// <summary>Whether the rendered button is enabled. Defaults to <see langword="true"/>.</summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(value, ref _isEnabled);
        }

        /// <summary>Arbitrary data the caller can hang off the command.</summary>
        public object Tag
        {
            get => _tag;
            set => SetProperty(value, ref _tag);
        }

        /// <summary>
        /// Command executed when the button is clicked, in addition to the dialog recording this
        /// choice and closing. Optional — a command with no <see cref="Command"/> is a pure choice.
        /// </summary>
        public ICommand Command
        {
            get => _command;
            set => SetProperty(value, ref _command);
        }

        /// <summary>Parameter passed to <see cref="Command"/> when it executes.</summary>
        public object CommandParameter
        {
            get => _commandParameter;
            set => SetProperty(value, ref _commandParameter);
        }

        /// <summary>
        /// Icon shown on the button, tinted to follow the button's foreground when it is a
        /// monochrome <see cref="DrawingImage"/> (e.g. an <see cref="IconKeys"/> resource).
        /// </summary>
        public ImageSource Glyph
        {
            get => _glyph;
            set => SetProperty(value, ref _glyph);
        }

        /// <summary>
        /// Rendered glyph size (width and height) on the button. Defaults to <c>16</c>; the glyphs
        /// are square, so one value drives both. Forwarded to the rendered <see cref="WWButton"/>'s
        /// <see cref="WWButton.GlyphWidth"/> / <see cref="WWButton.GlyphHeight"/>. Set
        /// <see cref="double.NaN"/> to fall back to the glyph's natural size.
        /// </summary>
        public double GlyphSize
        {
            get => _glyphSize;
            set => SetProperty(value, ref _glyphSize);
        }

        /// <summary>
        /// Stroke thickness override for a <see cref="DrawingImage"/> glyph, in the glyph's own
        /// source-coordinate units — use it to even out glyphs that read too thin or too heavy.
        /// <see cref="double.NaN"/> (default) keeps the glyph's authored thickness. Forwarded to
        /// the rendered <see cref="WWButton"/>'s <see cref="WWButton.GlyphStrokeThickness"/>.
        /// </summary>
        public double GlyphStrokeThickness
        {
            get => _glyphStrokeThickness;
            set => SetProperty(value, ref _glyphStrokeThickness);
        }

        /// <summary>
        /// How the rendered <see cref="WWButton"/> visualizes an asynchronous <see cref="Command"/>
        /// (one implementing <see cref="IAsyncCommand"/>). Defaults to <see cref="AsyncDisplayMode.None"/>.
        /// </summary>
        public AsyncDisplayMode AsyncDisplayMode
        {
            get => _asyncDisplayMode;
            set => SetProperty(value, ref _asyncDisplayMode);
        }

        /// <summary>Creates an empty command — set properties via initializer.</summary>
        public UICommand()
        {
        }

        /// <summary>Creates a command with an id and caption, optionally marked default / cancel.</summary>
        public UICommand(object id, string caption, bool isDefault = false, bool isCancel = false)
        {
            _id = id;
            _caption = caption;
            _isDefault = isDefault;
            _isCancel = isCancel;
        }

        /// <summary>Creates a command that also runs <paramref name="command"/> when clicked.</summary>
        public UICommand(object id, string caption, ICommand command, bool isDefault = false, bool isCancel = false)
            : this(id, caption, isDefault, isCancel)
        {
            _command = command;
        }

        /// <summary>Creates a command with an async display mode for an <see cref="IAsyncCommand"/>.</summary>
        public UICommand(object id, string caption, ICommand command, AsyncDisplayMode asyncDisplayMode, bool isDefault = false, bool isCancel = false)
            : this(id, caption, command, isDefault, isCancel)
        {
            _asyncDisplayMode = asyncDisplayMode;
        }
    }
}
