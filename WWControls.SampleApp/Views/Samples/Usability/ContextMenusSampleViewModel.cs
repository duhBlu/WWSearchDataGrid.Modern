using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWControls.SampleApp.Models;
using WWControls.SampleApp.SampleData;
using WWControls.SampleApp.SampleData.Generators;

namespace WWControls.SampleApp.Views.Samples.Usability
{
    public sealed partial class ContextMenusSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<OrderItem> _items = new();

        public ContextMenusSampleViewModel()
        {
            StartLoad(async () =>
            {
                var data = await Task.Run(() =>
                    SampleDataGenerator.Generate(100, OrderGenerator.Create));
                Items = new ObservableCollection<OrderItem>(data);
            });
        }
    }
}
