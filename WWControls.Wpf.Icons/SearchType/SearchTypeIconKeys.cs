using System.Windows;
using System.Windows.Media;

namespace WWControls.Wpf.Icons
{
    /// <summary>
    /// Typed <see cref="ComponentResourceKey"/> keys for the datagrid filter <see cref="DrawingImage"/>
    /// icons in <c>SearchType/SearchTypeIcons.xaml</c> - one per SearchType / DateInterval glyph.
    /// </summary>
    public static class SearchTypeIconKeys
    {
        public static new ComponentResourceKey Equals { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Equals));

        public static ComponentResourceKey NotEquals { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(NotEquals));

        public static ComponentResourceKey GreaterThan { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(GreaterThan));

        public static ComponentResourceKey GreaterThanOrEqualTo { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(GreaterThanOrEqualTo));

        public static ComponentResourceKey LessThan { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(LessThan));

        public static ComponentResourceKey LessThanOrEqualTo { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(LessThanOrEqualTo));

        public static ComponentResourceKey Between { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Between));

        public static ComponentResourceKey NotBetween { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(NotBetween));

        public static ComponentResourceKey Contains { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Contains));

        public static ComponentResourceKey DoesNotContain { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(DoesNotContain));

        public static ComponentResourceKey StartsWith { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(StartsWith));

        public static ComponentResourceKey EndsWith { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(EndsWith));

        public static ComponentResourceKey IsLike { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(IsLike));

        public static ComponentResourceKey IsNotLike { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(IsNotLike));

        public static ComponentResourceKey IsAnyOf { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(IsAnyOf));

        public static ComponentResourceKey IsNoneOf { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(IsNoneOf));

        public static ComponentResourceKey IsNull { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(IsNull));

        public static ComponentResourceKey IsNotNull { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(IsNotNull));

        public static ComponentResourceKey TopN { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(TopN));

        public static ComponentResourceKey BottomN { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(BottomN));

        public static ComponentResourceKey AboveAverage { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(AboveAverage));

        public static ComponentResourceKey BelowAverage { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(BelowAverage));

        public static ComponentResourceKey Unique { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Unique));

        public static ComponentResourceKey Duplicate { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Duplicate));

        public static ComponentResourceKey PriorThisYear { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(PriorThisYear));

        public static ComponentResourceKey EarlierThisYear { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(EarlierThisYear));

        public static ComponentResourceKey LaterThisYear { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(LaterThisYear));

        public static ComponentResourceKey BeyondThisYear { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(BeyondThisYear));

        public static ComponentResourceKey EarlierThisMonth { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(EarlierThisMonth));

        public static ComponentResourceKey LaterThisMonth { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(LaterThisMonth));

        public static ComponentResourceKey EarlierThisWeek { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(EarlierThisWeek));

        public static ComponentResourceKey LaterThisWeek { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(LaterThisWeek));

        public static ComponentResourceKey LastWeek { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(LastWeek));

        public static ComponentResourceKey NextWeek { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(NextWeek));

        public static ComponentResourceKey Yesterday { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Yesterday));

        public static ComponentResourceKey Today { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Today));

        public static ComponentResourceKey Tomorrow { get; } =
            new ComponentResourceKey(typeof(SearchTypeIconKeys), nameof(Tomorrow));

    }
}
