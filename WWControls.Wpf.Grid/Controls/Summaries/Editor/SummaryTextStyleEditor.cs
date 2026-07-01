using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// The Prefix / Value / Suffix text-styling sub-editor opened from the summary editor's Order
    /// tab. Each tab carries Bold / Italic / Underline toggles plus a <see cref="WWColorPicker"/>
    /// and a live preview. Edits the selected <see cref="SummaryEditorEntry"/>'s working-copy
    /// segment styles in place; closing returns to the parent editor, whose OK / Cancel decides
    /// persistence. Templated in the theme (<see cref="ThemeKeys.SummaryTextStyleEditor"/>); hosted
    /// in a window styled by the shared <see cref="ThemeKeys.PrimitivesWindow"/> chrome, same as the
    /// group summary editor.
    /// </summary>
    public class SummaryTextStyleEditor : Control
    {
        public SummaryTextStyleEditor()
        {
            DefaultStyleKey = typeof(SummaryTextStyleEditor);
        }

        internal SummaryTextStyleEditorViewModel ViewModel { get; private set; }

        private ICommand _closeCommand;

        /// <summary>Closes the styling dialog. Edits are already live on the entry, so there's nothing to apply.</summary>
        public ICommand CloseCommand => _closeCommand ??= new RelayCommand(_ => Window.GetWindow(this)?.Close());

        /// <summary>
        /// Opens the styling editor for <paramref name="entry"/> modally, owned by the window
        /// hosting <paramref name="ownerElement"/> (typically the group summary editor).
        /// </summary>
        public static void Show(SummaryEditorEntry entry, FrameworkElement ownerElement)
        {
            if (entry == null) return;

            var editor = new SummaryTextStyleEditor();
            editor.ViewModel = new SummaryTextStyleEditorViewModel(entry);
            editor.DataContext = editor.ViewModel;

            var host = new Window
            {
                Title = "Text styling",
                Content = editor,
                Owner = ownerElement == null ? null : Window.GetWindow(ownerElement),
                Width = 340,
                Height = 340,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
            };

            // Same chrome as the group summary editor's host — resolved by ComponentResourceKey
            // through the assembly's Themes/Generic.xaml.
            WindowHostHelper.ApplyDefaultChrome(host, editor);

            host.ShowDialog();
        }
    }
}
