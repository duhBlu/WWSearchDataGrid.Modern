using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WWControls.Wpf.Primitives;

namespace WWControls.SampleApp.Editors.Views.Samples.Dialogs
{
    /// <summary>
    /// Backs the WWMessageBox playground. The left panel configures a message; the buttons show it
    /// three ways — the standard <see cref="MessageBoxButton"/> drop-in, a custom
    /// <see cref="UICommand"/> list (Save / Don't Save / Cancel with glyphs), and an arbitrary set
    /// of choices — each writing what the user picked back to <see cref="LastResult"/>.
    /// </summary>
    public partial class MessageBoxSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _message = "Delete the selected cabinet?\n\nThis action can't be undone.";

        [ObservableProperty]
        private string _caption = "Confirm delete";

        [ObservableProperty]
        private MessageBoxImage _icon = MessageBoxImage.Warning;

        [ObservableProperty]
        private MessageBoxButton _buttons = MessageBoxButton.YesNoCancel;

        [ObservableProperty]
        private bool _showCopyButton = true;

        [ObservableProperty]
        private string _lastResult = "—";

        public MessageBoxImage[] Icons { get; } =
        {
            MessageBoxImage.None, MessageBoxImage.Information, MessageBoxImage.Question,
            MessageBoxImage.Warning, MessageBoxImage.Error,
        };

        public MessageBoxButton[] ButtonSets { get; } =
        {
            MessageBoxButton.OK, MessageBoxButton.OKCancel,
            MessageBoxButton.YesNo, MessageBoxButton.YesNoCancel,
        };

        /// <summary>Drop-in path — same signature as <see cref="System.Windows.MessageBox"/>, returns a <see cref="MessageBoxResult"/>.</summary>
        [RelayCommand]
        private void ShowStandard()
        {
            var result = WWMessageBox.Show(Message, Caption, Buttons, Icon);
            LastResult = $"Standard → {result}";
        }

        /// <summary>Custom path — a hand-built <see cref="UICommand"/> list, returns the chosen command.</summary>
        [RelayCommand]
        private void ShowCustom()
        {
            var commands = new[]
            {
                // GlyphSize / GlyphStrokeThickness tune how the glyph renders on the generated button.
                new UICommand("save", "_Save", isDefault: true)
                {
                    Glyph = Glyph(IconKeys.IconCheck),
                    GlyphSize = 15,
                    GlyphStrokeThickness = 2,
                },
                new UICommand("dontsave", "Do_n't Save"),
                new UICommand("cancel", "_Cancel", isCancel: true) 
                { 
                    Glyph = Glyph(IconKeys.IconClear), 
                    GlyphSize = 12,
                    GlyphStrokeThickness = 2,
                },
            };

            var chosen = WWMessageBox.Show(
                "You have unsaved changes to this order.\n\nSave them before closing?",
                "Unsaved changes",
                commands,
                MessageBoxImage.Question,
                showCopyButton: ShowCopyButton);

            LastResult = chosen is null
                ? "Custom → (dismissed)"
                : $"Custom → {chosen.Caption?.Replace("_", string.Empty)} (Id={chosen.Id})";
        }

        /// <summary>Shows that the button count is arbitrary — five choices from one call.</summary>
        [RelayCommand]
        private void ShowManyChoices()
        {
            var commands = new[]
            {
                new UICommand("retry", "_Retry", isDefault: true),
                new UICommand("skip", "_Skip"),
                new UICommand("skipall", "Skip _All"),
                new UICommand("rename", "R_ename"),
                new UICommand("abort", "_Abort", isCancel: true),
            };

            var chosen = WWMessageBox.Show(
                "Couldn't copy 'DoorPanel.stp' — a file with that name already exists.",
                "File copy",
                commands,
                MessageBoxImage.Error);

            LastResult = chosen is null ? "Many → (dismissed)" : $"Many → {chosen.Id}";
        }

        private static ImageSource? Glyph(System.Windows.ComponentResourceKey key)
            => Application.Current?.TryFindResource(key) as ImageSource;
    }
}
