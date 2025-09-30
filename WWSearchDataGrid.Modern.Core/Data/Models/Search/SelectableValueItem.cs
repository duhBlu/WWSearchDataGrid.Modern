namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Represents a selectable value item for multi-value search types (IsAnyOf, IsNoneOf)
    /// </summary>
    public class SelectableValueItem : ObservableObject
    {
        private string value;
        private object selectedItem;

        /// <summary>
        /// Gets or sets the text value entered by the user
        /// </summary>
        public string Value
        {
            get => value;
            set => SetProperty(value, ref this.value);
        }

        /// <summary>
        /// Gets or sets the selected item from the dropdown
        /// </summary>
        public object SelectedItem
        {
            get => selectedItem;
            set
            {
                if (SetProperty(value, ref selectedItem))
                {
                    // Sync the Value property when an item is selected
                    if (selectedItem != null)
                    {
                        Value = selectedItem.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance with an empty value
        /// </summary>
        public SelectableValueItem()
        {
            value = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance with the specified value
        /// </summary>
        public SelectableValueItem(object initialValue)
        {
            if (initialValue != null)
            {
                value = initialValue.ToString();
                selectedItem = initialValue;
            }
            else
            {
                value = string.Empty;
            }
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }
}