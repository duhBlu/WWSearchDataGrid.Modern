using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// One computed summary value — the per-item entry behind
    /// <see cref="GridColumn.TotalSummaryTextInfo"/> (and the group-header / footer / fixed-panel
    /// run lists). Immutable; the engine rebuilds it on every recompute. The display splits into
    /// three segments — <see cref="PrefixText"/>, <see cref="ValueText"/>, <see cref="SuffixText"/> —
    /// each carrying its own <see cref="SummaryTextStyle"/> so templates can render and style the
    /// pieces independently.
    /// </summary>
    public sealed class SummaryResult
    {
        internal SummaryResult(
            SummaryItemType summaryType, object value,
            string prefixText, string valueText, string suffixText,
            SummaryTextStyle prefixStyle, SummaryTextStyle valueStyle, SummaryTextStyle suffixStyle)
        {
            SummaryType = summaryType;
            Value = value;
            PrefixText = prefixText ?? string.Empty;
            ValueText = valueText ?? string.Empty;
            SuffixText = suffixText ?? string.Empty;
            // Substitute the shared frozen default for unset slots so the render-time bindings
            // (FontWeight / Foreground / …) always resolve against a non-null style.
            PrefixStyle = prefixStyle ?? SummaryTextStyle.Default;
            ValueStyle = valueStyle ?? SummaryTextStyle.Default;
            SuffixStyle = suffixStyle ?? SummaryTextStyle.Default;
        }

        /// <summary>The aggregate function that produced this result.</summary>
        public SummaryItemType SummaryType { get; }

        /// <summary>The raw computed value (null when undefined for the data).</summary>
        public object Value { get; }

        /// <summary>
        /// Leading segment — the user's <see cref="SummaryItem.Prefix"/>, or the default
        /// <c>Function=</c> / <c>Function(Caption)=</c> label when no prefix/suffix is set.
        /// </summary>
        public string PrefixText { get; }

        /// <summary>The formatted value segment.</summary>
        public string ValueText { get; }

        /// <summary>Trailing segment — the user's <see cref="SummaryItem.Suffix"/> (empty in the default form).</summary>
        public string SuffixText { get; }

        /// <summary>Look of <see cref="PrefixText"/>; never null (a frozen default stands in for an unset slot).</summary>
        public SummaryTextStyle PrefixStyle { get; }

        /// <summary>Look of <see cref="ValueText"/>; never null (a frozen default stands in for an unset slot).</summary>
        public SummaryTextStyle ValueStyle { get; }

        /// <summary>Look of <see cref="SuffixText"/>; never null (a frozen default stands in for an unset slot).</summary>
        public SummaryTextStyle SuffixStyle { get; }

        /// <summary>The whole entry as flat text (segments concatenated) — used for tooltips and joined fallbacks.</summary>
        public string Text => PrefixText + ValueText + SuffixText;

        public override string ToString() => Text;
    }
}
