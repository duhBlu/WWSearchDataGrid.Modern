using System.Windows;
using System.Windows.Media;

namespace WWControls.Wpf.Primitives
{
    /// <summary>
    /// Typed resource keys for the general-purpose button-content icons shipped with the WPF
    /// SearchDataGrid library — close/add/filter/etc. that recur across multiple control
    /// templates. Like <see cref="SearchTypeIconKeys"/> and <see cref="ThemeKeys"/>, these
    /// are <see cref="ComponentResourceKey"/> instances rather than loose string keys so that
    /// consumer resource scopes cannot collide and the public icon surface is discoverable
    /// from one static class.
    /// <para>
    /// Icons under this class are intended to be authored as monochrome <see cref="DrawingImage"/>
    /// resources whose color is supplied at use-time by the hosting control's
    /// <see cref="Control.Foreground"/>. Use <see cref="Icon"/> to render them so the icon
    /// follows the button's hover/pressed/disabled triggers without per-state plumbing.
    /// </para>
    /// <para>
    /// Multi-color, semantic icons (e.g. per-<see cref="SearchType"/> glyphs in the filter row)
    /// continue to live in <see cref="SearchTypeIconKeys"/> and are rendered without tinting.
    /// </para>
    /// </summary>
    /// <remarks>
    /// XAML usage:
    /// <code>
    /// &lt;sdg:Icon IconSource="{StaticResource {x:Static sdg:IconKeys.IconClear}}"
    ///              IconBrush="{TemplateBinding Foreground}"
    ///              Width="12" Height="12" /&gt;
    /// </code>
    /// </remarks>
    public static class IconKeys
    {
        // ─── Actions ───────────────────────────────────────────────────────────────────

        /// <summary>Close / clear / remove "X" icon — generic dismiss action.</summary>
        public static ComponentResourceKey IconClear { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconClear));

