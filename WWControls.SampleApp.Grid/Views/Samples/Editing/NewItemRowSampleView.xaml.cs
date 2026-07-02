using System.Windows.Controls;
using WWControls.SampleApp.Grid.Models;

namespace WWControls.SampleApp.Grid.Views.Samples.Editing
{
    public partial class NewItemRowSampleView : UserControl
    {
        public NewItemRowSampleView()
        {
            InitializeComponent();

            // The DataGrid new-item event isn't a command, so bridge it to the VM here: when the
            // user starts adding a row, hand the freshly-created TaskItem to the VM to seed its Id
            // and due date before the user begins typing.
            Grid.InitializingNewItem += OnInitializingNewItem;
        }

        private void OnInitializingNewItem(object? sender, InitializingNewItemEventArgs e)
        {
            if (DataContext is NewItemRowSampleViewModel vm && e.NewItem is TaskItem task)
                vm.PrepareNewTask(task);
        }
    }
}
