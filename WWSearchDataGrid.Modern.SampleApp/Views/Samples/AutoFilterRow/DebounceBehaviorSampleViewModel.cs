using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Generators;
using WWSearchDataGrid.Modern.WPF;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.AutoFilterRow
{
    /// <summary>
    /// Backs the debounce-and-live-filter sample. Owns the row collection so dataset swaps
    /// happen on a background thread; tracks the registered grid columns so the global
    /// "Immediate update" checkbox can write through to every <see cref="GridColumn.ImmediateUpdateAutoFilter"/>
    /// at once. Re-computes <see cref="EffectiveLiveFilteringLabel"/> whenever the inputs that
    /// drive it change (dataset size + the global override).
    /// </summary>
    public sealed partial class DebounceBehaviorSampleViewModel : SampleViewModelBase
    {
        // Dataset sizes — Small stays well below LiveFilteringRowCountThreshold (100k) so
        // immediate-update genuinely fires per keystroke; Medium sits at the threshold so the
        // auto-off behavior is visible; Large is heavy enough to feel the cost when overridden.
        private const int SmallCount = 1_000;
        private const int MediumCount = 100_000;
        private const int LargeCount = 1_000_000;

        [ObservableProperty]
        private ObservableCollection<OrderItem> _items = new();

        [ObservableProperty]
        private bool _isSmallSelected = true;

        [ObservableProperty]
        private bool _isMediumSelected;

        [ObservableProperty]
        private bool _isLargeSelected;

        [ObservableProperty]
        private bool _immediateUpdateAutoFilter = true;

        [ObservableProperty]
        private string _effectiveLiveFilteringLabel = "Live filtering: ACTIVE";

        private readonly List<GridColumn> _columns = new();
        private SearchDataGrid? _grid;
        private CancellationTokenSource? _cts;

        public DebounceBehaviorSampleViewModel()
        {
            _ = LoadAsync(SmallCount);
        }

        public void AttachGrid(SearchDataGrid grid) => _grid = grid;

        public void RegisterColumn(GridColumn column)
        {
            if (!_columns.Contains(column))
                _columns.Add(column);

            // Honor the current toggle state for newly registered columns so the sample starts
            // coherent regardless of XAML/grid load ordering.
            column.ImmediateUpdateAutoFilter = ImmediateUpdateAutoFilter;
        }

        partial void OnIsSmallSelectedChanged(bool value)
        {
            if (value) SwitchDataset(SmallCount);
        }

        partial void OnIsMediumSelectedChanged(bool value)
        {
            if (value) SwitchDataset(MediumCount);
        }

        partial void OnIsLargeSelectedChanged(bool value)
        {
            if (value) SwitchDataset(LargeCount);
        }

        partial void OnImmediateUpdateAutoFilterChanged(bool value)
        {
            foreach (var column in _columns)
                column.ImmediateUpdateAutoFilter = value;
            RefreshEffectiveLabel();
        }

        private void SwitchDataset(int count)
        {
            _ = LoadAsync(count);
        }

        private async Task LoadAsync(int count)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            IsBusy = true;
            Status = $"Generating {count:N0} rows…";

            try
            {
                var progress = new System.Progress<string>(s =>
                    Application.Current.Dispatcher.Invoke(() => Status = s));

                var generated = await SampleDataGenerator.GenerateAsync(
                    count, OrderGenerator.Create, seed: null, progress, token);

                if (token.IsCancellationRequested) return;

                Items = new ObservableCollection<OrderItem>(generated);
                RefreshEffectiveLabel();
            }
            catch (System.OperationCanceledException) { }
            finally
            {
                IsBusy = false;
                Status = string.Empty;
            }
        }

        /// <summary>
        /// Mirrors the runtime decision made deep inside the grid: above
        /// <see cref="SearchDataGrid.LiveFilteringRowCountThreshold"/>, live filtering is
        /// auto-disabled unless every column has it explicitly enabled. The label is purely
        /// informational — the grid itself authoritatively gates its own behavior.
        /// </summary>
        public void RefreshEffectiveLabel()
        {
            int rowCount = Items?.Count ?? 0;
            bool aboveThreshold = rowCount > SearchDataGrid.LiveFilteringRowCountThreshold;

            string state;
            if (!ImmediateUpdateAutoFilter)
                state = "INACTIVE (Immediate update is off — press Enter to commit)";
            else if (aboveThreshold)
                state = $"ACTIVE (overridden — costs visible above {SearchDataGrid.LiveFilteringRowCountThreshold:N0} rows)";
            else
                state = "ACTIVE";

            EffectiveLiveFilteringLabel = $"Live filtering: {state}";
        }
    }
}
