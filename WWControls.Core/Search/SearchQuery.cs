using System;

namespace WWControls.Core
{
    /// <summary>
    /// Normalized search input: a pre-lowercased, trimmed term used for case-insensitive substring
    /// matching. Both the query text and the candidate key are compared with <see cref="StringComparison.Ordinal"/>
    /// after lowercasing for speed.
    /// </summary>
    public sealed class SearchQuery
    {
        public static readonly SearchQuery Empty = new SearchQuery(string.Empty);

        public string Text { get; }
        public bool IsEmpty { get; }

        private SearchQuery(string normalized)
        {
            Text = normalized;
            IsEmpty = string.IsNullOrEmpty(normalized);
        }

        /// <summary>
        /// Returns true when <paramref name="searchKey"/> contains the query text. An empty query matches
        /// everything. <paramref name="searchKey"/> is lowercased here so callers can pass raw text.
        /// </summary>
        public bool Matches(string searchKey)
        {
            if (IsEmpty) return true;
            if (string.IsNullOrEmpty(searchKey)) return false;
            return searchKey.ToLowerInvariant().IndexOf(Text, StringComparison.Ordinal) >= 0;
        }

        public static SearchQuery Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return Empty;
            return new SearchQuery(raw.Trim().ToLowerInvariant());
        }
    }
}
