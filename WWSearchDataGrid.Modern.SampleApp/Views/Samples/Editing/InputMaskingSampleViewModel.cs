using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Generators;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.Editing
{
    public sealed partial class InputMaskingSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<Contact> _contacts = new();

        public InputMaskingSampleViewModel()
        {
            StartLoad(async () =>
            {
                var data = await Task.Run(() =>
                    SampleDataGenerator.Generate(50, ContactGenerator.Create));
                Contacts = new ObservableCollection<Contact>(data);
            });
        }
    }
}
