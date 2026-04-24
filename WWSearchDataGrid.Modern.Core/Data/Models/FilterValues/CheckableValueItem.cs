namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Represents a single checkable column value in the Filter Values tab.
    /// Each item maps to a distinct value in the column with its occurrence count.
    /// </summary>
    public class CheckableValueItem : ObservableObject
    {
        private bool _isChecked;
        private bool _isVisible = true;

        /// <summary>
        /// Gets or sets whether this value is checked (included in the filter).
        /// </summary>
        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(value, ref _isChecked);
        }

        /// <summary>
        /// Gets or sets whether this item is visible (used for search text filtering).
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(value, ref _isVisible);
        }

        /// <summary>
        /// The actual column value used for building filter expressions.
        /// </summary>
        public object RawValue { get; set; }

        /// <summary>
        /// The formatted display text shown to the user.
        /// Uses the DisplayValueProvider if configured, otherwise ToString().
        /// </summary>
        public string DisplayValue { get; set; }

        /// <summary>
        /// The number of rows that have this value.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// True when this item represents the null/blank entry.
        /// </summary>
        public bool IsBlank { get; set; }
    }
}
