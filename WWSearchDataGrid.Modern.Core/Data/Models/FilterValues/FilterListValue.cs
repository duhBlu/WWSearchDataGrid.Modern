using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Represents a value in a filter list
    /// </summary>
    public class FilterListValue : ObservableObject
    {
        private object value;

        public object Value
        {
            get => value;
            set => SetProperty(value, ref this.value);
        }
    }
}
