using System.Windows;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Typed resource keys for the general-purpose button-content icons shipped with the WPF
    /// SearchDataGrid library — close/add/filter/etc. that recur across multiple control
    /// templates. Like <see cref="SearchTypeIconKeys"/> and <see cref="SdgThemeKeys"/>, these
    /// are <see cref="ComponentResourceKey"/> instances rather than loose string keys so that
    /// consumer resource scopes cannot collide and the public icon surface is discoverable
    /// from one static class.
    /// <para>
    /// Icons under this class are intended to be authored as monochrome <see cref="DrawingImage"/>
    /// resources whose color is supplied at use-time by the hosting control's
    /// <see cref="Control.Foreground"/>. Use <see cref="SdgIcon"/> to render them so the icon
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
    /// &lt;sdg:SdgIcon IconSource="{StaticResource {x:Static sdg:SdgIconKeys.IconClear}}"
    ///              IconBrush="{TemplateBinding Foreground}"
    ///              Width="12" Height="12" /&gt;
    /// </code>
    /// </remarks>
    public static class SdgIconKeys
    {
        // ─── Actions ───────────────────────────────────────────────────────────────────

        /// <summary>Close / clear / remove "X" icon — generic dismiss action.</summary>
        public static ComponentResourceKey IconClear { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconClear));

        /// <summary>Add icon ("+") — used inline on filter rows / condition groups for "add condition".</summary>
        public static ComponentResourceKey IconAdd { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconAdd));

        // ─── Navigation ────────────────────────────────────────────────────────────────

        /// <summary>Chevron pointing down — dropdown trigger, move-down button, expand-downward affordance.</summary>
        public static ComponentResourceKey IconChevronDown { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconChevronDown));

        /// <summary>Chevron pointing up — collapse, move-up button, <see cref="NumericUpDown"/> increment.</summary>
        public static ComponentResourceKey IconChevronUp { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconChevronUp));
        
        public static ComponentResourceKey IconChevronLeft { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconChevronLeft));
        
        public static ComponentResourceKey IconChevronRight { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconChevronRight));
        
        public static ComponentResourceKey IconEdit { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconEdit));
        
        public static ComponentResourceKey IconCheck { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconCheck));
        
        public static ComponentResourceKey IconCheckIntermediate { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconCheckIntermediate));

        public static ComponentResourceKey IconExpandPanel { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconExpandPanel));
        
        public static ComponentResourceKey IconCollapsePanel { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconCollapsePanel));

        // ─── Domain ────────────────────────────────────────────────────────────────────

        /// <summary>Filter funnel icon — indicates an active filter in column headers (active-filter glyph).</summary>
        public static ComponentResourceKey IconFilter { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconFilter));

        /// <summary>Clear / remove this column's active filter — paired with the active-filter context.</summary>
        public static ComponentResourceKey IconClearFilter { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconClearFilter));

        /// <summary>Open the modal Filter Editor (multi-column composer) — distinct from the bare filter funnel.</summary>
        public static ComponentResourceKey IconFilterEditor { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconFilterEditor));

        // ─── Sorting ───────────────────────────────────────────────────────────────────

        /// <summary>Sort the column in ascending order (A→Z, low→high).</summary>
        public static ComponentResourceKey IconSortAscending { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconSortAscending));

        /// <summary>Sort the column in descending order (Z→A, high→low).</summary>
        public static ComponentResourceKey IconSortDescending { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconSortDescending));

        // ─── Column Context Menu ───────────────────────────────────────────────────────

        /// <summary>Copy — used by both "Copy" and "Copy With Headers" (label disambiguates).</summary>
        public static ComponentResourceKey IconCopy { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconCopy));

        /// <summary>Best-fit column width — used for the single-column "Best Fit Column" action only.</summary>
        public static ComponentResourceKey IconBestFit { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconBestFit));

        /// <summary>Show the column chooser panel.</summary>
        public static ComponentResourceKey IconColumnChooser { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconColumnChooser));

        /// <summary>Hide the current column.</summary>
        public static ComponentResourceKey IconHideColumn { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconHideColumn));

        /// <summary>Pin the column to the left edge.</summary>
        public static ComponentResourceKey IconPinLeft { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconPinLeft));

        /// <summary>Pin the column to the right edge.</summary>
        public static ComponentResourceKey IconPinRight { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconPinRight));

        /// <summary>Unpin the column, returning it to the scrollable region.</summary>
        public static ComponentResourceKey IconUnpin { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconUnpin));
        
        
        public static ComponentResourceKey IconAddCondition { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconAddCondition));
        
        public static ComponentResourceKey IconLogicalAnd { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconLogicalAnd));
        
        public static ComponentResourceKey IconLogicalOr { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconLogicalOr));
        
        public static ComponentResourceKey IconLogicalNotAnd { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconLogicalNotAnd));
        
        public static ComponentResourceKey IconLogicalNotOr { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconLogicalNotOr));

        

        public static ComponentResourceKey IconAddGroup { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconAddGroup));
        
        public static ComponentResourceKey IconCalendar { get; } =
            new ComponentResourceKey(typeof(SdgIconKeys), nameof(IconCalendar));


    }
}
