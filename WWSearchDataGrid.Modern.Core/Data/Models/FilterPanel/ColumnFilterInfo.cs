using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Information about a column's active filter state
    /// </summary>
    public class ColumnFilterInfo : ObservableObject
    {
        private string columnName;
        private string bindingPath;
        private FilterType filterType;
        private string displayText;
        private bool isActive;
        private object filterData;

        /// <summary>
        /// Gets or sets the display name of the column
        /// </summary>
        public string ColumnName
        {
            get => columnName;
            set => SetProperty(value, ref columnName);
        }

        /// <summary>
        /// Gets or sets the binding path for the column
        /// </summary>
        public string BindingPath
        {
            get => bindingPath;
            set => SetProperty(value, ref bindingPath);
        }

        /// <summary>
        /// Gets or sets the type of filter applied
        /// </summary>
        public FilterType FilterType
        {
            get => filterType;
            set => SetProperty(value, ref filterType);
        }

        /// <summary>
        /// Gets or sets the display text for the filter (e.g., "Contains 'test'")
        /// </summary>
        public string DisplayText
        {
            get => displayText;
            set => SetProperty(value, ref displayText);
        }

        /// <summary>
        /// Gets or sets whether this filter is currently active
        /// </summary>
        public bool IsActive
        {
            get => isActive;
            set => SetProperty(value, ref isActive);
        }

        /// <summary>
        /// Gets or sets the filter data reference (SearchControl or SearchTemplateController)
        /// </summary>
        public object FilterData
        {
            get => filterData;
            set => SetProperty(value, ref filterData);
        }
    }
}
