using System.Windows;
using System.Windows.Input;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Launcher
{
    public partial class LauncherWindow : Window
    {
        public LauncherWindow()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (_, _) => SystemCommands.CloseWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, (_, _) => SystemCommands.MaximizeWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, (_, _) => SystemCommands.MinimizeWindow(this)));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, (_, _) => SystemCommands.RestoreWindow(this)));

            SampleList.ItemsSource = SampleCatalog.Samples;
            if (SampleList.Items.Count > 0)
                SampleList.SelectedIndex = 0;
        }

        private void OnSampleDoubleClick(object sender, MouseButtonEventArgs e) => OpenSelected();

        private void OnSampleListKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                OpenSelected();
                e.Handled = true;
            }
        }

        private void OnOpenClick(object sender, RoutedEventArgs e) => OpenSelected();

        private void OpenSelected()
        {
            if (SampleList.SelectedItem is not SampleDefinition def)
                return;

            var window = def.WindowFactory();
            window.Show();
        }
    }
}
