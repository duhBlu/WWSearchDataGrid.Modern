using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Event arguments for remove filter event
    /// </summary>
    public class RemoveFilterEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the RemoveFilterEventArgs class
        /// </summary>
        public RemoveFilterEventArgs(ColumnFilterInfo filterInfo)
        {
            FilterInfo = filterInfo;
        }

        /// <summary>
        /// Gets the filter information to remove
        /// </summary>
        public ColumnFilterInfo FilterInfo { get; }
    }
}
