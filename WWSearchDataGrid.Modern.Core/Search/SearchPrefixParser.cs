using System.Collections.Generic;
using System.Linq;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// A single prefix shortcut entry with the operator symbol and optional user-input placeholder
    /// rendered separately for independent styling (e.g. operator="*", placeholder="value").
    /// </summary>
    public class PrefixShortcutEntry
    {
        /// <summary>The prefix characters the user types (e.g. "*", ">=", "><").</summary>
        public string Operator { get; }

        /// <summary>Placeholder hint for user input (e.g. "value", "a b"). Null when no input is needed.</summary>
        public string Placeholder { get; }

        public string Description { get; }
        public SearchType SearchType { get; }

        public PrefixShortcutEntry(string op, string placeholder, string description, SearchType searchType)
        {
            Operator = op;
            Placeholder = placeholder;
            Description = description;
            SearchType = searchType;
        }
    }

    /// <summary>
    /// A named group of prefix shortcut entries (e.g. "Text Matching").
    /// </summary>
    public class PrefixShortcutGroup
    {
        public string GroupName { get; }
        public List<PrefixShortcutEntry> Entries { get; }

        public PrefixShortcutGroup(string groupName, List<PrefixShortcutEntry> entries)
        {
            GroupName = groupName;
            Entries = entries;
        }
    }

    /// <summary>
    /// Parses search prefix shortcuts from column search box text input.
    /// Users type a prefix symbol followed by a value to control the search type.
    /// Example: ">100" → GreaterThan 100, "=john" → Equals "john", "#" → IsNull, "%john%" → IsLike
    /// </summary>
    public static class SearchPrefixParser
    {
        /// <summary>
        /// Parses search text for a prefix shortcut.
        /// Returns the detected SearchType (null if no prefix), the stripped value,
        /// and optional secondary value (for Between).
        /// </summary>
        public static (SearchType? searchType, string value, string secondaryValue) Parse(string text)
        {
            return Parse(text, ColumnDataType.Unknown);
        }

        /// <summary>
        /// Parses search text for a prefix shortcut, validating against the column data type.
        /// Prefixes that map to search types unsupported by the column type are ignored.
        /// ColumnDataType.Unknown allows all prefixes.
        /// </summary>
        public static (SearchType? searchType, string value, string secondaryValue) Parse(string text, ColumnDataType columnDataType)
        {
            if (string.IsNullOrEmpty(text))
                return (null, text, null);

            // Check 2-character prefixes first (longest match wins)
            if (text.Length >= 2)
            {
                string prefix2 = text.Substring(0, 2);
                switch (prefix2)
                {
                    case ">=":
                        if (IsAllowed(SearchType.GreaterThanOrEqualTo, columnDataType))
                            return (SearchType.GreaterThanOrEqualTo, text.Substring(2).TrimStart(), null);
                        break;
                    case "<=":
                        if (IsAllowed(SearchType.LessThanOrEqualTo, columnDataType))
                            return (SearchType.LessThanOrEqualTo, text.Substring(2).TrimStart(), null);
                        break;
                    case "><":
                        if (IsAllowed(SearchType.Between, columnDataType))
                            return ParseBetween(text.Substring(2).TrimStart());
                        break;
                    case "!=":
                        if (IsAllowed(SearchType.NotEquals, columnDataType))
                            return (SearchType.NotEquals, text.Substring(2).TrimStart(), null);
                        break;
                    case "!#":
                        if (IsAllowed(SearchType.IsNotNull, columnDataType))
                            return (SearchType.IsNotNull, null, null);
                        break;
                    // IsNotLike has no prefix shortcut — use the rule filter editor instead
                    case "s#":
                        if (IsAllowed(SearchType.StartsWith, columnDataType))
                            return (SearchType.StartsWith, text.Substring(2).TrimStart(), null);
                        break;
                    case "e#":
                        if (IsAllowed(SearchType.EndsWith, columnDataType))
                            return (SearchType.EndsWith, text.Substring(2).TrimStart(), null);
                        break;
                    case "t#":
                        if (IsAllowed(SearchType.TopN, columnDataType))
                            return (SearchType.TopN, text.Substring(2).TrimStart(), null);
                        break;
                    case "b#":
                        if (IsAllowed(SearchType.BottomN, columnDataType))
                            return (SearchType.BottomN, text.Substring(2).TrimStart(), null);
                        break;
                    case "a#":
                        if (IsAllowed(SearchType.AboveAverage, columnDataType))
                            return (SearchType.AboveAverage, null, null);
                        break;
                    case "v#":
                        if (IsAllowed(SearchType.BelowAverage, columnDataType))
                            return (SearchType.BelowAverage, null, null);
                        break;
                }
            }

            // Check 1-character prefixes
            switch (text[0])
            {
                case '>':
                    if (IsAllowed(SearchType.GreaterThan, columnDataType))
                        return (SearchType.GreaterThan, text.Substring(1).TrimStart(), null);
                    break;
                case '<':
                    if (IsAllowed(SearchType.LessThan, columnDataType))
                        return (SearchType.LessThan, text.Substring(1).TrimStart(), null);
                    break;
                case '=':
                    if (IsAllowed(SearchType.Equals, columnDataType))
                        return (SearchType.Equals, text.Substring(1).TrimStart(), null);
                    break;
                case '*':
                    if (IsAllowed(SearchType.Contains, columnDataType))
                        return (SearchType.Contains, text.Substring(1).TrimStart(), null);
                    break;
                case '#':
                    if (IsAllowed(SearchType.IsNull, columnDataType))
                        return (SearchType.IsNull, null, null);
                    break;
                case '%':
                    // Keep the leading % as part of the value - it's a valid LIKE wildcard.
                    // "%p%" means "contains p", "%p" means "ends with p", "p%" means "starts with p".
                    if (IsAllowed(SearchType.IsLike, columnDataType))
                        return (SearchType.IsLike, text, null);
                    break;
                case '!':
                    if (IsAllowed(SearchType.DoesNotContain, columnDataType))
                        return (SearchType.DoesNotContain, text.Substring(1).TrimStart(), null);
                    break;
            }

            // No prefix detected (or prefix not valid for column data type)
            return (null, text, null);
        }

        /// <summary>
        /// Returns all prefix shortcuts organized into named groups (no data type filtering).
        /// </summary>
        public static List<PrefixShortcutGroup> GetPrefixShortcutGroups()
        {
            return GetPrefixShortcutGroups(ColumnDataType.Unknown);
        }

        /// <summary>
        /// Returns prefix shortcuts filtered to those valid for the given column data type.
        /// Groups with no valid entries are omitted. Unknown allows all.
        /// </summary>
        public static List<PrefixShortcutGroup> GetPrefixShortcutGroups(ColumnDataType columnDataType)
        {
            var allGroups = new List<PrefixShortcutGroup>
            {
                new PrefixShortcutGroup("Text Matching", new List<PrefixShortcutEntry>
                {
                    new PrefixShortcutEntry("*", "value", "Contains", SearchType.Contains),
                    new PrefixShortcutEntry("!", "value", "Does not contain", SearchType.DoesNotContain),
                    new PrefixShortcutEntry("s#", "value", "Starts with", SearchType.StartsWith),
                    new PrefixShortcutEntry("e#", "value", "Ends with", SearchType.EndsWith),
                    new PrefixShortcutEntry("%", "value", "Like (SQL LIKE)", SearchType.IsLike),
                }),
                new PrefixShortcutGroup("Comparison", new List<PrefixShortcutEntry>
                {
                    new PrefixShortcutEntry("=", "value", "Equals", SearchType.Equals),
                    new PrefixShortcutEntry("!=", "value", "Not equals", SearchType.NotEquals),
                    new PrefixShortcutEntry(">", "value", "Greater than", SearchType.GreaterThan),
                    new PrefixShortcutEntry(">=", "value", "Greater or equal", SearchType.GreaterThanOrEqualTo),
                    new PrefixShortcutEntry("<", "value", "Less than", SearchType.LessThan),
                    new PrefixShortcutEntry("<=", "value", "Less or equal", SearchType.LessThanOrEqualTo),
                    new PrefixShortcutEntry("><", "a b", "Between", SearchType.Between),
                }),
                new PrefixShortcutGroup("Null Checks", new List<PrefixShortcutEntry>
                {
                    new PrefixShortcutEntry("#", null, "Is null", SearchType.IsNull),
                    new PrefixShortcutEntry("!#", null, "Is not null", SearchType.IsNotNull),
                }),
                new PrefixShortcutGroup("Aggregation", new List<PrefixShortcutEntry>
                {
                    new PrefixShortcutEntry("t#", "N", "Top N", SearchType.TopN),
                    new PrefixShortcutEntry("b#", "N", "Bottom N", SearchType.BottomN),
                    new PrefixShortcutEntry("a#", null, "Above average", SearchType.AboveAverage),
                    new PrefixShortcutEntry("v#", null, "Below average", SearchType.BelowAverage),
                }),
            };

            if (columnDataType == ColumnDataType.Unknown)
                return allGroups;

            // Filter entries and omit empty groups
            var filtered = new List<PrefixShortcutGroup>();
            foreach (var group in allGroups)
            {
                var validEntries = group.Entries
                    .Where(e => SearchTypeRegistry.IsValidForDataType(e.SearchType, columnDataType))
                    .ToList();

                if (validEntries.Count > 0)
                    filtered.Add(new PrefixShortcutGroup(group.GroupName, validEntries));
            }

            return filtered;
        }

        /// <summary>
        /// Returns true if the search type is allowed for the given column data type.
        /// Unknown allows everything.
        /// </summary>
        private static bool IsAllowed(SearchType searchType, ColumnDataType columnDataType)
        {
            if (columnDataType == ColumnDataType.Unknown)
                return true;

            return SearchTypeRegistry.IsValidForDataType(searchType, columnDataType);
        }

        /// <summary>
        /// Parses the Between value "10 50" into primary and secondary values.
        /// </summary>
        private static (SearchType? searchType, string value, string secondaryValue) ParseBetween(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (SearchType.Between, null, null);

            // Split on whitespace to get "10 50" → ["10", "50"]
            var parts = text.Split(new[] { ' ' }, 2, System.StringSplitOptions.RemoveEmptyEntries);

            string primary = parts.Length > 0 ? parts[0].Trim() : null;
            string secondary = parts.Length > 1 ? parts[1].Trim() : null;

            return (SearchType.Between, primary, secondary);
        }
    }
}
