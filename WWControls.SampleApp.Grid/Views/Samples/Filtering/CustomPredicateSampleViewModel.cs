using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWControls.SampleApp.Grid.Models;
using WWControls.SampleApp.Grid.SampleData;
using WWControls.SampleApp.Grid.SampleData.Generators;

namespace WWControls.SampleApp.Grid.Views.Samples.Filtering
{
    public partial class CustomPredicateSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private string _customPredicateText = string.Empty;

        [ObservableProperty]
        private bool _customPredicateActive;

        [ObservableProperty]
        private ObservableCollection<OrderItem> _items = new();

        public ObservableCollection<string> EventLog { get; } = new();

        public CustomPredicateSampleViewModel()
        {
            StartLoad(async () =>
            {
                var data = await Task.Run(() =>
                    SampleDataGenerator.Generate(500, OrderGenerator.Create));
                Items = new ObservableCollection<OrderItem>(data);
            });
        }

        public void LogEvent(string message)
        {
            var stamped = $"{DateTime.Now:HH:mm:ss} {message}";
            EventLog.Insert(0, stamped);
            while (EventLog.Count > 50) EventLog.RemoveAt(EventLog.Count - 1);
        }
    }
}
