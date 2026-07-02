using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWControls.SampleApp.Grid.Models;
using WWControls.SampleApp.Grid.SampleData;
using WWControls.SampleApp.Grid.SampleData.Generators;

namespace WWControls.SampleApp.Grid.Views.Samples.Columns
{
    public sealed partial class FixedColumnsSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<OrderItem> _items = new();

        public FixedColumnsSampleViewModel()
        {
            StartLoad(async () =>
            {
                var data = await Task.Run(() =>
                    SampleDataGenerator.Generate(200, OrderGenerator.Create));
                Items = new ObservableCollection<OrderItem>(data);
            });
        }
    }
}
