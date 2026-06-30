using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Data;
using WWControls.SampleApp.SampleData;
using WWControls.SampleApp.SampleData.Generators;

namespace WWControls.SampleApp.Views.Samples.DataBinding
{
    public sealed partial class DataTableManualSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private DataTable? _vendorTable;

        [ObservableProperty]
        private ICollectionView? _vendorTableView;

        public DataTableManualSampleViewModel()
        {
            StartLoad(async () =>
            {
                var table = await Task.Run(() => VendorTableGenerator.Create(rowCount: 25));
                VendorTable = table;
                VendorTableView = new ListCollectionView(table.DefaultView);
            });
        }
    }
}
