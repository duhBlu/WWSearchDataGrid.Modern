using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Generators;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.DataBinding
{
    public sealed partial class PocoAttributesSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<Customer> _customers = new();

        public PocoAttributesSampleViewModel()
        {
            StartLoad(async () =>
            {
                var data = await Task.Run(() =>
                    SampleDataGenerator.Generate(25, CustomerGenerator.Create));
                Customers = new ObservableCollection<Customer>(data);
            });
        }
    }
}
