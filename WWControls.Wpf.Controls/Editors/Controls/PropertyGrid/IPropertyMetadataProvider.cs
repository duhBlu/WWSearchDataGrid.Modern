using System;

namespace WWControls.Wpf.Controls.Editors
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
    /// A <see cref="IPropertyMetadataProvider"/> that also signals when its metadata changes, so the
    /// grid can re-pull <see cref="IPropertyMetadataProvider.GetPropertyMetadata"/> and refresh the
    /// affected rows live — without reassigning <c>SelectedObject</c>. A source that implements only
    /// the base <see cref="IPropertyMetadataProvider"/> keeps the snapshot behavior (metadata read
    /// once at build time).
    /// </summary>
    /// <example>
    /// <code>
    /// public class ProductRow : IObservablePropertyMetadataProvider
    /// {
    ///     public event EventHandler&lt;PropertyMetadataChangedEventArgs&gt; PropertyMetadataChanged;
    ///
    ///     private bool _isLocked;
    ///     public bool IsLocked
    ///     {
    ///         get =&gt; _isLocked;
    ///         set { _isLocked = value; PropertyMetadataChanged?.Invoke(this, new PropertyMetadataChangedEventArgs(nameof(Price))); }
    ///     }
    ///
    ///     public PropertyMetadataOverride GetPropertyMetadata(string propertyName)
    ///         =&gt; propertyName == nameof(Price) ? new PropertyMetadataOverride { IsReadOnly = IsLocked } : null;
    /// }
    /// </code>
    /// </example>
    public interface IObservablePropertyMetadataProvider : IPropertyMetadataProvider
    {
        /// <summary>
        /// Raised when the metadata for a property has changed and should be re-pulled. A null or
        /// empty <see cref="PropertyMetadataChangedEventArgs.PropertyName"/> means "all properties".
        /// </summary>
        event EventHandler<PropertyMetadataChangedEventArgs> PropertyMetadataChanged;
    }

    /// <summary>Carries the property whose metadata changed (null / empty = all).</summary>
    public sealed class PropertyMetadataChangedEventArgs : EventArgs
    {
        public PropertyMetadataChangedEventArgs(string propertyName)
        {
            PropertyName = propertyName;
        }

        /// <summary>The property whose metadata changed, or null / empty for all properties.</summary>
        public string PropertyName { get; }
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
