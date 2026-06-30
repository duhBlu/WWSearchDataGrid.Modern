using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WWControls.SampleApp.Controls;

namespace WWControls.SampleApp
{
    public partial class App : Application
    {
        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Override AvalonEdit's bundled XML / C# highlighting with our tuned-for-#FAFAFA
            // palette before any source viewer is shown.
            CustomSyntaxHighlighting.Register();

            // Register SystemCommands handlers once at the Window class level so the title-bar
            // buttons declared in Styles/WindowStyle.xaml work without per-window code-behind.
            CommandManager.RegisterClassCommandBinding(typeof(Window),
                new CommandBinding(SystemCommands.CloseWindowCommand,
                    (s, _) => SystemCommands.CloseWindow((Window)s!)));
            CommandManager.RegisterClassCommandBinding(typeof(Window),
                new CommandBinding(SystemCommands.MaximizeWindowCommand,
                    (s, _) => SystemCommands.MaximizeWindow((Window)s!)));
            CommandManager.RegisterClassCommandBinding(typeof(Window),
                new CommandBinding(SystemCommands.MinimizeWindowCommand,
                    (s, _) => SystemCommands.MinimizeWindow((Window)s!)));
            CommandManager.RegisterClassCommandBinding(typeof(Window),
                new CommandBinding(SystemCommands.RestoreWindowCommand,
                    (s, _) => SystemCommands.RestoreWindow((Window)s!)));
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Debug.WriteLine($"Unhandled exception: {e.Exception}");
            MessageBox.Show(
                $"An unexpected error occurred: {e.Exception.Message}\n\nPlease contact support if the issue persists.",
                "Application Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
