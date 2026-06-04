using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Generators;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.Grouping
{
    public sealed partial class BasicGroupingSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<OrderItem> _items = new();

        public BasicGroupingSampleViewModel()
        {
            StartLoad(async () =>
            {
                var data = await Task.Run(() =>
                    SampleDataGenerator.Generate(300, OrderGenerator.Create));
                Items = new ObservableCollection<OrderItem>(data);
            });
        }
    }
}
