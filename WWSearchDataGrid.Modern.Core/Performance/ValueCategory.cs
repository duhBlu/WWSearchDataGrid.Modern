namespace WWSearchDataGrid.Modern.Core.Performance
{
    /// <summary>
    /// Categorizes values for proper distinction between null, empty, whitespace, and normal values
    /// </summary>
    public enum ValueCategory
    {
        /// <summary>
        /// Regular non-null, non-empty values
        /// </summary>
        Normal,
        
        /// <summary>
        /// Null values
        /// </summary>
        Null,
        
        /// <summary>
        /// String.Empty values
        /// </summary>
        Empty,
        
        /// <summary>
        /// Strings containing only whitespace
        /// </summary>
        Whitespace
    }
}