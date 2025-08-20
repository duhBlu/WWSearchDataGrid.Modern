using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Types of filter applications
    /// </summary>
    public enum FilterApplicationType
    {
        /// <summary>
        /// No filter applied
        /// </summary>
        None,

        /// <summary>
        /// Rule-based filter from search templates
        /// </summary>
        RuleBased,

        /// <summary>
        /// All filters cleared
        /// </summary>
        Cleared
    }
}
