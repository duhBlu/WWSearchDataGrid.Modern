using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWControls.SampleApp.Models;
using WWControls.SampleApp.SampleData;
using WWControls.SampleApp.SampleData.Generators;

namespace WWControls.SampleApp.Views.Samples.AnimationPerformance
{
    public sealed partial class ScrollingAnimationSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<OrderItem> _items = new();

        public ScrollingAnimationSampleViewModel()
        {
            StartLoad(async () =>
            {
                var data = await Task.Run(() =>
                    SampleDataGenerator.Generate(5000, OrderGenerator.Create));
                Items = new ObservableCollection<OrderItem>(data);
            });
        }
    }
}
