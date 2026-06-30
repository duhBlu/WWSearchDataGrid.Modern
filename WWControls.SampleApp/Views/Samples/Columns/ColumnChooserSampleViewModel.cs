using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWControls.SampleApp.Models;
using WWControls.SampleApp.SampleData;
using WWControls.SampleApp.SampleData.Generators;

namespace WWControls.SampleApp.Views.Samples.Columns
{
    public sealed partial class ColumnChooserSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<OrderItem> _items = new();

        public ColumnChooserSampleViewModel()
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
