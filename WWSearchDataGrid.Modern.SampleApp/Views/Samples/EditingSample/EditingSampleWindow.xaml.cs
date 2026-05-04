using System.Windows;
using System.Windows.Input;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.EditingSample
{
    public partial class EditingSampleWindow : Window
    {
        public EditingSampleWindow()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (_, _) => SystemCommands.CloseWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, (_, _) => SystemCommands.MaximizeWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, (_, _) => SystemCommands.MinimizeWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, (_, _) => SystemCommands.RestoreWindow(this)));
        }
    }
}
