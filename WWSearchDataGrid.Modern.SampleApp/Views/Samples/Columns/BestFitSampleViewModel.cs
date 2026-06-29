using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Generators;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.Columns
{
    public sealed partial class BestFitSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<OrderItem> _items = new();

        // Alternates each reload so BestFitModeOnSourceChange visibly re-fits to different
        // content widths.
        private bool _longOutliers = true;

        public BestFitSampleViewModel()
        {
            StartLoad(async () => Items = await GenerateAsync());
        }

        [RelayCommand]
        private async Task ReloadDataAsync()
        {
            _longOutliers = !_longOutliers;
            Items = await GenerateAsync();
        }

        private async Task<ObservableCollection<OrderItem>> GenerateAsync()
        {
            bool longOutliers = _longOutliers;
            List<OrderItem> rows = await Task.Run(() =>
            {
                var data = SampleDataGenerator.Generate(2000, OrderGenerator.Create);

                // Plant wide outliers deep past the first viewport so VisibleRows best-fit
                // misses them and AllRows catches them.
                string suffix = longOutliers
                    ? " — Preferred National Accounts Division (West Region)"
                    : " — Natl. Accts";
                for (int i = 150; i < data.Count; i += 400)
                {
                    data[i].CustomerName += suffix;
                    data[i].JobName += suffix;
                }

                return data;
            });

            return new ObservableCollection<OrderItem>(rows);
        }
    }
}
