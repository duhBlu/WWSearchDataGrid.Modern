using System;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Represents a data transformation that operates on the entire dataset
    /// before traditional row-by-row filtering is applied
    /// </summary>
    public class DataTransformation
    {
        /// <summary>
        /// Gets or sets the type of transformation to apply
        /// </summary>
        public DataTransformationType Type { get; set; }

        /// <summary>
        /// Gets or sets the column property path to transform on
        /// </summary>
        public string ColumnPath { get; set; }

        /// <summary>
        /// Gets or sets the parameter for the transformation (e.g., N for TopN/BottomN)
        /// </summary>
        public object Parameter { get; set; }

        /// <summary>
        /// Gets or sets the data type of the column being transformed
        /// </summary>
        public ColumnDataType DataType { get; set; }

        /// <summary>
        /// Gets or sets the column name for display purposes
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Initializes a new instance of the DataTransformation class
        /// </summary>
        public DataTransformation()
        {
            Type = DataTransformationType.None;
        }

        /// <summary>
        /// Initializes a new instance of the DataTransformation class
        /// </summary>
        /// <param name="type">The type of transformation</param>
        /// <param name="columnPath">The column property path</param>
        /// <param name="parameter">The transformation parameter</param>
        /// <param name="dataType">The column data type</param>
        /// <param name="columnName">The column name</param>
        public DataTransformation(DataTransformationType type, string columnPath, object parameter = null, ColumnDataType dataType = ColumnDataType.String, string columnName = null)
        {
            Type = type;
            ColumnPath = columnPath;
            Parameter = parameter;
            DataType = dataType;
            ColumnName = columnName;
        }

        /// <summary>
        /// Creates a copy of this transformation
        /// </summary>
        /// <returns>A new DataTransformation instance with the same values</returns>
        public DataTransformation Clone()
        {
            return new DataTransformation(Type, ColumnPath, Parameter, DataType, ColumnName);
        }

        /// <summary>
        /// Gets a human-readable description of this transformation
        /// </summary>
        /// <returns>Description string</returns>
        public string GetDescription()
        {
            switch (Type)
            {
                case DataTransformationType.TopN:
                    return $"Top {Parameter} by {ColumnName}";
                case DataTransformationType.BottomN:
                    return $"Bottom {Parameter} by {ColumnName}";
                case DataTransformationType.AboveAverage:
                    return $"Above average {ColumnName}";
                case DataTransformationType.BelowAverage:
                    return $"Below average {ColumnName}";
                case DataTransformationType.Unique:
                    return $"Unique {ColumnName}";
                case DataTransformationType.Duplicate:
                    return $"Duplicate {ColumnName}";
                case DataTransformationType.None:
                default:
                    return "No transformation";
            }
        }

        /// <summary>
        /// Determines if this transformation requires a numeric column
        /// </summary>
        /// <returns>True if the transformation requires numeric data</returns>
        public bool RequiresNumericData()
        {
            return Type == DataTransformationType.TopN ||
                   Type == DataTransformationType.BottomN ||
                   Type == DataTransformationType.AboveAverage ||
                   Type == DataTransformationType.BelowAverage;
        }

        /// <summary>
        /// Determines if this transformation requires a parameter value
        /// </summary>
        /// <returns>True if the transformation requires a parameter</returns>
        public bool RequiresParameter()
        {
            return Type == DataTransformationType.TopN ||
                   Type == DataTransformationType.BottomN;
        }

        /// <summary>
        /// Validates that this transformation is properly configured
        /// </summary>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(ColumnPath))
                return false;

            if (RequiresParameter() && Parameter == null)
                return false;

            if (RequiresParameter() && !int.TryParse(Parameter.ToString(), out int value))
                return false;

            if (RequiresParameter() && int.Parse(Parameter.ToString()) <= 0)
                return false;

            return true;
        }

        /// <summary>
        /// Gets the parameter as an integer value
        /// </summary>
        /// <returns>The parameter as an integer, or 0 if not applicable</returns>
        public int GetParameterAsInt()
        {
            if (Parameter != null && int.TryParse(Parameter.ToString(), out int value))
                return value;
            return 0;
        }
    }
}