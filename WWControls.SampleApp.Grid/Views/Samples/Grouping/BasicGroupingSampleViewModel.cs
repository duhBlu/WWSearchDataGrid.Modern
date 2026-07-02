using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WWControls.SampleApp.Grid.Models;
using WWControls.SampleApp.Grid.SampleData;
using WWControls.SampleApp.Grid.SampleData.Generators;

namespace WWControls.SampleApp.Grid.Views.Samples.Grouping
{
    public sealed partial class BasicGroupingSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<OrderItem> _items = new();

        private CancellationTokenSource _addCts;

        public BasicGroupingSampleViewModel()
        {
            StartLoad(async () =>
            {
                var data = await Task.Run(() =>
                    SampleDataGenerator.Generate(300, OrderGenerator.Create));
                Items = new ObservableCollection<OrderItem>(data);
            });
        }

        /// <summary>
        /// Appends <paramref name="count"/> freshly generated orders to the live collection so the
        /// grouped grid grows on screen and the row-count status bar ticks up. New rows continue the
        /// index sequence so their generated values (OrderNumber, dates, …) stay distinct.
        /// </summary>
        /// <remarks>
        /// The collection is replaced wholesale (a single reset) rather than raising one
        /// CollectionChanged per row — at +1,000,000 the per-item path would refilter/re-project the
        /// grid a million times. Generation runs on a worker thread behind the busy overlay; group
        /// expansion state is path-keyed and survives the reset.
        /// </remarks>
        public async void AddRows(int count)
        {
            if (count <= 0) return;

            _addCts?.Cancel();
            _addCts = new CancellationTokenSource();
            var token = _addCts.Token;

            IsBusy = true;
            Status = $"Adding {count:N0} row{(count == 1 ? string.Empty : "s")}…";
            try
            {
                int baseIndex = Items.Count;
                var progress = new Progress<string>(s => Status = s);

                var generated = await SampleDataGenerator.GenerateAsync(
                    count,
                    (rnd, i) => OrderGenerator.Create(rnd, baseIndex + i),
                    seed: null,
                    progress,
                    token);

                if (token.IsCancellationRequested) return;

                Items = new ObservableCollection<OrderItem>(Items.Concat(generated));
            }
            catch (OperationCanceledException) { }
            finally
            {
                IsBusy = false;
                Status = string.Empty;
            }
        }
    }
}
