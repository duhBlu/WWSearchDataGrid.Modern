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
        SingleComboBox,
        SingleTextBox,
        DualComboBox,
        DualDateTimePicker,
        NumericUpDown,
        NoInput,
        DateTimePickerList,
        ComboBoxList,
        DateIntervalCheckList
    }
}
