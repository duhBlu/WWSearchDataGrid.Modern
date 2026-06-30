using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf
{
    /// <summary>
    /// Compact picker for the active <see cref="SearchType"/> on a column. Shows the icon of
    /// the selected type and opens a popup of valid types filtered by <see cref="ColumnDataType"/>.
    /// </summary>
    public class SearchTypeSelector : Control
    {
        static SearchTypeSelector()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SearchTypeSelector),
                new FrameworkPropertyMetadata(typeof(SearchTypeSelector)));
        }

        #region Dependency Properties

        public static readonly DependencyProperty SelectedSearchTypeProperty =
            DependencyProperty.Register(nameof(SelectedSearchType), typeof(SearchType),
                typeof(SearchTypeSelector),
                new FrameworkPropertyMetadata(SearchType.Contains,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty ColumnDataTypeProperty =
            DependencyProperty.Register(nameof(ColumnDataType), typeof(ColumnDataType),
                typeof(SearchTypeSelector),
                new FrameworkPropertyMetadata(ColumnDataType.Unknown, OnFilteringInputChanged));

        public static readonly DependencyProperty IsNullableProperty =
            DependencyProperty.Register(nameof(IsNullable), typeof(bool),
                typeof(SearchTypeSelector),
                new FrameworkPropertyMetadata(true, OnFilteringInputChanged));

        private static readonly DependencyPropertyKey AvailableSearchTypesPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(AvailableSearchTypes), typeof(IEnumerable<SearchTypeOption>),
                typeof(SearchTypeSelector),
                new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty AvailableSearchTypesProperty =
            AvailableSearchTypesPropertyKey.DependencyProperty;

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool),
                typeof(SearchTypeSelector),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Explicit operator whitelist — null falls back to the data-type registry lookup.
        /// </summary>
        public static readonly DependencyProperty SupportedSearchTypesProperty =
            DependencyProperty.Register(nameof(SupportedSearchTypes), typeof(IEnumerable<SearchType>),
                typeof(SearchTypeSelector),
                new FrameworkPropertyMetadata(null, OnFilteringInputChanged));

        #endregion

        #region CLR Properties

        /// <summary>The currently selected search type. Two-way by default.</summary>
        public SearchType SelectedSearchType
        {
            get => (SearchType)GetValue(SelectedSearchTypeProperty);
            set => SetValue(SelectedSearchTypeProperty, value);
        }

        /// <summary>Column data type, used to filter <see cref="AvailableSearchTypes"/>.</summary>
        public ColumnDataType ColumnDataType
        {
            get => (ColumnDataType)GetValue(ColumnDataTypeProperty);
            set => SetValue(ColumnDataTypeProperty, value);
        }

        /// <summary>Whether the bound property allows null. Drives whether <c>IsNull</c>/<c>IsNotNull</c> appear in the dropdown.</summary>
        public bool IsNullable
        {
            get => (bool)GetValue(IsNullableProperty);
            set => SetValue(IsNullableProperty, value);
        }

        /// <summary>The set of search types currently selectable for this column's data type.</summary>
        public IEnumerable<SearchTypeOption> AvailableSearchTypes
        {
            get => (IEnumerable<SearchTypeOption>)GetValue(AvailableSearchTypesProperty);
            private set => SetValue(AvailableSearchTypesPropertyKey, value);
        }

        /// <summary>Whether the popup is open.</summary>
        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        /// <inheritdoc cref="SupportedSearchTypesProperty"/>
        public IEnumerable<SearchType> SupportedSearchTypes
        {
            get => (IEnumerable<SearchType>)GetValue(SupportedSearchTypesProperty);
            set => SetValue(SupportedSearchTypesProperty, value);
        }

        #endregion

        /// <summary>
        /// Raised on item click — fires even when the click doesn't change selection, so
        /// re-clicking the active entry still counts as a user commit (which SelectionChanged misses).
        /// </summary>
        public event EventHandler ItemChosen;

        private ICommand _selectSearchTypeCommand;

        /// <summary>
        /// Invoked by each <see cref="SearchTypeOption"/> MenuItem in the dropdown. Sets
        /// <see cref="SelectedSearchType"/> and fires <see cref="ItemChosen"/> on every click —
        /// including re-clicks of the active option, which a plain SelectedValue binding
        /// would silently drop. The ContextMenu's auto-close handles dismissal.
        /// </summary>
        public ICommand SelectSearchTypeCommand => _selectSearchTypeCommand ??= new RelayCommand<SearchTypeOption>(option =>
        {
            if (option == null) return;
            SelectedSearchType = option.SearchType;
            ItemChosen?.Invoke(this, EventArgs.Empty);
        });

        public SearchTypeSelector()
        {
            RebuildAvailableSearchTypes();
        }

        private static void OnFilteringInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((SearchTypeSelector)d).RebuildAvailableSearchTypes();

        private void RebuildAvailableSearchTypes()
        {
            // Explicit whitelist wins over data-type registry — EditSettings knows the editor's
            // value shape; the registry is the fallback when no whitelist is supplied.
            IEnumerable<SearchTypeMetadata> metadata;
            var supported = SupportedSearchTypes;
            if (supported != null)
            {
                metadata = supported
                    .Select(t => SearchTypeRegistry.GetMetadata(t))
                    .Where(m => m != null);
            }
            else
            {
                metadata = ColumnDataType == ColumnDataType.Unknown
                    ? SearchTypeRegistry.GetFiltersForDataType(ColumnDataType.String, IsNullable)
                    : SearchTypeRegistry.GetFiltersForDataType(ColumnDataType, IsNullable);
            }

            AvailableSearchTypes = metadata
                .OrderBy(m => GroupOrdinal(m.SearchType))
                .ThenBy(m => m.DisplayName)
                .Select(m => new SearchTypeOption(m.SearchType, m.DisplayName))
                .ToList()
                .AsReadOnly();

            if (!AvailableSearchTypes.Any(o => o.SearchType == SelectedSearchType))
            {
                var fallback = AvailableSearchTypes.FirstOrDefault();
                if (fallback != null)
                    SelectedSearchType = fallback.SearchType;
            }
        }

        private static int GroupOrdinal(SearchType type) => type switch
        {
            SearchType.Contains or SearchType.DoesNotContain or SearchType.StartsWith
                or SearchType.EndsWith or SearchType.IsLike or SearchType.IsNotLike => 0,
            SearchType.Equals or SearchType.NotEquals or SearchType.GreaterThan
                or SearchType.GreaterThanOrEqualTo or SearchType.LessThan or SearchType.LessThanOrEqualTo
                or SearchType.Between or SearchType.NotBetween => 1,
            SearchType.BetweenDates or SearchType.NotBetweenDates
                or SearchType.Today or SearchType.Yesterday or SearchType.DateInterval => 2,
            SearchType.IsAnyOf or SearchType.IsNoneOf => 3,
            SearchType.IsNull or SearchType.IsNotNull => 4,
            SearchType.TopN or SearchType.BottomN
                or SearchType.AboveAverage or SearchType.BelowAverage => 5,
            SearchType.Unique or SearchType.Duplicate => 6,
            _ => 99
        };
    }
}
