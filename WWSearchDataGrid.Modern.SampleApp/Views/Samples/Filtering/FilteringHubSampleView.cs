using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.SampleApp.Controls;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.Filtering
{
    /// <summary>
    /// The Data Shaping → Filtering hub. An inner tree on the left lists the filtering mini-samples;
    /// selecting a leaf swaps the matching sample view into the right panel. Existing filtering views
    /// are reused verbatim (each keeps its own source-tab host); unbuilt nodes resolve to a
    /// <see cref="PlannedSampleView"/>. Views are created lazily on first selection and cached, so
    /// heavy sample view models only spin up when the user navigates to them.
    /// </summary>
    public sealed class FilteringHubSampleView : UserControl
    {
        private readonly ContentControl _host = new() { Margin = new Thickness(0) };
        private readonly SampleLoadingOverlay _overlay = new() { Status = "Loading sample…" };
        private readonly Dictionary<TreeViewItem, UserControl> _cache = new();
        private int _selectionGeneration;

        public FilteringHubSampleView()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(240) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var tree = BuildTree();
            tree.SelectedItemChanged += OnNodeSelected;
            Grid.SetColumn(tree, 0);

            var splitter = new GridSplitter
            {
                Width = 5,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = new SolidColorBrush(Color.FromRgb(0xE0, 0xE0, 0xE0)),
            };
            Grid.SetColumn(splitter, 1);

            var rightPanel = new Grid();
            rightPanel.Children.Add(_host);
            rightPanel.Children.Add(_overlay);
            Grid.SetColumn(rightPanel, 2);

            grid.Children.Add(tree);
            grid.Children.Add(splitter);
            grid.Children.Add(rightPanel);
            Content = grid;
        }

        private TreeView BuildTree()
        {
            var tree = new TreeView { BorderThickness = new Thickness(0), Padding = new Thickness(4) };

            var excel = Branch("Excel-Style Drop-Down Filter");
            excel.Items.Add(Leaf("Custom Popup Content", () => new CustomFilterElementsSampleView()));
            excel.Items.Add(Leaf("Multi-Tab Popup", () => new MultiTabFilterPopupSampleView()));
            excel.Items.Add(Leaf("Group Filters", () => PlannedSampleView.Planned(
                "Group Filters",
                "Filtering against a grouped Excel-style popup — values rolled up by a grouping key inside the dropdown.",
                "Implement grouped value listing in the filter popup",
                "Prereq: column grouping support in the library")));
            excel.IsExpanded = true;
            tree.Items.Add(excel);

            tree.Items.Add(Leaf("Filter Editor", () => new FilterStringSampleView()));

            var filterRow = Branch("Filter Row");
            filterRow.Items.Add(Leaf("Options Playground", () => new FilterRow.OptionsPlaygroundSampleView()));
            filterRow.Items.Add(Leaf("Custom Templates", () => new FilterRow.CustomTemplatesSampleView()));
            filterRow.IsExpanded = true;
            tree.Items.Add(filterRow);

            tree.Items.Add(Leaf("Filter Panel", () => new CustomPredicateSampleView()));
            tree.Items.Add(Leaf("Search Modes", () => new SearchModesSampleView()));

            return tree;
        }

        private static TreeViewItem Branch(string header) =>
            new() { Header = header, FontWeight = FontWeights.SemiBold };

        private static TreeViewItem Leaf(string header, Func<UserControl> factory) =>
            new() { Header = header, Tag = factory };

        private void OnNodeSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is not TreeViewItem node || node.Tag is not Func<UserControl> factory)
                return; // branch node — keep the current right-panel view

            var generation = ++_selectionGeneration;

            if (_cache.TryGetValue(node, out var cached))
            {
                _host.Content = cached;
                _overlay.IsBusy = false;
                return;
            }

            // Heavy sample views (data generation + VM spin-up) construct on the UI thread.
            // Show the overlay synchronously, then defer factory() so the spinner gets a paint
            // pass before the UI thread is consumed. Background priority lets Render run first.
            _overlay.IsBusy = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var view = factory();
                _cache[node] = view;

                if (generation != _selectionGeneration)
                    return; // a newer selection superseded us — let that path own the swap

                _host.Content = view;
                _overlay.IsBusy = false;
            }), DispatcherPriority.Background);
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            // Select the first leaf so the right panel isn't empty on first paint.
            if (Content is Grid grid)
                foreach (var child in grid.Children)
                    if (child is TreeView tree && tree.Items.Count > 0 && tree.Items[0] is TreeViewItem first
                        && first.Items.Count > 0 && first.Items[0] is TreeViewItem firstLeaf)
                    {
                        firstLeaf.IsSelected = true;
                        break;
                    }
        }
    }
}
