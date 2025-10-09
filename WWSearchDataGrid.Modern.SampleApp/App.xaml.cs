using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace WWSearchDataGrid.Modern.SampleApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes a new instance of the App class
        /// </summary>
        public App()
        {
            // Register any global exception handlers here
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        /// <summary>
        /// Handles unhandled exceptions in the application
        /// </summary>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Log the exception
            Debug.WriteLine($"Unhandled exception: {e.Exception}");

            // Show a user-friendly message
            MessageBox.Show(
                $"An unexpected error occurred: {e.Exception.Message}\n\nPlease contact support if the issue persists.",
                "Application Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Mark as handled to prevent application crash
            e.Handled = true;
        }
    }
}
