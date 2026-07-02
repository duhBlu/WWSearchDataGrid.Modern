using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWControls.SampleApp.Grid.Models;
using WWControls.SampleApp.Grid.SampleData;
using WWControls.SampleApp.Grid.SampleData.Generators;

namespace WWControls.SampleApp.Grid.Views.Samples.FilterRow
{
    /// <summary>
    /// Backs the custom-templates sample. Just supplies a row collection — the sample is a
    /// recipe demo for <see cref="WWControls.Wpf.GridColumn.FilterRowEditTemplate"/>,
    /// not a runtime-tweakable playground. The status name array consumed by the radio-button
    /// template is set via <c>GridColumn.Tag</c> directly in XAML.
    /// </summary>
    public sealed partial class CustomTemplatesSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<OrderItem> _items = new();

        public CustomTemplatesSampleViewModel()
        {
            StartLoad(async () =>
            {
                var data = await Task.Run(() =>
                    SampleDataGenerator.Generate(500, OrderGenerator.Create));
                Items = new ObservableCollection<OrderItem>(data);
            });
        }
    }
}
