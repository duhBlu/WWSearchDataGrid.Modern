using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWControls.SampleApp.Grid.Models;
using WWControls.SampleApp.Grid.SampleData;

namespace WWControls.SampleApp.Grid.Views.Samples.Editing
{
    /// <summary>
    /// Backs the Data Error Indication sample. Seeds <see cref="PersonInfo"/> rows that each trip a
    /// different severity so all three badge tones are visible on load: a missing name (red Error),
    /// a missing address or e-mail (blue Info), and a badly formatted phone or e-mail (amber
    /// Warning). Fix a flagged cell and its badge clears; break a good cell and the matching badge
    /// appears, because the row self-reports through <see cref="System.ComponentModel.INotifyDataErrorInfo"/>.
    /// </summary>
    public sealed partial class DataErrorIndicationSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<PersonInfo> _people = new();

        public DataErrorIndicationSampleViewModel()
        {
            StartLoad(() =>
            {
                People = new ObservableCollection<PersonInfo>
                {
                    // Valid row — no badges.
                    new("Nancy", "Davolio", "507 - 20th Ave. E.", "(206) 555-9857", "nancy@example.com"),

                    // Missing first name → blocking Error (red).
                    new("", "Fuller", "908 W. Capital Way", "(206) 555-9482", "andrew@example.com"),

                    // Missing address → advisory Info (blue); unformatted phone → Warning (amber).
                    new("Janet", "Leverling", "", "555-3412", "janet@example.com"),

                    // Malformed e-mail → Warning (amber).
                    new("Margaret", "Peacock", "4110 Old Redmond Rd.", "(206) 555-8122", "margaret(at)example.com"),

                    // Missing e-mail → advisory Info (blue).
                    new("Steven", "Buchanan", "14 Garrett Hill", "(206) 555-4848", ""),
                };
                return Task.CompletedTask;
            });
        }
    }
}
