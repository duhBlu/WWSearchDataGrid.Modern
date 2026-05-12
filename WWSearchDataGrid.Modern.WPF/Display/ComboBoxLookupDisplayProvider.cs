using System.Collections;
using System.ComponentModel;
using WWSearchDataGrid.Modern.Core.Display;

namespace WWSearchDataGrid.Modern.WPF.Display
{
    /// <summary>
    /// Display value provider for columns whose editor is a <see cref="ComboBoxEditSettings"/>.
    /// Translates the raw cell value (typically a foreign-key id, or the item itself) into the
    /// user-facing text shown in the dropdown, so filter popups, chips, and copy commands match
    /// what the user sees in the cell rather than the underlying value.
    /// <list type="bullet">
    ///   <item><b>SelectedValuePath set</b> (foreign-key columns): raw value is an id; the
    ///   provider looks up the item in <c>ItemsSource</c> by its <see cref="_selectedValuePath"/>
    ///   property and returns its <see cref="_displayMemberPath"/> property.</item>
    ///   <item><b>DisplayMemberPath only</b>: raw value is the item itself; reads the
    ///   <see cref="_displayMemberPath"/> property off it directly.</item>
    /// </list>
    /// Reverse parsing isn't supported — filter expressions against this column compare display
    /// strings, not the raw ids.
    /// </summary>
    public class ComboBoxLookupDisplayProvider : IDisplayValueProvider
    {
        private readonly IEnumerable _itemsSource;
        private readonly string _displayMemberPath;
        private readonly string _selectedValuePath;

        public ComboBoxLookupDisplayProvider(IEnumerable itemsSource, string displayMemberPath, string selectedValuePath)
        {
            _itemsSource = itemsSource;
            _displayMemberPath = displayMemberPath;
            _selectedValuePath = selectedValuePath;
        }

        public string FormatValue(object rawValue)
        {
            if (rawValue == null) return string.Empty;

            if (!string.IsNullOrEmpty(_selectedValuePath) && _itemsSource != null)
            {
                foreach (var item in _itemsSource)
                {
                    if (item == null) continue;
                    var idValue = ReadProperty(item, _selectedValuePath);
                    if (Equals(idValue, rawValue))
                    {
                        return string.IsNullOrEmpty(_displayMemberPath)
                            ? item.ToString()
                            : ReadProperty(item, _displayMemberPath)?.ToString() ?? string.Empty;
                    }
                }
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(_displayMemberPath))
                return ReadProperty(rawValue, _displayMemberPath)?.ToString() ?? string.Empty;

            return rawValue.ToString();
        }

        public object ParseValue(string displayText) => null;

        public bool CanParse => false;

        public bool UseRawComparison => false;

        private static object ReadProperty(object item, string path)
        {
            if (item == null || string.IsNullOrEmpty(path)) return null;
            var pd = TypeDescriptor.GetProperties(item)[path];
            return pd?.GetValue(item);
        }
    }
}
