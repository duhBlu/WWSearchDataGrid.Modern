using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Defines the UI template type for filter value input
    /// </summary>
    public enum FilterInputTemplate
    {
        SingleSearchTextBox,
        DualSearchTextBox,
        DualDateTimePicker,
        NumericUpDown,
        NoInput,
        DateTimePickerList,
        SearchTextBoxList,
        DateIntervalCheckList
    }
}
