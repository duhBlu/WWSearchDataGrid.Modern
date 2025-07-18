using System;

namespace WWSearchDataGrid.Modern.Core.Performance
{
    /// <summary>
    /// Metadata for a value that includes categorization and display information
    /// </summary>
    public class ValueMetadata : IEquatable<ValueMetadata>
    {
        public object Value { get; private set; }
        public ValueCategory Category { get; private set; }
        public string DisplayText { get; private set; }

        private ValueMetadata(object value, ValueCategory category, string displayText)
        {
            Value = value;
            Category = category;
            DisplayText = displayText;
        }

        /// <summary>
        /// Factory method that creates ValueMetadata with proper categorization
        /// </summary>
        /// <param name="value">The value to categorize</param>
        /// <returns>ValueMetadata with appropriate category and display text</returns>
        public static ValueMetadata Create(object value)
        {
            if (value == null)
            {
                return new ValueMetadata(null, ValueCategory.Null, "(null)");
            }

            if (value is string stringValue)
            {
                if (stringValue.Length == 0)
                {
                    return new ValueMetadata(value, ValueCategory.Empty, "(empty)");
                }

                if (string.IsNullOrWhiteSpace(stringValue))
                {
                    return new ValueMetadata(value, ValueCategory.Whitespace, "(blank)");
                }

                return new ValueMetadata(value, ValueCategory.Normal, stringValue);
            }

            // For non-string values, they are either null (handled above) or normal
            return new ValueMetadata(value, ValueCategory.Normal, value.ToString());
        }

        /// <summary>
        /// Gets the display text for this value
        /// </summary>
        /// <returns>Display text appropriate for the value category</returns>
        public string GetDisplayText()
        {
            return DisplayText;
        }

        /// <summary>
        /// Determines if this value is considered "blank" (null, empty, or whitespace)
        /// </summary>
        public bool IsBlank => Category == ValueCategory.Null || Category == ValueCategory.Empty || Category == ValueCategory.Whitespace;

        /// <summary>
        /// Determines if this value is null
        /// </summary>
        public bool IsNull => Category == ValueCategory.Null;

        /// <summary>
        /// Determines if this value is empty string
        /// </summary>
        public bool IsEmpty => Category == ValueCategory.Empty;

        /// <summary>
        /// Determines if this value is whitespace-only
        /// </summary>
        public bool IsWhitespace => Category == ValueCategory.Whitespace;

        public bool Equals(ValueMetadata other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            // Compare categories first for efficiency
            if (Category != other.Category) return false;

            // For null values, both are equal if both are null
            if (Category == ValueCategory.Null) return true;

            // For other categories, compare the actual values
            return Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ValueMetadata);
        }

        public override int GetHashCode()
        {
            // Use category-based hash codes for better distribution
            switch (Category)
            {
                case ValueCategory.Null:
                    return ValueCategory.Null.GetHashCode();
                case ValueCategory.Empty:
                    return ValueCategory.Empty.GetHashCode();
                case ValueCategory.Whitespace:
                    return ValueCategory.Whitespace.GetHashCode() ^ (Value?.GetHashCode() ?? 0);
                default:
                    return ValueCategory.Normal.GetHashCode() ^ (Value?.GetHashCode() ?? 0);
            }
        }

        public override string ToString()
        {
            return $"ValueMetadata: {DisplayText} ({Category})";
        }
    }
}