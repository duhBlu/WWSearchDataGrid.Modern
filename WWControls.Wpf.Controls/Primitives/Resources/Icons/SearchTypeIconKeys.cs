using System.Windows;
using System.Windows.Media;
using WWControls.Core;

namespace WWControls.Wpf.Primitives
{
    /// <summary>
    /// Typed resource keys for the per-<see cref="SearchType"/> and per-<see cref="DateInterval"/>
    /// <see cref="DrawingImage"/> icons shipped with the WPF SearchDataGrid library. Like the
    /// style keys in <see cref="ThemeKeys"/>, these are <see cref="ComponentResourceKey"/>
    /// instances rather than loose string keys so that:
    /// <list type="bullet">
    ///   <item>Consumer resource scopes cannot collide with our icon names by accident.</item>
    ///   <item>Consumers retheme an icon by redefining a single entry under the same key.</item>
    ///   <item>The set of public icons is discoverable through this static class instead of
    ///         pattern-matching against arbitrary strings in a merged ResourceDictionary.</item>
    /// </list>
    /// All internal building blocks of the icon dictionary (Geometry, DrawingGroup, etc.) remain
    /// implementation-detail string keys inside <c>Resources/Icons/SearchTypeIcons.xaml</c> — only
    /// the final <c>DrawingImage</c> per icon is exposed here.
    /// </summary>
    /// <remarks>
    /// Lookup pattern from code:
    /// <code>
    /// var image = (DrawingImage)Application.Current.FindResource(SearchTypeIconKeys.Equals);
    /// </code>
    /// Lookup pattern from XAML:
    /// <code>
    /// &lt;Image Source="{StaticResource {x:Static sdg:SearchTypeIconKeys.Equals}}" /&gt;
    /// </code>
    /// </remarks>
    public static class SearchTypeIconKeys
    {
        // ─── SearchType — basic value comparisons ──────────────────────────────────────

        /// <summary>Icon for <see cref="SearchType.Equals"/>.</summary>
        public static new ComponentResourceKey Equals { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Equals));

        /// <summary>Icon for <see cref="SearchType.NotEquals"/>.</summary>
        public static ComponentResourceKey NotEquals { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(NotEquals));

        /// <summary>Icon for <see cref="SearchType.GreaterThan"/>.</summary>
        public static ComponentResourceKey GreaterThan { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(GreaterThan));

        /// <summary>Icon for <see cref="SearchType.GreaterThanOrEqualTo"/>.</summary>
        public static ComponentResourceKey GreaterThanOrEqualTo { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(GreaterThanOrEqualTo));

        /// <summary>Icon for <see cref="SearchType.LessThan"/>.</summary>
        public static ComponentResourceKey LessThan { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(LessThan));

        /// <summary>Icon for <see cref="SearchType.LessThanOrEqualTo"/>.</summary>
        public static ComponentResourceKey LessThanOrEqualTo { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(LessThanOrEqualTo));

