using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Display-friendly wrapper around a <see cref="SearchType"/> for the
    /// <see cref="SearchTypeSelector"/> dropdown. Pairs the enum value with the localized
    /// display name supplied by <see cref="SearchTypeRegistry"/>.
    /// </summary>
    /// <remarks>
    /// Phase 5 dropped the <c>PrefixShortcut</c> field — the selector is now the only path
    /// to switching search types; the legacy prefix-tokens UI is gone.
    /// </remarks>
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
