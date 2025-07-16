using System.Threading.Tasks;

namespace WWSearchDataGrid.Modern.Core.Performance
{
    public interface IColumnValueProvider
    {
        Task<ColumnValueResponse> GetValuesAsync(ColumnValueRequest request);
        Task<int> GetTotalCountAsync(string columnKey);
        void InvalidateColumn(string columnKey);
    }
}