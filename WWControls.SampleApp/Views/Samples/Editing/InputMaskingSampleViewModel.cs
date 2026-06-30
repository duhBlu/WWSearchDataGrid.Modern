using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWControls.SampleApp.Models;
using WWControls.SampleApp.SampleData;
using WWControls.SampleApp.SampleData.Generators;

namespace WWControls.SampleApp.Views.Samples.Editing
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
