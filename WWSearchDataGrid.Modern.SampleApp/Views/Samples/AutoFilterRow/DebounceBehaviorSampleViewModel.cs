using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Generators;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.AutoFilterRow
{
    /// <summary>
    /// Backs the debounce-and-live-filter sample. Owns the row collection so dataset swaps
    /// happen on a background thread. Live filtering itself is bound directly to the grid's
    /// <c>EnableLiveFiltering</c> DP in XAML — the view model does not gate it.
    /// </summary>
    public sealed partial class DebounceBehaviorSampleViewModel : SampleViewModelBase
    {
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

        private CancellationTokenSource? _cts;

        public DebounceBehaviorSampleViewModel()
        {
            _ = LoadAsync(SmallCount);
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
            }
            catch (System.OperationCanceledException) { }
            finally
            {
                IsBusy = false;
                Status = string.Empty;
            }
        }
    }
}
