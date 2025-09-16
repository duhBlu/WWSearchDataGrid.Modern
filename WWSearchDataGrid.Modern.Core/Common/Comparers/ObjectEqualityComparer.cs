using System.Collections.Generic;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Custom equality comparer for objects that handles null values properly
    /// </summary>
    internal sealed class ObjectEqualityComparer : IEqualityComparer<object>
    {
        private static readonly ObjectEqualityComparer _instance = new ObjectEqualityComparer();
        
        /// <summary>
        /// Gets the singleton instance of ObjectEqualityComparer
        /// </summary>
        public static ObjectEqualityComparer Instance => _instance;
        
        private ObjectEqualityComparer() { }
        
        public new bool Equals(object x, object y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;

            return x.Equals(y);
        }

        public int GetHashCode(object obj)
        {
            return obj?.GetHashCode() ?? 0;
        }
    }
}