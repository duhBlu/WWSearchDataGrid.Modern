using System;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Special class to represent null values with proper display text
    /// Provides consistent null value representation across the application
    /// </summary>
    public sealed class NullDisplayValue : IEquatable<NullDisplayValue>
    {
        private static readonly Lazy<NullDisplayValue> _instance = new Lazy<NullDisplayValue>(() => new NullDisplayValue());
        
        /// <summary>
        /// Gets the singleton instance of NullDisplayValue
        /// </summary>
        public static NullDisplayValue Instance => _instance.Value;
        
        private NullDisplayValue() { }
        
        public override string ToString() => "(null)";
        
        public override bool Equals(object obj) => obj is NullDisplayValue;
        
        public bool Equals(NullDisplayValue other) => other != null;
        
        public override int GetHashCode() => 0; // All null display values are equal
        
        public static bool operator ==(NullDisplayValue left, NullDisplayValue right) => Equals(left, right);
        
        public static bool operator !=(NullDisplayValue left, NullDisplayValue right) => !Equals(left, right);
    }
}