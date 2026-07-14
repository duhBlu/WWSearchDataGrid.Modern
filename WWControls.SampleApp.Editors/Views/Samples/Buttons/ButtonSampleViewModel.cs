using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WWControls.Wpf.Primitives;
using AsyncCommand = WWControls.Core.AsyncCommand;

namespace WWControls.SampleApp.Editors.Views.Samples.Buttons
{
    /// <summary>
    /// Backs the WWButton playground: one preview button whose every property is driven by the
    /// option controls. The button's command is the click counter in synchronous modes and an
    /// <see cref="AsyncCommand"/> once an <see cref="AsyncDisplayMode"/> is selected, so a single
    /// button demonstrates the simple / repeat / toggle kinds and both async display modes.
    /// </summary>
    public partial class ButtonSampleViewModel : ObservableObject
    {
        // ── Behavior ──────────────────────────────────────────────────────────
        [ObservableProperty]
        private ButtonKind _kind = ButtonKind.Simple;

        [ObservableProperty]
        private string _contentText = "Preview";

        [ObservableProperty]
        private bool _isButtonEnabled = true;

        // ── Glyph ─────────────────────────────────────────────────────────────
        [ObservableProperty]
        private GlyphChoice _glyph = GlyphChoice.Add;

        [ObservableProperty]
        private Dock _glyphAlignment = Dock.Left;

        [ObservableProperty]
        private double _glyphSize = 12;

        [ObservableProperty]
        private double _glyphToContentOffset = 4;

        // ── Chrome ────────────────────────────────────────────────────────────
        [ObservableProperty]
        private double _cornerRadius = 4;

        // ── Toggle ────────────────────────────────────────────────────────────
        [ObservableProperty]
        private bool _isThreeState;

        [ObservableProperty]
        private bool? _toggleState = false;

        // ── Repeat ────────────────────────────────────────────────────────────
        [ObservableProperty]
        private int _delay = 500;

        [ObservableProperty]
        private int _interval = 50;

        // ── Async ─────────────────────────────────────────────────────────────
        [ObservableProperty]
        private AsyncDisplayMode _asyncDisplayMode = AsyncDisplayMode.None;

        [ObservableProperty]
        private WheelKind _asyncWheelKind = WheelKind.Arc;

        // ── Live state readouts ───────────────────────────────────────────────
        [ObservableProperty]
        private int _clickCount;

        [ObservableProperty]
        private string _asyncStatus = "Idle";

        // ── Option sources ────────────────────────────────────────────────────
        public ButtonKind[] Kinds { get; } = { ButtonKind.Simple, ButtonKind.Repeat, ButtonKind.Toggle };
        public GlyphChoice[] Glyphs { get; } =
            { GlyphChoice.None, GlyphChoice.Add, GlyphChoice.Edit, GlyphChoice.Calendar, GlyphChoice.Filter, GlyphChoice.Copy, GlyphChoice.Check };
        public Dock[] GlyphAlignments { get; } = { Dock.Left, Dock.Right, Dock.Top, Dock.Bottom };
        public AsyncDisplayMode[] AsyncDisplayModes { get; } =
            { AsyncDisplayMode.None, AsyncDisplayMode.Wait, AsyncDisplayMode.WaitCancel };
        public WheelKind[] AsyncWheelKinds { get; } = { WheelKind.Arc, WheelKind.Dots };

        // ── Conditional option visibility ─────────────────────────────────────
        public bool ShowRepeatOptions => Kind == ButtonKind.Repeat;
        public bool ShowToggleOptions => Kind == ButtonKind.Toggle;
        public bool ShowWheelOption => AsyncDisplayMode != AsyncDisplayMode.None;

        partial void OnKindChanged(ButtonKind value)
        {
            OnPropertyChanged(nameof(ShowRepeatOptions));
            OnPropertyChanged(nameof(ShowToggleOptions));
        }

        partial void OnAsyncDisplayModeChanged(AsyncDisplayMode value)
        {
            OnPropertyChanged(nameof(ShowWheelOption));
            OnPropertyChanged(nameof(PreviewCommand));
        }

        // ── Commands ──────────────────────────────────────────────────────────
        /// <summary>Runs while an async display mode is selected; cancellable for the WaitCancel demo.</summary>
        public AsyncCommand AsyncPreviewCommand { get; }

        /// <summary>
        /// What the preview button binds its <c>Command</c> to: the click counter in synchronous
        /// modes, the async command once a display mode is chosen so the wheel / cancel visuals engage.
        /// </summary>
        public ICommand PreviewCommand =>
            AsyncDisplayMode == AsyncDisplayMode.None ? IncrementCommand : AsyncPreviewCommand;

        public ButtonSampleViewModel()
        {
            AsyncPreviewCommand = new AsyncCommand(async ct =>
            {
                try
                {
                    AsyncStatus = "Running…";
                    await Task.Delay(TimeSpan.FromSeconds(4), ct);
                    AsyncStatus = "Done";
                }
                catch (OperationCanceledException)
                {
                    AsyncStatus = "Canceled";
                }
            });
        }

        [RelayCommand]
        private void Increment() => ClickCount++;
    }
}
