using System;
using System.Globalization;
using System.Windows.Data;
using WWControls.Wpf.Controls.Editors;
using WWControls.Wpf.Controls.Primitives;

namespace WWControls.SampleApp.Editors.Views.Samples.Editors
{
    /// <summary>
    /// Maps a combo's <see cref="IncrementalFilteringMode"/> to the
    /// <see cref="HighlightMatchMode"/> a custom-ItemTemplate <see cref="HighlightTextBlock"/>
    /// should use (Smart highlights like Contains) — the same mapping the built-in highlight
    /// rows apply.
    /// </summary>
    public sealed class FilterModeToMatchModeConverter : IValueConverter
    {
        public static FilterModeToMatchModeConverter Instance { get; } = new FilterModeToMatchModeConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value as IncrementalFilteringMode? ?? IncrementalFilteringMode.Smart)
            {
                case IncrementalFilteringMode.StartsWith: return HighlightMatchMode.StartsWith;
                case IncrementalFilteringMode.EndsWith: return HighlightMatchMode.EndsWith;
                default: return HighlightMatchMode.Contains;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
