using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace WWControls.Wpf.Primitives
{
    /// <summary>
    /// The library's message box — a modal dialog carrying a message, an optional severity icon,
    /// and a row of buttons. Unlike <see cref="System.Windows.MessageBox"/>, the buttons are not a
    /// fixed Yes / No / OK / Cancel set: pass a list of <see cref="UICommand"/> and the box renders
    /// one <see cref="WWButton"/> per command, returning the one the user picked. Standard
    /// <see cref="System.Windows.MessageBoxButton"/> / <see cref="System.Windows.MessageBoxResult"/>
    /// overloads are provided too, so it drops in for existing <c>MessageBox.Show</c> call sites.
    /// </summary>
    /// <remarks>
    /// Shown by the static <see cref="O:WWControls.Wpf.Primitives.WWMessageBox.Show"/> methods,
    /// which host the control in a modal <see cref="Window"/> wearing the library's shared chrome
    /// (<see cref="PrimitiveThemeKeys.Window"/>, applied through
    /// <see cref="WWControls.Wpf.WindowHostHelper"/>) — the same hosting pattern the Filter Editor
    /// and Column Chooser use. Clicking any command button records it as
    /// <see cref="SelectedCommand"/>, runs that command's <see cref="UICommand.Command"/> if set,
    /// and closes the dialog. A command marked <see cref="UICommand.IsDefault"/> answers to Enter;
    /// one marked <see cref="UICommand.IsCancel"/> answers to Esc and to the window's close button.
    /// </remarks>
    public class WWMessageBox : Control
    {
        static WWMessageBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWMessageBox),
                new FrameworkPropertyMetadata(typeof(WWMessageBox)));
        }

        /// <summary>Creates an empty message box. Prefer the static <c>Show</c> methods.</summary>
        public WWMessageBox()
        {
            Commands = new ObservableCollection<UICommand>();
        }

        // ─── Content ─────────────────────────────────────────────────────────────────────

        /// <summary>Identifies the <see cref="Message"/> dependency property.</summary>
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(nameof(Message), typeof(string), typeof(WWMessageBox),
                new PropertyMetadata(string.Empty));

        /// <summary>The body text of the dialog.</summary>
        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        /// <summary>Identifies the <see cref="Caption"/> dependency property.</summary>
        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register(nameof(Caption), typeof(string), typeof(WWMessageBox),
                new PropertyMetadata(string.Empty));

        /// <summary>The dialog title, shown in the host window's caption bar.</summary>
        public string Caption
        {
            get => (string)GetValue(CaptionProperty);
            set => SetValue(CaptionProperty, value);
        }

        /// <summary>Identifies the <see cref="MessageImage"/> dependency property.</summary>
        public static readonly DependencyProperty MessageImageProperty =
            DependencyProperty.Register(nameof(MessageImage), typeof(MessageBoxImage), typeof(WWMessageBox),
                new PropertyMetadata(MessageBoxImage.None, OnMessageImageChanged));

        /// <summary>
        /// The severity glyph shown beside the message. Mapped onto a <see cref="StatusIcon"/>
        /// badge via <see cref="Status"/> — <see cref="MessageBoxImage.Question"/> has no dedicated
        /// badge and reuses the informational one.
        /// </summary>
        public MessageBoxImage MessageImage
        {
            get => (MessageBoxImage)GetValue(MessageImageProperty);
            set => SetValue(MessageImageProperty, value);
        }

        private static readonly DependencyPropertyKey StatusPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(Status), typeof(StatusKind), typeof(WWMessageBox),
                new PropertyMetadata(StatusKind.None));

        /// <summary>Identifies the read-only <see cref="Status"/> dependency property.</summary>
        public static readonly DependencyProperty StatusProperty = StatusPropertyKey.DependencyProperty;

        /// <summary>
        /// The <see cref="StatusKind"/> the template's badge shows, derived from
        /// <see cref="MessageImage"/>. Read-only — set <see cref="MessageImage"/> to drive it.
        /// </summary>
        public StatusKind Status => (StatusKind)GetValue(StatusProperty);

        private static void OnMessageImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((WWMessageBox)d).SetValue(StatusPropertyKey, ToStatusKind((MessageBoxImage)e.NewValue));
        }

        // ─── Buttons ─────────────────────────────────────────────────────────────────────

        /// <summary>Identifies the <see cref="Commands"/> dependency property.</summary>
        public static readonly DependencyProperty CommandsProperty =
            DependencyProperty.Register(nameof(Commands), typeof(IList<UICommand>), typeof(WWMessageBox),
                new PropertyMetadata(null));

        /// <summary>
        /// The choices the box renders as buttons, left to right. Defaults to an empty
        /// <see cref="ObservableCollection{T}"/>; assign your own list or add to this one.
        /// </summary>
        public IList<UICommand> Commands
        {
            get => (IList<UICommand>)GetValue(CommandsProperty);
            set => SetValue(CommandsProperty, value);
        }

        /// <summary>Identifies the <see cref="ShowCopyButton"/> dependency property.</summary>
        public static readonly DependencyProperty ShowCopyButtonProperty =
            DependencyProperty.Register(nameof(ShowCopyButton), typeof(bool), typeof(WWMessageBox),
                new PropertyMetadata(true));

        /// <summary>
        /// Whether the footer shows a "Copy Message" button that copies <see cref="Message"/> to the
        /// clipboard. Defaults to <see langword="true"/>.
        /// </summary>
        public bool ShowCopyButton
        {
            get => (bool)GetValue(ShowCopyButtonProperty);
            set => SetValue(ShowCopyButtonProperty, value);
        }

        /// <summary>Identifies the <see cref="CopyButtonCaption"/> dependency property.</summary>
        public static readonly DependencyProperty CopyButtonCaptionProperty =
            DependencyProperty.Register(nameof(CopyButtonCaption), typeof(string), typeof(WWMessageBox),
                new PropertyMetadata("Co_py Message"));

        /// <summary>Caption of the copy-to-clipboard button.</summary>
        public string CopyButtonCaption
        {
            get => (string)GetValue(CopyButtonCaptionProperty);
            set => SetValue(CopyButtonCaptionProperty, value);
        }

        private static readonly DependencyPropertyKey SelectedCommandPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(SelectedCommand), typeof(UICommand), typeof(WWMessageBox),
                new PropertyMetadata(null));

        /// <summary>Identifies the read-only <see cref="SelectedCommand"/> dependency property.</summary>
        public static readonly DependencyProperty SelectedCommandProperty = SelectedCommandPropertyKey.DependencyProperty;

        /// <summary>
        /// The command the user picked. Null until a button is clicked; the static <c>Show</c>
        /// methods fall back to the <see cref="UICommand.IsCancel"/> command when the window is
        /// dismissed without a pick.
        /// </summary>
        public UICommand SelectedCommand => (UICommand)GetValue(SelectedCommandProperty);

        // ─── Template wiring ───────────────────────────────────────────────────────────────

        /// <summary>Name of the command-button host (an <see cref="ItemsControl"/>) in the template.</summary>
        public const string PartCommands = "PART_Commands";

        /// <summary>Name of the copy-to-clipboard button in the template.</summary>
        public const string PartCopyButton = "PART_CopyButton";

        private ButtonBase _copyButton;
        private ItemsControl _commandsHost;

        public override void OnApplyTemplate()
        {
            if (_copyButton != null)
                _copyButton.Click -= OnCopyClick;
            if (_commandsHost != null)
                _commandsHost.RemoveHandler(ButtonBase.ClickEvent, (RoutedEventHandler)OnCommandClick);

            base.OnApplyTemplate();

            _copyButton = GetTemplateChild(PartCopyButton) as ButtonBase;
            if (_copyButton != null)
                _copyButton.Click += OnCopyClick;

            _commandsHost = GetTemplateChild(PartCommands) as ItemsControl;
            if (_commandsHost != null)
                _commandsHost.AddHandler(ButtonBase.ClickEvent, (RoutedEventHandler)OnCommandClick);
        }

        private void OnCommandClick(object sender, RoutedEventArgs e)
        {
            var command = FindCommand(e.OriginalSource as DependencyObject);
            if (command != null)
                CommitAndClose(command);
        }

        private void OnCopyClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(Message ?? string.Empty);
            }
            catch
            {
                // The clipboard can be locked by another process; a failed copy is not worth
                // surfacing from inside a message box.
            }
        }

        /// <summary>Walks up from the clicked element to the button carrying a <see cref="UICommand"/>.</summary>
        private static UICommand FindCommand(DependencyObject source)
        {
            while (source is Visual)
            {
                if (source is FrameworkElement fe && fe.DataContext is UICommand command)
                    return command;
                source = VisualTreeHelper.GetParent(source);
            }
            return null;
        }

        private void CommitAndClose(UICommand command)
        {
            SetValue(SelectedCommandPropertyKey, command);

            var host = Window.GetWindow(this);
            if (host == null)
                return;

            try
            {
                host.DialogResult = true;
            }
            catch (InvalidOperationException)
            {
                // Non-modal host — DialogResult can't be set; just close.
            }
            host.Close();
        }

        private static StatusKind ToStatusKind(MessageBoxImage image)
        {
            switch (image)
            {
                case MessageBoxImage.Error: return StatusKind.Error;         // == Hand == Stop
                case MessageBoxImage.Warning: return StatusKind.Warning;     // == Exclamation
                case MessageBoxImage.Information: return StatusKind.Info;     // == Asterisk
                case MessageBoxImage.Question: return StatusKind.Question;
                default: return StatusKind.None;
            }
        }

        // ─── Show ────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Shows the message box with a custom set of choices and returns the one the user picked
        /// (or the <see cref="UICommand.IsCancel"/> command, then <see langword="null"/>, if the
        /// window is dismissed without a pick).
        /// </summary>
        /// <param name="message">The body text.</param>
        /// <param name="caption">The window title.</param>
        /// <param name="commands">The buttons to offer, left to right.</param>
        /// <param name="image">The severity glyph.</param>
        /// <param name="owner">Owner window; when null, the active window is used.</param>
        /// <param name="showCopyButton">Whether to show the "Copy Message" button.</param>
        public static UICommand Show(string message, string caption, IEnumerable<UICommand> commands,
            MessageBoxImage image = MessageBoxImage.None, Window owner = null, bool showCopyButton = true)
        {
            return OnUiThread(() => ShowCore(message, caption, commands, image, owner, showCopyButton));
        }

        /// <summary>Shows an OK message box.</summary>
        public static MessageBoxResult Show(string message)
            => Show(message, string.Empty, MessageBoxButton.OK, MessageBoxImage.None);

        /// <summary>Shows an OK message box with a caption.</summary>
        public static MessageBoxResult Show(string message, string caption)
            => Show(message, caption, MessageBoxButton.OK, MessageBoxImage.None);

        /// <summary>Shows a message box with a standard button set.</summary>
        public static MessageBoxResult Show(string message, string caption, MessageBoxButton buttons)
            => Show(message, caption, buttons, MessageBoxImage.None);

        /// <summary>
        /// Shows a message box with a standard button set and severity icon — a drop-in for
        /// <see cref="System.Windows.MessageBox.Show(string, string, MessageBoxButton, MessageBoxImage)"/>.
        /// </summary>
        public static MessageBoxResult Show(string message, string caption, MessageBoxButton buttons,
            MessageBoxImage image, Window owner = null)
        {
            var chosen = Show(message, caption, BuildStandardCommands(buttons), image, owner);
            return chosen?.Id is MessageBoxResult result ? result : MessageBoxResult.None;
        }

        private static UICommand ShowCore(string message, string caption, IEnumerable<UICommand> commands,
            MessageBoxImage image, Window owner, bool showCopyButton)
        {
            var box = new WWMessageBox
            {
                Message = message,
                Caption = caption,
                MessageImage = image,
                ShowCopyButton = showCopyButton,
            };
            if (commands != null)
            {
                foreach (var command in commands)
                    if (command != null)
                        box.Commands.Add(command);
            }

            var host = new Window
            {
                Title = caption ?? string.Empty,
                Content = box,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };

            var resolvedOwner = ResolveOwner(owner);
            if (resolvedOwner != null && !ReferenceEquals(resolvedOwner, host))
            {
                try
                {
                    host.Owner = resolvedOwner;
                    host.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                catch (InvalidOperationException)
                {
                    // Owner not yet shown — fall back to centering on screen.
                    host.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            }

            WindowHostHelper.ApplyDefaultChrome(host, box);
            host.ShowDialog();

            // A window closed via its X button (or Esc with no cancel button) leaves no selection;
            // treat that as the cancel choice when one exists, mirroring MessageBox semantics.
            return box.SelectedCommand ?? box.Commands.FirstOrDefault(c => c != null && c.IsCancel);
        }

        private static Window ResolveOwner(Window explicitOwner)
        {
            if (explicitOwner != null)
                return explicitOwner;

            var app = Application.Current;
            if (app == null)
                return null;

            Window active = null, focused = null;
            foreach (Window window in app.Windows)
            {
                if (active == null && window.IsActive)
                    active = window;
                if (focused == null && window.IsFocused)
                    focused = window;
            }
            return active ?? focused ?? app.MainWindow;
        }

        private static T OnUiThread<T>(Func<T> func)
        {
            var app = Application.Current;
            if (app != null && !app.Dispatcher.CheckAccess())
                return app.Dispatcher.Invoke(func);
            return func();
        }

        private static IEnumerable<UICommand> BuildStandardCommands(MessageBoxButton buttons)
        {
            switch (buttons)
            {
                case MessageBoxButton.OKCancel:
                    return new[]
                    {
                        new UICommand(MessageBoxResult.OK, "_OK", true, false),
                        new UICommand(MessageBoxResult.Cancel, "_Cancel", false, true),
                    };
                case MessageBoxButton.YesNo:
                    return new[]
                    {
                        new UICommand(MessageBoxResult.Yes, "_Yes", true, false),
                        new UICommand(MessageBoxResult.No, "_No", false, false),
                    };
                case MessageBoxButton.YesNoCancel:
                    return new[]
                    {
                        new UICommand(MessageBoxResult.Yes, "_Yes", true, false),
                        new UICommand(MessageBoxResult.No, "_No", false, false),
                        new UICommand(MessageBoxResult.Cancel, "_Cancel", false, true),
                    };
                case MessageBoxButton.OK:
                default:
                    return new[] { new UICommand(MessageBoxResult.OK, "_OK", true, false) };
            }
        }
    }
}
