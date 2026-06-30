using WWControls.Core;

namespace WWControls.Wpf
{
    /// <summary>
    /// Display-friendly wrapper around a <see cref="SearchType"/> for the
    /// <see cref="SearchTypeSelector"/> dropdown.
    /// </summary>
    public sealed class SearchTypeOption
    {
        public SearchType SearchType { get; }
        public string DisplayName { get; }

        public SearchTypeOption(SearchType searchType, string displayName)
        {
            SearchType = searchType;
            DisplayName = displayName;
        }
    }
}
