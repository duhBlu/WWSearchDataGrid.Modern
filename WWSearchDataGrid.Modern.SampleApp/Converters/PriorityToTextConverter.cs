using System;
using System.Globalization;
using System.Windows.Data;
using WWSearchDataGrid.Modern.SampleApp.Models;

namespace WWSearchDataGrid.Modern.SampleApp.Converters
{
    /// <summary>
    /// Converts Priority enum values to user-friendly display text with visual indicators.
    /// Demonstrates display-value-aware filtering with IValueConverter.
    /// </summary>
    internal class PriorityToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Priority priority)
            {
                return priority switch
                {
                    Priority.Low => "Low",
                    Priority.Normal => "Normal",
                    Priority.High => "High!",
                    Priority.Urgent => "URGENT!",
                    Priority.Critical => "CRITICAL",
                    _ => value.ToString()
                };
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                var clean = text.Trim('!', ' ').ToUpperInvariant();
                if (clean == "LOW") return Priority.Low;
                if (clean == "NORMAL") return Priority.Normal;
                if (clean == "HIGH") return Priority.High;
                if (clean == "URGENT") return Priority.Urgent;
                if (clean == "CRITICAL") return Priority.Critical;
            }
            return Priority.Normal;
        }
    }
}
