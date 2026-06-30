using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WWControls.SampleApp.Controls
{
    /// <summary>
    /// Process-wide settings for the SampleHostControl. Singleton so the user's chosen layout
    /// (tabbed vs. split) persists as they navigate between samples in the LauncherWindow — each
    /// sample swaps in a fresh SampleHostControl instance, but they all bind to this one source.
    /// </summary>
    public sealed class SampleHostSettings : INotifyPropertyChanged
    {
        public static SampleHostSettings Instance { get; } = new();

        private SampleLayoutMode _layoutMode = SampleLayoutMode.Tabbed;
        public SampleLayoutMode LayoutMode
        {
            get => _layoutMode;
            set
            {
                if (_layoutMode == value) return;
                _layoutMode = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