        /// <summary>Icon for <see cref="SearchType.Between"/> (and <see cref="SearchType.BetweenDates"/>).</summary>
        public static ComponentResourceKey Between { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Between));

        /// <summary>Icon for <see cref="SearchType.NotBetween"/> (and <see cref="SearchType.NotBetweenDates"/>).</summary>
        public static ComponentResourceKey NotBetween { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(NotBetween));

        // ─── SearchType — string / text matching ───────────────────────────────────────

        /// <summary>Icon for <see cref="SearchType.Contains"/>.</summary>
        public static ComponentResourceKey Contains { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Contains));

        /// <summary>Icon for <see cref="SearchType.DoesNotContain"/>.</summary>
        public static ComponentResourceKey DoesNotContain { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(DoesNotContain));

        /// <summary>Icon for <see cref="SearchType.StartsWith"/>.</summary>
        public static ComponentResourceKey StartsWith { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(StartsWith));

        /// <summary>Icon for <see cref="SearchType.EndsWith"/>.</summary>
        public static ComponentResourceKey EndsWith { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(EndsWith));

        /// <summary>Icon for <see cref="SearchType.IsLike"/>.</summary>
        public static ComponentResourceKey IsLike { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(IsLike));

        /// <summary>Icon for <see cref="SearchType.IsNotLike"/>.</summary>
        public static ComponentResourceKey IsNotLike { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(IsNotLike));

        // ─── SearchType — set membership ───────────────────────────────────────────────

        /// <summary>Icon for <see cref="SearchType.IsAnyOf"/>.</summary>
        public static ComponentResourceKey IsAnyOf { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(IsAnyOf));

        /// <summary>Icon for <see cref="SearchType.IsNoneOf"/>.</summary>
        public static ComponentResourceKey IsNoneOf { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(IsNoneOf));

        // ─── SearchType — null checks ──────────────────────────────────────────────────

        /// <summary>Icon for <see cref="SearchType.IsNull"/>.</summary>
        public static ComponentResourceKey IsNull { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(IsNull));

        /// <summary>Icon for <see cref="SearchType.IsNotNull"/>.</summary>
        public static ComponentResourceKey IsNotNull { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(IsNotNull));

        // ─── SearchType — statistical ──────────────────────────────────────────────────

        /// <summary>Icon for <see cref="SearchType.TopN"/>.</summary>
        public static ComponentResourceKey TopN { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(TopN));

        /// <summary>Icon for <see cref="SearchType.BottomN"/>.</summary>
        public static ComponentResourceKey BottomN { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(BottomN));

        /// <summary>Icon for <see cref="SearchType.AboveAverage"/>.</summary>
        public static ComponentResourceKey AboveAverage { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(AboveAverage));

        /// <summary>Icon for <see cref="SearchType.BelowAverage"/>.</summary>
        public static ComponentResourceKey BelowAverage { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(BelowAverage));

        // ─── SearchType — uniqueness ───────────────────────────────────────────────────

        /// <summary>Icon for <see cref="SearchType.Unique"/>.</summary>
        public static ComponentResourceKey Unique { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Unique));

        /// <summary>Icon for <see cref="SearchType.Duplicate"/>.</summary>
        public static ComponentResourceKey Duplicate { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Duplicate));

        // ─── DateInterval ──────────────────────────────────────────────────────────────

        /// <summary>Icon for <see cref="DateInterval.PriorThisYear"/>.</summary>
        public static ComponentResourceKey PriorThisYear { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(PriorThisYear));

        /// <summary>Icon for <see cref="DateInterval.EarlierThisYear"/>.</summary>
        public static ComponentResourceKey EarlierThisYear { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(EarlierThisYear));

        /// <summary>Icon for <see cref="DateInterval.LaterThisYear"/>.</summary>
        public static ComponentResourceKey LaterThisYear { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(LaterThisYear));

        /// <summary>Icon for <see cref="DateInterval.BeyondThisYear"/>.</summary>
        public static ComponentResourceKey BeyondThisYear { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(BeyondThisYear));

        /// <summary>Icon for <see cref="DateInterval.EarlierThisMonth"/>.</summary>
        public static ComponentResourceKey EarlierThisMonth { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(EarlierThisMonth));

        /// <summary>Icon for <see cref="DateInterval.LaterThisMonth"/>.</summary>
        public static ComponentResourceKey LaterThisMonth { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(LaterThisMonth));

        /// <summary>Icon for <see cref="DateInterval.EarlierThisWeek"/>.</summary>
        public static ComponentResourceKey EarlierThisWeek { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(EarlierThisWeek));

        /// <summary>Icon for <see cref="DateInterval.LaterThisWeek"/>.</summary>
        public static ComponentResourceKey LaterThisWeek { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(LaterThisWeek));

        /// <summary>Icon for <see cref="DateInterval.LastWeek"/>.</summary>
        public static ComponentResourceKey LastWeek { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(LastWeek));

        /// <summary>Icon for <see cref="DateInterval.NextWeek"/>.</summary>
        public static ComponentResourceKey NextWeek { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(NextWeek));

        /// <summary>Icon for <see cref="DateInterval.Yesterday"/> (and <see cref="SearchType.Yesterday"/>).</summary>
        public static ComponentResourceKey Yesterday { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Yesterday));

        /// <summary>Icon for <see cref="DateInterval.Today"/> (and <see cref="SearchType.Today"/>).</summary>
        public static ComponentResourceKey Today { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Today));

        /// <summary>Icon for <see cref="DateInterval.Tomorrow"/>.</summary>
        public static ComponentResourceKey Tomorrow { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Tomorrow));
    }
}
