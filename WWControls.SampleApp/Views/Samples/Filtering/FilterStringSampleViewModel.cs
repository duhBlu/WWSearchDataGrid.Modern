using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWControls.SampleApp.Models;
using WWControls.SampleApp.SampleData;
using WWControls.SampleApp.SampleData.Generators;

namespace WWControls.SampleApp.Views.Samples.Filtering
{
    public partial class FilterStringSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<OrderItem> _items = new();

        [ObservableProperty]
        private string _filterString = "[OrderStatusName] = 'Submitted'";

        public FilterStringSampleViewModel()
        {
            StartLoad(async () =>
            {
                var data = await Task.Run(() =>
                    SampleDataGenerator.Generate(500, OrderGenerator.Create));
                Items = new ObservableCollection<OrderItem>(data);
            });
        }

        [RelayCommand]
        private void Clear() => FilterString = string.Empty;

        [RelayCommand]
        private void ApplyPreset(string preset) => FilterString = preset ?? string.Empty;
    }
}
