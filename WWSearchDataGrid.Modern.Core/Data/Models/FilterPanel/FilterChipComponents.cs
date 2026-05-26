using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Components of a filter chip for display and token parsing
    /// Represents the parsed components of a filter condition
    /// </summary>
    public class FilterChipComponents
    {
        /// <summary>
        /// Gets or sets the display name of the column this component filters on.
        /// Populated by the host when building chips so each template-level token group
        /// can render its own column name — preparation for groups that mix columns.
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets or sets the search operation type description
        /// </summary>
        public string SearchTypeText { get; set; }

        /// <summary>
        /// Gets or sets the primary input value
        /// </summary>
        public string PrimaryValue { get; set; }

        /// <summary>
        /// Gets or sets the secondary input value
        /// </summary>
        public string SecondaryValue { get; set; }

        /// <summary>
        /// Gets or sets the operator text between values
        /// </summary>
        public string ValueOperatorText { get; set; }

        /// <summary>
        /// Gets or sets whether this filter has date interval values
        /// </summary>
        public bool IsDateInterval { get; set; }

        /// <summary>
        /// Gets or sets whether this filter requires no input values
        /// </summary>
        public bool HasNoInputValues { get; set; }

        /// <summary>
        /// Gets or sets the operator text for this component (e.g., "AND", "OR")
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// Gets or sets the index of the SearchTemplateGroup this component belongs to
        /// </summary>
        public int GroupIndex { get; set; }

        /// <summary>
        /// Gets or sets the index of the SearchTemplate this component belongs to
        /// </summary>
        public int TemplateIndex { get; set; }

        /// <summary>
        /// Gets or sets whether this component's operator represents a group-level operator (between groups)
        /// rather than a template-level operator (within a group)
        /// </summary>
        public bool IsGroupLevelOperator { get; set; }

        /// <summary>
        /// Gets or sets the collection of individual values for multi-value filters
        /// </summary>
        public ObservableCollection<string> ValueItems { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// Gets whether this component has multiple value items
        /// </summary>
        public bool HasMultipleValues => ValueItems?.Count > 0;

        /// <summary>
        /// Populates <see cref="ValueItems"/> from a bracketed list embedded in
        /// <see cref="PrimaryValue"/> — the shape <c>FormatMultiValueFilter</c> /
        /// <c>FormatDateListFilter</c> emit for <c>IsAnyOf</c> / <c>IsNoneOf</c> /
        /// <c>IsOnAnyOfDates</c>. Bracket delimiters are the authoritative signal that the
        /// value is a list; bare comma-containing single values (e.g. a date formatted as
        /// "Friday, December 19, 2025") are intentionally left alone so the chip renders one
        /// token instead of splitting on prose commas.
        /// </summary>
        public void ParsePrimaryValueAsMultipleValues()
        {
            if (string.IsNullOrEmpty(PrimaryValue) || ValueItems.Count > 0) return;

            var bracketMatch = Regex.Match(PrimaryValue, @"\[([^\]]+)\]");
            if (!bracketMatch.Success) return;

            var listContent = bracketMatch.Groups[1].Value;
            var items = listContent.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in items)
            {
                var cleanItem = item.Trim().Trim('\'', '"');
                if (!string.IsNullOrEmpty(cleanItem))
                {
                    ValueItems.Add(cleanItem);
                }
            }
        }
    }
}