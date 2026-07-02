using System.Windows.Controls;
using WWControls.SampleApp.Grid.Models;
using WWControls.Wpf;

namespace WWControls.SampleApp.Grid.Views.Samples.Editing
{
    public partial class EditEntireRowSampleView : UserControl
    {
        public EditEntireRowSampleView()
        {
            InitializeComponent();

            // RowEditEnded isn't a command, so bridge it to the VM here for the status panel.
            Grid.RowEditEnded += OnRowEditEnded;
        }

        private void OnRowEditEnded(object? sender, RowEditEventArgs e)
        {
            if (DataContext is not EditEntireRowSampleViewModel vm)
                return;

            var item = e.Item as EditRowItem;
            if (e.Committed)
                vm.NoteCommitted(item);
            else
                vm.NoteCancelled(item);
        }
    }
}
