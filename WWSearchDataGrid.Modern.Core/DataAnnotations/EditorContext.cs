namespace WWSearchDataGrid.Modern.Core.DataAnnotations
{
    /// <summary>
    /// The presentation host an <see cref="EditorAttributeBase"/> targets. A property may carry
    /// several editor attributes — one per host — and each host picks the editor declared for it,
    /// falling back to <see cref="Default"/>. Today only <see cref="Grid"/> and
    /// <see cref="Default"/> have a consumer (the <c>SearchDataGrid</c>); the others are recognized
    /// but inert.
    /// </summary>
    public enum EditorContext
    {
        /// <summary>The editor used in any host that has no more-specific editor attribute.</summary>
        Default = 0,

        /// <summary>The editor used when the property is shown in a data grid.</summary>
        Grid,

        /// <summary>The editor used when the property is shown in a layout control. Inert today.</summary>
        LayoutControl,

        /// <summary>The editor used when the property is shown in a property grid. Inert today.</summary>
        PropertyGrid,
    }
}
