using System.Windows.Controls;
using WWControls.Wpf.Controls.Editors;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    public partial class ListBoxSampleView : UserControl
    {
        public ListBoxSampleView() => InitializeComponent();

        private ListBoxSampleViewModel ViewModel => (ListBoxSampleViewModel)DataContext;

        private void OnDemoSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = DemoListBox.SelectedItems.Cast<string>().ToList();
            ViewModel.SelectionSummary = selected.Count == 0
                ? "Selected: (none)"
                : $"Selected ({selected.Count}): {string.Join(", ", selected)}";
        }

        // The engine reports the move; applying it to the data source is the consumer's job.
        private void OnDemoItemReordered(object sender, ItemReorderedEventArgs e)
        {
            ViewModel.Items.Move(e.OldIndex, e.NewIndex);
            ViewModel.LastReorder = $"Last reorder: '{e.Item}' moved {e.OldIndex} → {e.NewIndex}";
        }
    }
}
