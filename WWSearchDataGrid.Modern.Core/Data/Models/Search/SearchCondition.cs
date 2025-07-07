using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Represents a search condition and associated metadata
    /// </summary>
    public class SearchCondition
    {

        #region Properties

        /// <summary>
        /// Gets or sets the type of the column being searched
        /// </summary>
        public Type TargetType { get; set; }

        /// <summary>
        /// Gets or sets the string representation of the search value
        /// </summary>
        public string StringValue { get; set; }

        /// <summary>
        /// Gets or sets the converted primary search value
        /// </summary>
        public object PrimaryValue { get; set; }

        /// <summary>
        /// Gets or sets the converted secondary search value (for Between operations)
        /// </summary>
        public object SecondaryValue { get; set; }

        /// <summary>
        /// Gets whether the search value is a boolean
        /// </summary>
        public bool IsBoolean { get; private set; }

        /// <summary>
        /// Gets whether the search value is a date/time
        /// </summary>
        public bool IsDateTime { get; private set; }

        /// <summary>
        /// Gets whether the search value is a numeric value
        /// </summary>
        public bool IsNumeric { get; private set; }

        /// <summary>
        /// Gets whether the search value is a string
        /// </summary>
        public bool IsString { get; private set; }

        /// <summary>
        /// Gets or sets the search operation type
        /// </summary>
        public SearchType SearchType { get; set; } = SearchType.StartsWith;

        /// <summary>
        /// Gets or sets the numeric value for TopN, BottomN operations
        /// </summary>
        public int? CountValue { get; set; }

        /// <summary>
        /// Gets or sets the date interval type for DateInterval operations
        /// </summary>
        public DateInterval? DateIntervalValue { get; set; }

        /// <summary>
        /// Gets or sets the raw primary search value
        /// </summary>
        public object RawPrimaryValue { get; set; }

        /// <summary>
        /// Gets or sets the raw secondary search value
        /// </summary>
        public object RawSecondaryValue { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the SearchCondition class
        /// </summary>
        public SearchCondition()
        {
        }

        /// <summary>
        /// Initializes a new instance of the SearchCondition class with specified values
        /// </summary>
        /// <param name="targetType">Type of the column being searched</param>
        /// <param name="searchType">Type of search operation</param>
        /// <param name="primaryValue">Primary search value</param>
        /// <param name="secondaryValue">Secondary search value (optional)</param>
        public SearchCondition(Type targetType, SearchType searchType, object primaryValue, object secondaryValue = null, int? countValue = null, DateInterval? dateIntervalValue = null)
        {
            TargetType = targetType;
            SearchType = searchType;
            RawPrimaryValue = primaryValue;
            RawSecondaryValue = secondaryValue;
            CountValue = countValue;
            DateIntervalValue = dateIntervalValue;
            ConvertValues();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Resets all type flags
        /// </summary>
        private void ResetTypeFlags()
        {
            IsBoolean = false;
            IsDateTime = false;
            IsNumeric = false;
            IsString = false;
        }

        /// <summary>
        /// Clears all converted values
        /// </summary>
        private void ClearConvertedValues()
        {
            IsNumeric = false;
            IsDateTime = false;
            IsString = false;
            PrimaryValue = null;
            SecondaryValue = null;
            StringValue = null;
        }

        /// <summary>
        /// Converts an input value to the appropriate target type
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <returns>Converted value</returns>
        private object ConvertValueInternal(object value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()) || value.ToString() == "custom filter")
            {
                return null;
            }

            string stringValue = value.ToString();

            if (TargetType != null)
            {
                var underlyingType = Nullable.GetUnderlyingType(TargetType) ?? TargetType;

                if (underlyingType == typeof(DateTime))
                {
                    if (DateTime.TryParse(stringValue, out DateTime dateTimeValue))
                        return Convert.ChangeType(value, underlyingType);
                }
                else if (ReflectionHelper.IsNumericType(underlyingType))
                {
                    if (decimal.TryParse(stringValue, out decimal decimalValue))
                        return Convert.ChangeType(value, underlyingType);
                }
                else if (underlyingType == typeof(bool))
                {
                    if (value is bool)
                        return Convert.ChangeType(value, underlyingType);
                }
                else if (underlyingType == typeof(string))
                {
                    return stringValue.ToLower();
                }
            }

            return value; // Return original value if no conversion needed
        }

        /// <summary>
        /// Ensures primary and secondary values are correctly ordered for range comparisons
        /// </summary>
        private void OrderRangeValues()
        {
            if (PrimaryValue != null && SecondaryValue != null)
            {
                if (IsNumeric)
                {
                    var primaryDecimal = TypeTranslatorHelper.ConvertToDecimal(PrimaryValue);
                    var secondaryDecimal = TypeTranslatorHelper.ConvertToDecimal(SecondaryValue);

                    if (primaryDecimal.HasValue && secondaryDecimal.HasValue && primaryDecimal > secondaryDecimal)
                    {
                        var temp = PrimaryValue;
                        PrimaryValue = SecondaryValue;
                        SecondaryValue = temp;
                    }
                }
                else if (IsDateTime)
                {
                    var primaryDate = TypeTranslatorHelper.ConvertToDateTime(PrimaryValue);
                    var secondaryDate = TypeTranslatorHelper.ConvertToDateTime(SecondaryValue);

                    if (primaryDate.HasValue && secondaryDate.HasValue && primaryDate > secondaryDate)
                    {
                        var temp = PrimaryValue;
                        PrimaryValue = SecondaryValue;
                        SecondaryValue = temp;
                    }
                }
                else if (IsString)
                {
                    var primaryString = PrimaryValue?.ToString() ?? "";
                    var secondaryString = SecondaryValue?.ToString() ?? "";

                    if (string.Compare(primaryString, secondaryString, StringComparison.OrdinalIgnoreCase) > 0)
                    {
                        var temp = PrimaryValue;
                        PrimaryValue = SecondaryValue;
                        SecondaryValue = temp;
                    }
                }
            }
        }


        /// <summary>
        /// Clears all values
        /// </summary>
        public void Clear()
        {
            RawPrimaryValue = null;
            RawSecondaryValue = null;
            ClearConvertedValues();
        }

        /// <summary>
        /// Converts raw values to the appropriate types for comparison
        /// </summary>
        public void ConvertValues()
        {
            ClearConvertedValues();
            PrimaryValue = ConvertValueInternal(RawPrimaryValue);
            SecondaryValue = ConvertValueInternal(RawSecondaryValue);
            StringValue = RawPrimaryValue.ToStringEmptyIfNull().ToLower();

            DetermineTypeFlags();

            OrderRangeValues();
        }

        private void DetermineTypeFlags()
        {
            ResetTypeFlags();

            if (TargetType != null)
            {
                var underlyingType = Nullable.GetUnderlyingType(TargetType) ?? TargetType;

                if (underlyingType == typeof(DateTime))
                {
                    IsDateTime = true;
                }
                else if (ReflectionHelper.IsNumericType(underlyingType))
                {
                    IsNumeric = true;
                }
                else if (underlyingType == typeof(bool))
                {
                    IsBoolean = true;
                }
                else
                {
                    IsString = true;
                }
            }
            else
            {
                // Fallback: try to determine type from actual values
                DetermineTypeFlagsFromValues();
            }
        }

        private void DetermineTypeFlagsFromValues()
        {
            // Try to determine type from the actual values
            var valueToCheck = RawPrimaryValue ?? RawSecondaryValue;

            if (valueToCheck != null)
            {
                if (valueToCheck is DateTime || DateTime.TryParse(valueToCheck.ToString(), out _))
                {
                    IsDateTime = true;
                }
                else if (ReflectionHelper.IsNumericValue(valueToCheck) || decimal.TryParse(valueToCheck.ToString(), out _))
                {
                    IsNumeric = true;
                }
                else if (valueToCheck is bool || bool.TryParse(valueToCheck.ToString(), out _))
                {
                    IsBoolean = true;
                }
                else
                {
                    IsString = true;
                }
            }
            else
            {
                // Ultimate fallback
                IsString = true;
            }
        }

        #endregion
    }
}
