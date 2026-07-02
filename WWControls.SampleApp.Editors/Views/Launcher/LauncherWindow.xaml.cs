using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WWControls.SampleApp.Editors.Views.Launcher
{
    public partial class LauncherWindow : Window
    {
        // Width to restore to when the navigator is re-expanded. Defaults to the initial column
        // width set in XAML; updated each time the user drags the splitter, so toggling collapse
        // then expand returns to whatever width they last chose.
        private double _navigatorExpandedWidth = 240;

        // Pixel width of the collapsed strip — just wide enough to host the expand chevron centered.
        private const double CollapsedNavigatorWidth = 32;

        public LauncherWindow()
        {
            InitializeComponent();
            SampleTree.ItemsSource = SampleCatalog.Categories;

            // Auto-select the first sample in the first category so the right pane isn't empty on launch.
            var firstSample = SampleCatalog.Categories.FirstOrDefault()?.Samples.FirstOrDefault();
            if (firstSample != null)
                LoadSample(firstSample);
        }

        private void OnSampleSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is SampleDefinition def)
                LoadSample(def);
            // If a category was selected (not a leaf), keep the previous sample visible.
        }

        /// <summary>
        /// Clears the host immediately so the previous sample disappears, then constructs the new
        /// sample's view on a Background-priority dispatcher tick. The deferral lets the launcher
        /// repaint the empty host (and any in-flight rendering) before XAML construction blocks the
        /// UI thread, so the swap feels responsive even for heavy samples. Sample VMs handle their
        /// own async data loading via SampleViewModelBase, so the spinner shown by SampleHostControl
        /// keeps animating once the new view is in place.
        /// </summary>
        private void LoadSample(SampleDefinition def)
        {
            SampleHost.Content = null;
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                SampleHost.Content = def.ViewFactory();
            }), DispatcherPriority.Background);
        }

        /// <summary>
        /// Toggles the navigator pane between its stored expanded width and a thin collapsed strip
        /// (just the chevron toggle remains visible). Also hides the GridSplitter when collapsed —
        /// there's nothing useful to resize from a closed pane.
        /// </summary>
        private void OnNavigatorToggleChanged(object sender, RoutedEventArgs e)
        {
            if (NavigatorSplitter is null || NavigatorColumn is null || SplitterColumn is null) return;

            var expanded = NavigatorToggle.IsChecked == true;
            if (expanded)
            {
                NavigatorColumn.MinWidth = 160;
                NavigatorColumn.Width = new GridLength(_navigatorExpandedWidth);
                SplitterColumn.Width = new GridLength(5);
                NavigatorSplitter.Visibility = Visibility.Visible;
                NavigatorHeader.Visibility = Visibility.Visible;
                SampleTree.Visibility = Visibility.Visible;
            }
            else
            {
                // Capture the user's current width before collapsing so re-expand restores it.
                if (NavigatorColumn.ActualWidth > CollapsedNavigatorWidth)
                    _navigatorExpandedWidth = NavigatorColumn.ActualWidth;
                NavigatorColumn.MinWidth = CollapsedNavigatorWidth;
                NavigatorColumn.Width = new GridLength(CollapsedNavigatorWidth);
                SplitterColumn.Width = new GridLength(0);
                NavigatorSplitter.Visibility = Visibility.Collapsed;
                NavigatorHeader.Visibility = Visibility.Collapsed;
                SampleTree.Visibility = Visibility.Collapsed;
            }
        }
    }
}
