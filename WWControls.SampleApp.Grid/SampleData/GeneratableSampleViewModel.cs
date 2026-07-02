using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WWControls.SampleApp.Grid.SampleData
{
    /// <summary>
    /// Base ViewModel for samples that bind to a randomized collection. Subclasses provide
    /// a per-row producer; this class wires up Generate/Clear commands, busy state, status text,
    /// row count, and cancellation. The bound collection is replaced wholesale on each Generate
    /// so the grid's ItemsSource sees a single change.
    /// </summary>
    public abstract partial class GeneratableSampleViewModel<T> : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<T> _items = new();

        [ObservableProperty]
        private int _rowCount = 1000;

        private CancellationTokenSource? _cts;

        protected abstract T CreateItem(Random rnd, int index);

        protected GeneratableSampleViewModel()
        {
            _ = LoadInitialAsync();
        }

        private async Task LoadInitialAsync() => await GenerateAsync(RowCount);

        [RelayCommand]
        private Task Generate(object? countParam)
        {
            int count = countParam switch
            {
                int i => i,
                string s when int.TryParse(s, out var parsed) => parsed,
                _ => RowCount
            };
            return GenerateAsync(count);
        }

        private async Task GenerateAsync(int count)
        {
            if (count <= 0) return;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            IsBusy = true;
            try
            {
                var progress = new Progress<string>(s =>
                {
                    Application.Current.Dispatcher.Invoke(() => Status = s);
                });

                List<T> generated = await SampleDataGenerator.GenerateAsync(
                    count, CreateItem, seed: null, progress, token);

                if (token.IsCancellationRequested) return;

                Items = new ObservableCollection<T>(generated);
            }
            catch (OperationCanceledException) { }
            finally
            {
                IsBusy = false;
                Status = string.Empty;
            }
        }

        [RelayCommand]
        private void Clear()
        {
            _cts?.Cancel();
            Items = new ObservableCollection<T>();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
        }
    }
}
