using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Data;
using WWSearchDataGrid.Modern.SampleApp.SampleData;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Generators;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.DataBinding
{
    public sealed partial class DataTableAutoGenSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private DataTable? _vendorTable;

        [ObservableProperty]
        private ICollectionView? _vendorTableView;

        public DataTableAutoGenSampleViewModel()
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
