using System.Windows;
using System.Windows.Controls;
using WWControls.SampleApp.Models;
using WWControls.Wpf;

namespace WWControls.SampleApp.Views.Samples.Editing
{
    public partial class EditFormSampleView : UserControl
    {
        public EditFormSampleView()
        {
            InitializeComponent();

            // RowEditEnded isn't a command, so bridge it to the VM here for the status panel
            // (the edit form raises the same events as the strip).
            Grid.RowEditEnded += OnRowEditEnded;
        }

        private void OnRowEditEnded(object? sender, RowEditEventArgs e)
        {
            if (DataContext is not EditFormSampleViewModel vm)
                return;

            var item = e.Item as EditRowItem;
            if (e.Committed)
                vm.NoteCommitted(item);
            else
                vm.NoteCancelled(item);
        }

        // Opens the edit form for the selected row programmatically (works even when
        // RowEditTrigger is Never).
        private void OnShowEditFormClick(object sender, RoutedEventArgs e)
        {
            if (Grid.SelectedItem != null)
                Grid.ShowEditForm(Grid.SelectedItem);
        }

        // Swaps the auto-generated layout for the custom EditFormTemplate and back.
        private void OnCustomLayoutToggled(object sender, RoutedEventArgs e)
        {
            Grid.EditFormTemplate = CustomLayoutToggle.IsChecked == true
                ? (DataTemplate)Resources["CustomEditForm"]
                : null;
        }
    }
}
