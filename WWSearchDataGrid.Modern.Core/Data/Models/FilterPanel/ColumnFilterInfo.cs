using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private string conjunction;
         private string searchTypeText;
        private string primaryValue;
        private string secondaryValue;
        private bool isDateInterval;
        private bool hasNoInputValues;
        private string valueOperatorText;
        private ObservableCollection<FilterChipComponents> filterComponents;

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

        /// <summary>
        /// Gets or sets the conjunction to be displayed before this filter (e.g., "AND", "OR")
        /// </summary>
        public string Conjunction
        {
            get => conjunction;
            set => SetProperty(value, ref conjunction);
        }

        /// <summary>
        /// Gets or sets the search operation type description (e.g., "Contains", "Between", "Is null")
        /// </summary>
        public string SearchTypeText
        {
            get => searchTypeText;
            set => SetProperty(value, ref searchTypeText);
        }

        /// <summary>
        /// Gets or sets the primary input value (e.g., the search term, first date, etc.)
        /// </summary>
        public string PrimaryValue
        {
            get => primaryValue;
            set => SetProperty(value, ref primaryValue);
        }

        /// <summary>
        /// Gets or sets the secondary input value (e.g., second date in Between operations)
        /// </summary>
        public string SecondaryValue
        {
            get => secondaryValue;
            set => SetProperty(value, ref secondaryValue);
        }

        /// <summary>
        /// Gets or sets whether this filter has date interval values
        /// </summary>
        public bool IsDateInterval
        {
            get => isDateInterval;
            set => SetProperty(value, ref isDateInterval);
        }

        /// <summary>
        /// Gets or sets whether this filter requires no input values
        /// </summary>
        public bool HasNoInputValues
        {
            get => hasNoInputValues;
            set => SetProperty(value, ref hasNoInputValues);
        }

        /// <summary>
        /// Gets or sets the operator text between primary and secondary values (e.g., "and")
        /// </summary>
        public string ValueOperatorText
        {
            get => valueOperatorText;
            set => SetProperty(value, ref valueOperatorText);
        }

        /// <summary>
        /// Gets or sets the collection of filter components for complex filters with multiple conditions
        /// </summary>
        public ObservableCollection<FilterChipComponents> FilterComponents
        {
            get
            {
                if (filterComponents == null)
                    filterComponents = new ObservableCollection<FilterChipComponents>();
                return filterComponents;
            }
            set => SetProperty(value, ref filterComponents);
        }

    }
}