        /// <summary>Add icon ("+") — used inline on filter rows / condition groups for "add condition".</summary>
        public static ComponentResourceKey IconAdd { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconAdd));

        // ─── Navigation ────────────────────────────────────────────────────────────────

        /// <summary>Chevron pointing down — dropdown trigger, move-down button, expand-downward affordance.</summary>
        public static ComponentResourceKey IconChevronDown { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconChevronDown));

        /// <summary>Chevron pointing up — collapse, move-up button, numeric up/down increment.</summary>
        public static ComponentResourceKey IconChevronUp { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconChevronUp));
        
        public static ComponentResourceKey IconChevronLeft { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconChevronLeft));
        
        public static ComponentResourceKey IconChevronRight { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconChevronRight));

        public static ComponentResourceKey IconCaretRight { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconCaretRight));
        public static ComponentResourceKey IconCaretLeft { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconCaretLeft));
        public static ComponentResourceKey IconCaretUp { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconCaretUp));
        public static ComponentResourceKey IconCaretDown { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconCaretDown));


        public static ComponentResourceKey IconEdit { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconEdit));
        
        public static ComponentResourceKey IconCheck { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconCheck));
        
        public static ComponentResourceKey IconCheckIntermediate { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconCheckIntermediate));

        public static ComponentResourceKey IconExpandPanel { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconExpandPanel));
        
        public static ComponentResourceKey IconCollapsePanel { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconCollapsePanel));

        // ─── Domain ────────────────────────────────────────────────────────────────────

        /// <summary>Filter funnel icon — indicates an active filter in column headers (active-filter glyph).</summary>
        public static ComponentResourceKey IconFilter { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconFilter));

        /// <summary>Clear / remove this column's active filter — paired with the active-filter context.</summary>
        public static ComponentResourceKey IconClearFilter { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconClearFilter));

        /// <summary>Open the modal Filter Editor (multi-column composer) — distinct from the bare filter funnel.</summary>
        public static ComponentResourceKey IconFilterEditor { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconFilterEditor));

        // ─── Sorting ───────────────────────────────────────────────────────────────────

        /// <summary>Sort the column in ascending order (A→Z, low→high).</summary>
        public static ComponentResourceKey IconSortAscending { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconSortAscending));

        /// <summary>Sort the column in descending order (Z→A, high→low).</summary>
        public static ComponentResourceKey IconSortDescending { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconSortDescending));

        // ─── Column Context Menu ───────────────────────────────────────────────────────

        /// <summary>Copy — used by both "Copy" and "Copy With Headers" (label disambiguates).</summary>
        public static ComponentResourceKey IconCopy { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconCopy));

        /// <summary>Best-fit column width — used for the single-column "Best Fit Column" action only.</summary>
        public static ComponentResourceKey IconBestFit { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconBestFit));

        /// <summary>Show the column chooser panel.</summary>
        public static ComponentResourceKey IconColumnChooser { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconColumnChooser));

        /// <summary>Hide the current column.</summary>
        public static ComponentResourceKey IconHideColumn { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconHideColumn));

        /// <summary>Pin the column to the left edge.</summary>
        public static ComponentResourceKey IconPinLeft { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconPinLeft));

        /// <summary>Pin the column to the right edge.</summary>
        public static ComponentResourceKey IconPinRight { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconPinRight));

        /// <summary>Unpin the column, returning it to the scrollable region.</summary>
        public static ComponentResourceKey IconUnpin { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconUnpin));

        /// <summary>Group the rows by this column — column header / group-panel action.</summary>
        public static ComponentResourceKey IconGroup { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconGroup));

        /// <summary>Remove the column from the grouping — column header / group-panel action.</summary>
        public static ComponentResourceKey IconUngroup { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconUngroup));

        /// <summary>Expand every group in the grid — group-panel "Expand All" action.</summary>
        public static ComponentResourceKey IconExpandGroups { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconExpandGroups));
      
        /// <summary>Collapse every group in the grid — group-panel "Collapse All" action.</summary>
        public static ComponentResourceKey IconCollapseGroups { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconCollapseGroups));

        public static ComponentResourceKey IconAddCondition { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconAddCondition));

        public static ComponentResourceKey IconCustomizeSummary { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconCustomizeSummary));

        public static ComponentResourceKey IconLogicalAnd { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconLogicalAnd));
        
        public static ComponentResourceKey IconLogicalOr { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconLogicalOr));
        
        public static ComponentResourceKey IconLogicalNotAnd { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconLogicalNotAnd));
        
        public static ComponentResourceKey IconLogicalNotOr { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconLogicalNotOr));

        

        public static ComponentResourceKey IconAddGroup { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconAddGroup));
        
        public static ComponentResourceKey IconCalendar { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconCalendar));

        // ─── Status ────────────────────────────────────────────────────────────────────
        // Monochrome glyphs sized to sit inside a StatusIcon badge; tinted (usually white) by
        // the hosting control's brush. One per StatusKind.

        /// <summary>Information glyph ("i") — paired with <see cref="StatusKind.Info"/>.</summary>
        public static ComponentResourceKey IconStatusInfo { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconStatusInfo));

        /// <summary>Success glyph (check mark) — paired with <see cref="StatusKind.Success"/>.</summary>
        public static ComponentResourceKey IconStatusSuccess { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconStatusSuccess));

        /// <summary>Warning glyph ("!") — paired with <see cref="StatusKind.Warning"/>.</summary>
        public static ComponentResourceKey IconStatusWarning { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconStatusWarning));

        /// <summary>Error glyph ("✕") — paired with <see cref="StatusKind.Error"/>.</summary>
        public static ComponentResourceKey IconStatusError { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconStatusError));


        public static ComponentResourceKey IconCount { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconCount));
        
        public static ComponentResourceKey IconMin { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconMin));
        
        public static ComponentResourceKey IconMax { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconMax));
        
        public static ComponentResourceKey IconSum { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconSum));
        
        public static ComponentResourceKey IconAverage { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconAverage));
        
        public static ComponentResourceKey IconTextBold { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconTextBold));
        
        public static ComponentResourceKey IconTextItalic { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconTextItalic));
        
        public static ComponentResourceKey IconTextUnderline { get; } =
            new ComponentResourceKey(typeof(IconKeys), nameof(IconTextUnderline));
    }
}
