using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace WWSearchDataGrid.Modern.SampleApp.Services
{
    /// <summary>
    /// Manages runtime theme switching between Generic and Custom themes
    /// </summary>
    public class ThemeManager
    {
        private static ThemeManager? _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();

        private ThemeType _currentTheme = ThemeType.Custom; // Default based on App.xaml

        /// <summary>
        /// Theme resource URI mappings
        /// </summary>
        private readonly Dictionary<ThemeType, List<Uri>> _themeResourceUris = new()
        {
            {
                ThemeType.Generic,
                new List<Uri>
                {
                    new Uri("/WWSearchDataGrid.Modern.WPF;component/Themes/Generic.xaml", UriKind.Relative)
                }
            },
            {
                ThemeType.Custom,
                new List<Uri>
                {
                    new Uri("/Styles/SearchDataGridControlStyles/ThemeResources.xaml", UriKind.Relative),
                    new Uri("/Styles/SearchDataGridControlStyles/Primitives/NumericUpDownStyles.xaml", UriKind.Relative),
                    new Uri("/Styles/SearchDataGridControlStyles/Primitives/SearchTextBoxStyles.xaml", UriKind.Relative),
                    new Uri("/Styles/DefaultControlStyles.xaml", UriKind.Relative),
                    new Uri("/Styles/SearchDataGridControlStyles/ColumnChooserStyles.xaml", UriKind.Relative),
                    new Uri("/Styles/SearchDataGridControlStyles/ColumnFilterEditorStyles.xaml", UriKind.Relative),
                    new Uri("/Styles/SearchDataGridControlStyles/FilterPanelStyles.xaml", UriKind.Relative),
                    new Uri("/Styles/SearchDataGridControlStyles/ColumnSearchBoxStyles.xaml", UriKind.Relative),
                    new Uri("/Styles/SearchDataGridControlStyles/SearchDataGridStyles.xaml", UriKind.Relative)
                }
            }
        };

        /// <summary>
        /// Gets the current active theme
        /// </summary>
        public ThemeType CurrentTheme => _currentTheme;

        private ThemeManager()
        {
        }

        /// <summary>
        /// Switches to the specified theme
        /// </summary>
        /// <param name="targetTheme">The theme to switch to</param>
        /// <exception cref="InvalidOperationException">Thrown when Application.Current is null</exception>
        public void SwitchTheme(ThemeType targetTheme)
        {
            if (Application.Current == null)
            {
                throw new InvalidOperationException("Cannot switch theme: Application.Current is null");
            }

            if (_currentTheme == targetTheme)
            {
                return; // Already on target theme
            }

            // Remove all theme-related resource dictionaries
            RemoveThemeResources();

            // Add new theme resource dictionaries
            AddThemeResources(targetTheme);

            _currentTheme = targetTheme;

            // Update dynamic theme resources
            UpdateDynamicThemeResources(targetTheme);
        }

        /// <summary>
        /// Removes all theme-related resource dictionaries from the application
        /// </summary>
        private void RemoveThemeResources()
        {
            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
            var allThemeUris = _themeResourceUris.Values.SelectMany(list => list).ToList();

            // Remove in reverse order to prevent dependency issues
            for (int i = mergedDictionaries.Count - 1; i >= 0; i--)
            {
                var dictionary = mergedDictionaries[i];
                if (dictionary.Source != null && IsThemeResource(dictionary.Source, allThemeUris))
                {
                    mergedDictionaries.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Adds resource dictionaries for the specified theme
        /// </summary>
        /// <param name="theme">The theme to add resources for</param>
        private void AddThemeResources(ThemeType theme)
        {
            if (!_themeResourceUris.TryGetValue(theme, out var uris))
            {
                throw new ArgumentException($"Unknown theme: {theme}", nameof(theme));
            }

            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

            // Add resource dictionaries in order (important for dependency resolution)
            foreach (var uri in uris)
            {
                try
                {
                    var resourceDictionary = new ResourceDictionary { Source = uri };
                    mergedDictionaries.Add(resourceDictionary);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to load resource dictionary: {uri}", ex);
                }
            }
        }

        /// <summary>
        /// Determines if a URI is a theme resource
        /// </summary>
        /// <param name="source">The URI to check</param>
        /// <param name="themeUris">List of all theme URIs</param>
        /// <returns>True if the URI is a theme resource</returns>
        private bool IsThemeResource(Uri source, List<Uri> themeUris)
        {
            var sourceString = source.ToString();
            return themeUris.Any(themeUri =>
                string.Equals(sourceString, themeUri.ToString(), StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Updates dynamic theme resources that MainWindow binds to
        /// </summary>
        /// <param name="theme">The active theme</param>
        private void UpdateDynamicThemeResources(ThemeType theme)
        {
            var resources = Application.Current.Resources;

            if (theme == ThemeType.Custom)
            {
                // Dark theme colors for MainWindow
                resources["Theme.ControlPanel.Background"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2D2D30"));
                resources["Theme.ControlPanel.Foreground"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CCCCCC"));
                resources["Theme.Window.Background"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E1E1E"));
            }
            else
            {
                // Light theme colors for MainWindow
                resources["Theme.ControlPanel.Background"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F0F0F0"));
                resources["Theme.ControlPanel.Foreground"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333333"));
                resources["Theme.Window.Background"] = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFFFF"));
            }
        }
    }

    /// <summary>
    /// Available theme types
    /// </summary>
    public enum ThemeType
    {
        /// <summary>
        /// Default library styles from WWSearchDataGrid.Modern.WPF/Themes/Generic.xaml
        /// </summary>
        Generic,

        /// <summary>
        /// Custom overridden styles from SampleApp/Styles/SearchDataGridControlStyles/
        /// </summary>
        Custom
    }
}
