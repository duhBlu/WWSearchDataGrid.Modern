using System.ComponentModel;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Concrete data context handed to <see cref="GridColumn.FilterRowEditTemplate"/>
    /// (and the display fallback). Implements <see cref="IDataErrorInfo"/> as a no-op so
    /// XAML <c>ValidatesOnDataErrors=True</c> bindings inside the template don't bind-fail —
    /// the filter row has no concept of validation errors today; future polish could expose
    /// per-cell errors via this surface.
    /// </summary>
    public sealed class EditGridCellData : GridCellData, IDataErrorInfo
    {
        public string Error => string.Empty;

        public string this[string columnName] => string.Empty;
    }
}
