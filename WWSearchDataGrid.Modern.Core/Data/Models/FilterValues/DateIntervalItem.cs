using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Represents a date interval selection item
    /// </summary>
    public class DateIntervalItem : ObservableObject
    {
        private bool isSelected;

        public DateInterval Interval { get; set; }
        public string DisplayName { get; set; }

        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(value, ref isSelected);
        }
    }
}
