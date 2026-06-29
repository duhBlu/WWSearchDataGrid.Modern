using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// One configured summary in the View Totals editor's working copy — a (column, function)
    /// pair, or the group row count when <see cref="Column"/> is null. Mutated freely while the
    /// dialog is open; <see cref="GroupSummaryEditorViewModel.Apply"/> writes the surviving
    /// entries back as <see cref="SummaryItem"/>s on OK.
    /// </summary>
    public sealed class SummaryEditorEntry : INotifyPropertyChanged
    {
        private string _prefix;
        private string _displayFormat;
        private string _suffix;

        internal SummaryEditorEntry(GridColumn column, SummaryItemType summaryType)
        {
            Column = column;
            SummaryType = summaryType;
        }

        /// <summary>The column this summary aggregates, or null for the group row count.</summary>
        public GridColumn Column { get; }

        public SummaryItemType SummaryType { get; }

        /// <summary>True for the "Show row count" entry (no owning column).</summary>
        public bool IsRowCount => Column == null;

        /// <summary>Which side list the entry currently sits in; maintained by the move commands.</summary>
        public SummaryItemAlignment Alignment { get; set; } = SummaryItemAlignment.Right;

        /// <summary>
        /// True when the live surface renders this entry WITHOUT its column caption — a
        /// totals-cell entry aggregating the owner column's own field (the cell shows
        /// <c>Count=…</c>, only foreign targets carry <c>Function(Caption)=…</c>). Keeps the
        /// <see cref="Example"/> preview honest; the list's <see cref="DisplayName"/> stays
        /// caption-qualified either way so entries remain tellable apart.
        /// </summary>
        internal bool OmitsCaption { get; init; }

        /// <summary>List caption — <c>Count</c> for the row count, else <c>Function(Column)</c>.</summary>
        public string DisplayName
        {
            get
            {
                string function = SearchDataGrid.FunctionCaption(SummaryType);
                if (IsRowCount) return function;
                string caption = Column.HeaderCaption;
                return string.IsNullOrEmpty(caption) ? function : function + "(" + caption + ")";
            }
        }

        public string Prefix
        {
            get => _prefix;
            set { if (_prefix != value) { _prefix = value; OnPropertyChanged(); RaiseExampleChanged(); } }
        }

        public string DisplayFormat
        {
            get => _displayFormat;
            set { if (_displayFormat != value) { _displayFormat = value; OnPropertyChanged(); RaiseExampleChanged(); } }
        }

        public string Suffix
        {
            get => _suffix;
            set { if (_suffix != value) { _suffix = value; OnPropertyChanged(); RaiseExampleChanged(); } }
        }

        /// <summary>
        /// Working-copy look of the prefix segment — edited by the text-styling sub-dialog and
        /// written back by <see cref="ToSummaryItem"/>. Never null; defaults to an empty
        /// (no-override) style.
        /// </summary>
        public SummaryTextStyle PrefixStyle { get; set; } = new SummaryTextStyle();

        /// <summary>Working-copy look of the value segment. See <see cref="PrefixStyle"/>.</summary>
        public SummaryTextStyle ValueStyle { get; set; } = new SummaryTextStyle();

        /// <summary>Working-copy look of the suffix segment. See <see cref="PrefixStyle"/>.</summary>
        public SummaryTextStyle SuffixStyle { get; set; } = new SummaryTextStyle();

        /// <summary>
        /// The three preview segments against a sample value of 100, mirroring the engine's
        /// prefix / value / suffix resolution. The dialog renders these as separately styled runs.
        /// </summary>
        private (string prefix, string value, string suffix) ExampleSegments()
        {
            var culture = CultureInfo.CurrentCulture;
            string format = DisplayFormat;
            bool hasFix = !string.IsNullOrEmpty(Prefix) || !string.IsNullOrEmpty(Suffix);

            string valueText = null;
            if (!string.IsNullOrEmpty(format) && format.IndexOf("{0", System.StringComparison.Ordinal) >= 0)
            {
                try { valueText = string.Format(culture, format, 100m); }
                catch (System.FormatException) { valueText = null; }
                if (valueText != null && !hasFix)
                    return (string.Empty, valueText, string.Empty);
            }
            if (valueText == null)
                valueText = SearchDataGrid.FormatValue(100m, format, culture);

            if (hasFix)
                return (Prefix ?? string.Empty, valueText, Suffix ?? string.Empty);
            string label = OmitsCaption ? SearchDataGrid.FunctionCaption(SummaryType) : DisplayName;
            return (label + "=", valueText, string.Empty);
        }

        /// <summary>Prefix segment of the live preview.</summary>
        public string ExamplePrefix => ExampleSegments().prefix;

        /// <summary>Value segment of the live preview.</summary>
        public string ExampleValue => ExampleSegments().value;

        /// <summary>Suffix segment of the live preview.</summary>
        public string ExampleSuffix => ExampleSegments().suffix;

        /// <summary>
        /// Live preview of the whole entry text against a sample value of 100, mirroring the
        /// engine's prefix / format / suffix resolution (<c>"Count=100.00"</c> in the dialog).
        /// </summary>
        public string Example
        {
            get
            {
                var (prefix, value, suffix) = ExampleSegments();
                return prefix + value + suffix;
            }
        }

        private void RaiseExampleChanged()
        {
            OnPropertyChanged(nameof(Example));
            OnPropertyChanged(nameof(ExamplePrefix));
            OnPropertyChanged(nameof(ExampleValue));
            OnPropertyChanged(nameof(ExampleSuffix));
        }

        /// <summary>
        /// Materializes the working copy as a fresh <see cref="SummaryItem"/>.
        /// <paramref name="fieldName"/> stamps the aggregation target for per-level group
        /// summaries (null for total summaries, which always aggregate their owning column).
        /// </summary>
        internal SummaryItem ToSummaryItem(int orderIndex, string fieldName = null) => new SummaryItem
        {
            SummaryType = SummaryType,
            FieldName = string.IsNullOrEmpty(fieldName) ? null : fieldName,
            Prefix = string.IsNullOrEmpty(Prefix) ? null : Prefix,
            DisplayFormat = string.IsNullOrEmpty(DisplayFormat) ? null : DisplayFormat,
            Suffix = string.IsNullOrEmpty(Suffix) ? null : Suffix,
            Alignment = Alignment,
            OrderIndex = orderIndex,
            // Persist a null slot when the segment carries no overrides, keeping unstyled items clean.
            PrefixStyle = NormalizeStyle(PrefixStyle),
            ValueStyle = NormalizeStyle(ValueStyle),
            SuffixStyle = NormalizeStyle(SuffixStyle),
        };

        private static SummaryTextStyle NormalizeStyle(SummaryTextStyle style)
            => style == null || style.IsDefault ? null : style.Copy();

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
