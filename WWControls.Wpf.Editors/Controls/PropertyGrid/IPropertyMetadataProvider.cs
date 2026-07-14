namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// Implemented by a <see cref="WWPropertyGrid"/> source object to supply property metadata at
    /// runtime, overriding the static attributes the grid would otherwise read by reflection.
    ///
    /// C# attributes are compile-time constants — <c>[Browsable(component.IsEditable)]</c> is not
    /// expressible. A source that needs state-dependent metadata implements this interface and
    /// returns overrides computed from its current state.
    /// <code>
    /// public class ProductRow : IPropertyMetadataProvider
    /// {
    ///     public bool IsLocked { get; set; }
    ///
    ///     public PropertyMetadataOverride GetPropertyMetadata(string propertyName)
    ///     {
    ///         switch (propertyName)
    ///         {
    ///             case nameof(Price):
    ///                 return new PropertyMetadataOverride { IsReadOnly = IsLocked };
    ///             case nameof(InternalId):
    ///                 return new PropertyMetadataOverride { Browsable = false };
    ///             default:
    ///                 return null; // no override, use static attributes
    ///         }
    ///     }
    /// }
    /// </code>
    /// </summary>
    public interface IPropertyMetadataProvider
    {
        /// <summary>
        /// Returns runtime overrides for the given property, or null to use static attributes only.
        /// Called once per property while the grid builds its item list.
        /// </summary>
        PropertyMetadataOverride GetPropertyMetadata(string propertyName);
    }

    /// <summary>
    /// Runtime overrides for property-grid metadata. Null fields mean "use the static attribute
    /// value"; non-null fields override the corresponding attribute.
    /// </summary>
    public class PropertyMetadataOverride
    {
        /// <summary>Overrides <c>[Browsable]</c>. Null = use attribute. False = hide from the grid.</summary>
        public bool? Browsable { get; set; }

        /// <summary>Overrides <c>[ReadOnly]</c>. Null = use attribute.</summary>
        public bool? IsReadOnly { get; set; }

        /// <summary>Overrides <c>[Category]</c>. Null = use attribute.</summary>
        public string Category { get; set; }

        /// <summary>Overrides <c>[DisplayName]</c>. Null = use attribute.</summary>
        public string DisplayName { get; set; }

        /// <summary>Overrides <c>[Description]</c>. Null = use attribute.</summary>
        public string Description { get; set; }

        /// <summary>Overrides <c>[PropertyOrder]</c>. Null = use attribute.</summary>
        public int? PropertyOrder { get; set; }
    }
}
