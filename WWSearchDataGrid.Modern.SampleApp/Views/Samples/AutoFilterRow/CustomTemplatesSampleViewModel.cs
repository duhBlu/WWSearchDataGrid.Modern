using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Generators;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.AutoFilterRow
{
    /// <summary>
    /// Backs the custom-templates sample. Just supplies a row collection — the sample is a
    /// recipe demo for <see cref="WWSearchDataGrid.Modern.WPF.GridColumn.AutoFilterRowEditTemplate"/>,
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
