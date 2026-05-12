using System;
using System.Windows;
using System.Windows.Controls;
using WWSearchDataGrid.Modern.SampleApp.Models;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.Filtering
{
    public partial class CustomPredicateSampleView : UserControl
    {
        private CustomPredicateSampleViewModel? _vm;

        public CustomPredicateSampleView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            DataContextChanged += (_, _) => _vm = DataContext as CustomPredicateSampleViewModel;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _vm = DataContext as CustomPredicateSampleViewModel;
            if (_vm is null) return;

            // Custom SearchFilter — applied alongside any column filters.
            Grid.SearchFilter = item =>
            {
                if (!_vm.CustomPredicateActive) return true;
                if (item is not OrderItem order) return true;
                var needle = _vm.CustomPredicateText ?? string.Empty;
                var notes = order.SpecialInstructionsText ?? string.Empty;
                return string.IsNullOrEmpty(needle)
                    ? !string.IsNullOrEmpty(notes)
                    : notes.Contains(needle, StringComparison.OrdinalIgnoreCase);
            };

            _vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(CustomPredicateSampleViewModel.CustomPredicateText)
                    or nameof(CustomPredicateSampleViewModel.CustomPredicateActive))
                {
                    Grid.FilterItemsSource();
                }
            };

            // Subscribe to FilterPanel events for the live event log.
            var panel = Grid.FilterPanel;
            if (panel is null) return;

            panel.FiltersEnabledChanged += (_, ev) =>
                _vm.LogEvent($"FiltersEnabledChanged: Enabled={ev.Enabled}");
            panel.FilterRemoved += (_, ev) =>
                _vm.LogEvent($"FilterRemoved: {ev.FilterInfo?.ColumnName ?? "(unknown)"}");
            panel.ValueRemovedFromToken += (_, ev) =>
                _vm.LogEvent($"ValueRemovedFromToken: {ev.ValueToken?.DisplayText ?? "(token)"}");
            panel.OperatorToggled += (_, ev) =>
                _vm.LogEvent($"OperatorToggled: Level={ev.Level}, NewOperator={ev.NewOperator}");
            panel.ClearAllFiltersRequested += (_, _) =>
                _vm.LogEvent("ClearAllFiltersRequested");
        }
    }
}
