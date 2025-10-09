using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF.Converters
{
    /// <summary>
    /// Unified converter for creating hierarchical indentation margins
    /// Supports both horizontal (left) and vertical (top) indentation
    /// </summary>
    public class HierarchicalMarginConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the indentation amount per level (default is 20 pixels)
        /// </summary>
        public double IndentPerLevel { get; set; } = 20;

        /// <summary>
        /// Gets or sets the base left margin (default is 4 pixels)
        /// </summary>
        public double BaseLeftMargin { get; set; } = 4;

        /// <summary>
        /// Gets or sets the base top margin (default is 0 pixels)
        /// </summary>
        public double BaseTopMargin { get; set; } = 0;

        /// <summary>
        /// Gets or sets the indentation direction: "Horizontal" (left) or "Vertical" (top)
        /// </summary>
        public string Direction { get; set; } = "Horizontal";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int groupLevel)
            {
                double indent = groupLevel * IndentPerLevel;

                if (Direction.Equals("Vertical", StringComparison.OrdinalIgnoreCase))
                {
                    // Vertical indentation: multiply top margin by group level
                    return new Thickness(BaseLeftMargin, BaseTopMargin + indent, 0, 0);
                }
                else // Horizontal (default)
                {
                    // Horizontal indentation: multiply left margin by group level
                    return new Thickness(BaseLeftMargin + indent, BaseTopMargin, 0, 0);
                }
            }

            return new Thickness(BaseLeftMargin, BaseTopMargin, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
