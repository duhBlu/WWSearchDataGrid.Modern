using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Enumeration of filter types
    /// </summary>
    public enum FilterType
    {
        /// <summary>
        /// Simple text-based filter from SearchControl
        /// </summary>
        Simple,

        /// <summary>
        /// Advanced multi-criteria filter from AdvancedFilterControl
        /// </summary>
        Advanced
    }
}
