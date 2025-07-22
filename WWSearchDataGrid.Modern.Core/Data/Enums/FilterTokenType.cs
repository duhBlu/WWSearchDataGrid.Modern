using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Types of filter tokens in the Filter Panel
    /// </summary>
    public enum FilterTokenType
    {
        /// <summary>
        /// Opening bracket token (e.g., "[")
        /// </summary>
        OpenBracket,

        /// <summary>
        /// Column name token (e.g., "ColumnName")
        /// </summary>
        ColumnName,

        /// <summary>
        /// Search type token (e.g., "is any of", "contains")
        /// </summary>
        SearchType,

        /// <summary>
        /// Individual value token (e.g., "'Value1'")
        /// </summary>
        Value,

        /// <summary>
        /// Operator token between values (e.g., "and")
        /// </summary>
        Operator,

        /// <summary>
        /// Closing bracket token (e.g., "]")
        /// </summary>
        CloseBracket,

        /// <summary>
        /// Logical connector between search template group filters (e.g., "AND", "OR")
        /// </summary>
        GroupLogicalConnectorToken,

        /// <summary>
        /// Logical connector between search template filters (e.g., "AND", "OR")
        /// </summary>
        TemplateLogicalConnectorToken,

        /// <summary>
        /// Remove action token for the entire logical filter
        /// </summary>
        RemoveAction
    }
}
