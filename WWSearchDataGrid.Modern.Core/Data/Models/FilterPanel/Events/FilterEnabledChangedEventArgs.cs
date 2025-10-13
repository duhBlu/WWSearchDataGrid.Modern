using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Event arguments for filter enabled changed event
    /// </summary>
    public class FilterEnabledChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the FilterEnabledChangedEventArgs class
        /// </summary>
        public FilterEnabledChangedEventArgs(bool enabled)
        {
            Enabled = enabled;
        }

        /// <summary>
        /// Gets whether filters are enabled
        /// </summary>
        public bool Enabled { get; }
    }


}
