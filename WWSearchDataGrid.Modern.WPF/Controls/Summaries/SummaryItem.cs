using System.Windows;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// One summary definition on a column — an aggregate function plus an optional display
    /// format. Declared inside <see cref="GridColumn.TotalSummaries"/> (computed over the
    /// filtered rows, shown in the grid's total summary row) or
    /// <see cref="GridColumn.GroupSummaries"/> (computed per group, shown inline in each
    /// group header). A <see cref="Freezable"/> so the owning
    /// <see cref="System.Windows.FreezableCollection{T}"/> raises Changed for item-level
    /// edits and the grid recomputes.
    /// </summary>
    public class SummaryItem : Freezable
    {
        public static readonly DependencyProperty SummaryTypeProperty =
            DependencyProperty.Register(
                nameof(SummaryType),
                typeof(SummaryItemType),
                typeof(SummaryItem),
                new PropertyMetadata(SummaryItemType.Count));

        /// <summary>The aggregate function this item computes.</summary>
        public SummaryItemType SummaryType
        {
            get => (SummaryItemType)GetValue(SummaryTypeProperty);
            set => SetValue(SummaryTypeProperty, value);
        }

        public static readonly DependencyProperty DisplayFormatProperty =
            DependencyProperty.Register(
                nameof(DisplayFormat),
                typeof(string),
                typeof(SummaryItem),
                new PropertyMetadata(null));

        /// <summary>
        /// Format for the rendered summary text. A composite format containing <c>{0</c>
        /// (e.g. <c>"Sum: {0:C2}"</c>) replaces the whole text; a plain format specifier
        /// (e.g. <c>"C2"</c>, <c>"N0"</c>) formats just the value inside the default
        /// <c>Function=value</c> text. When unset, the value falls back to the column's
        /// effective display format (<c>DisplayStringFormat</c> family) and then to
        /// <c>ToString()</c>.
        /// </summary>
        public string DisplayFormat
        {
            get => (string)GetValue(DisplayFormatProperty);
            set => SetValue(DisplayFormatProperty, value);
        }

        public static readonly DependencyProperty FieldNameProperty =
            DependencyProperty.Register(
                nameof(FieldName),
                typeof(string),
                typeof(SummaryItem),
                new PropertyMetadata(null));

        /// <summary>
        /// The field this entry aggregates. In <see cref="GridColumn.TotalSummaries"/>,
        /// null/empty aggregates the owning column's own field, while a foreign target renders
        /// caption-qualified in that column's totals cell. In the grid-level
        /// <see cref="SearchDataGrid.GroupSummaries"/> / <see cref="SearchDataGrid.FixedTotalSummaries"/>
        /// collections the target is required for value aggregates (there is no owning column);
        /// a no-FieldName Count entry renders as the bare row count.
        /// </summary>
        public string FieldName
        {
            get => (string)GetValue(FieldNameProperty);
            set => SetValue(FieldNameProperty, value);
        }

        public static readonly DependencyProperty PrefixProperty =
            DependencyProperty.Register(
                nameof(Prefix),
                typeof(string),
                typeof(SummaryItem),
                new PropertyMetadata(null));

        /// <summary>
        /// Literal text rendered before the formatted value (e.g. <c>"Count="</c>). When either
        /// <see cref="Prefix"/> or <see cref="Suffix"/> is set, the entry renders as
        /// <c>Prefix + value + Suffix</c> instead of the default <c>Function=value</c> text.
        /// </summary>
        public string Prefix
        {
            get => (string)GetValue(PrefixProperty);
            set => SetValue(PrefixProperty, value);
        }

        public static readonly DependencyProperty SuffixProperty =
            DependencyProperty.Register(
                nameof(Suffix),
                typeof(string),
                typeof(SummaryItem),
                new PropertyMetadata(null));

        /// <summary>Literal text rendered after the formatted value. See <see cref="Prefix"/>.</summary>
        public string Suffix
        {
            get => (string)GetValue(SuffixProperty);
            set => SetValue(SuffixProperty, value);
        }

        public static readonly DependencyProperty AlignmentProperty =
            DependencyProperty.Register(
                nameof(Alignment),
                typeof(SummaryItemAlignment),
                typeof(SummaryItem),
                new PropertyMetadata(SummaryItemAlignment.Right));

        /// <summary>
        /// Which side of a horizontal summary run this entry renders on (group headers, fixed
        /// total summary panel). Ignored by the column-aligned total summary cells.
        /// </summary>
        public SummaryItemAlignment Alignment
        {
            get => (SummaryItemAlignment)GetValue(AlignmentProperty);
            set => SetValue(AlignmentProperty, value);
        }

        public static readonly DependencyProperty OrderIndexProperty =
            DependencyProperty.Register(
                nameof(OrderIndex),
                typeof(int),
                typeof(SummaryItem),
                new PropertyMetadata(0));

        /// <summary>
        /// Position of this entry within its summary run, across every column's summaries
        /// (lower renders first; ties break by column order, then declaration order). The
        /// View Totals editor rewrites these when the user reorders entries.
        /// </summary>
        public int OrderIndex
        {
            get => (int)GetValue(OrderIndexProperty);
            set => SetValue(OrderIndexProperty, value);
        }

        public static readonly DependencyProperty PrefixStyleProperty =
            DependencyProperty.Register(
                nameof(PrefixStyle),
                typeof(SummaryTextStyle),
                typeof(SummaryItem),
                new PropertyMetadata(null));

        /// <summary>
        /// Look of the prefix segment. In the default <c>Function=value</c> form (no
        /// <see cref="Prefix"/> / <see cref="Suffix"/>) this segment is the <c>Function=</c> /
        /// <c>Function(Caption)=</c> label, so styling it colors the label. Null = inherit the
        /// surface's base look.
        /// </summary>
        public SummaryTextStyle PrefixStyle
        {
            get => (SummaryTextStyle)GetValue(PrefixStyleProperty);
            set => SetValue(PrefixStyleProperty, value);
        }

        public static readonly DependencyProperty ValueStyleProperty =
            DependencyProperty.Register(
                nameof(ValueStyle),
                typeof(SummaryTextStyle),
                typeof(SummaryItem),
                new PropertyMetadata(null));

        /// <summary>Look of the formatted-value segment. Null = inherit the surface's base look.</summary>
        public SummaryTextStyle ValueStyle
        {
            get => (SummaryTextStyle)GetValue(ValueStyleProperty);
            set => SetValue(ValueStyleProperty, value);
        }

        public static readonly DependencyProperty SuffixStyleProperty =
            DependencyProperty.Register(
                nameof(SuffixStyle),
                typeof(SummaryTextStyle),
                typeof(SummaryItem),
                new PropertyMetadata(null));

        /// <summary>
        /// Look of the suffix segment (unused in the default form, which has no suffix). Null =
        /// inherit the surface's base look.
        /// </summary>
        public SummaryTextStyle SuffixStyle
        {
            get => (SummaryTextStyle)GetValue(SuffixStyleProperty);
            set => SetValue(SuffixStyleProperty, value);
        }

        protected override Freezable CreateInstanceCore() => new SummaryItem();
    }
}
