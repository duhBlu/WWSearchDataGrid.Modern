using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using WWControls.SampleApp.Grid.Models;
using WWControls.SampleApp.Grid.SampleData;
using WWControls.SampleApp.Grid.SampleData.Generators;

namespace WWControls.SampleApp.Grid.Views.Samples.DataBinding
{
    /// <summary>Which item-source shape the auto-columns grid is currently showing.</summary>
    public enum BindingSourceKind
    {
        Poco,
        DataTable,
    }

    /// <summary>
    /// Backs the merged Auto Columns Generation sample. Both an attributed POCO collection and a
    /// DataTable are loaded up front and bound to their own AutoGenerateColumns grid; the view shows
    /// whichever the <see cref="SourceKind"/> toggle selects. Two grids (rather than one with a
    /// swapped ItemsSource) because the grid keeps its first-generated auto-columns when the source
    /// schema changes wholesale — see SAMPLE_BACKLOG.md.
    /// </summary>
    public sealed partial class AutoColumnsSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<Customer> _customers = new();

        [ObservableProperty]
        private ICollectionView? _vendorTableView;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ActiveDescription))]
        [NotifyPropertyChangedFor(nameof(IsPoco))]
        [NotifyPropertyChangedFor(nameof(IsDataTable))]
        private BindingSourceKind _sourceKind = BindingSourceKind.Poco;

        public bool IsPoco => SourceKind == BindingSourceKind.Poco;

        public bool IsDataTable => SourceKind == BindingSourceKind.DataTable;

        public string ActiveDescription => SourceKind == BindingSourceKind.Poco
            ? "POCO collection. Headers come from [Display(Name)], order from [Display(Order)]. InternalId is hidden by [Browsable(false)]; InternalNotes by [Display(AutoGenerateField=false)]."
            : "DataTable bound through ITypedList. Columns and CLR types come straight from DataTable.Columns — no annotations, so headers match column names verbatim and no formatting is applied.";

        public AutoColumnsSampleViewModel()
        {
            StartLoad(async () =>
            {
                var customerData = await Task.Run(() =>
                    SampleDataGenerator.Generate(25, CustomerGenerator.Create));
                Customers = new ObservableCollection<Customer>(customerData);

                var table = await Task.Run(() => VendorTableGenerator.Create(rowCount: 25));
                VendorTableView = new ListCollectionView(table.DefaultView);
            });
        }
    }
}
