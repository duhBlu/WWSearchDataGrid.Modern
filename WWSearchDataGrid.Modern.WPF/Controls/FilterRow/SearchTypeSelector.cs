using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Compact picker for the active <see cref="SearchType"/> on a column. Displays the
    /// icon of the currently selected type and opens a popup of valid types (filtered by
    /// <see cref="ColumnDataType"/>) on click.
    /// </summary>
    /// <remarks>
    /// Phase 5: this selector is the only path to choosing a search type — the legacy
    /// prefix-token shortcuts (<c>&gt;</c>, <c>!=</c>, <c>s#</c>, …) were removed alongside
    /// the legacy <c>ColumnSearchBox</c>. Selection here drives
    /// <see cref="ColumnFilterControl.SelectedSearchType"/> directly.
    /// </remarks>
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
        /// Explicit whitelist of <see cref="SearchType"/>s to surface in the dropdown — used
        /// when the host (typically <see cref="ColumnFilterControl"/>) wants per-editor-shape
        /// scoping instead of the broader data-type lookup. <c>null</c> falls back to
        /// <see cref="SearchTypeRegistry.GetFiltersForDataType(ColumnDataType, bool)"/>.
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

        public const string PartListName = "PART_List";

        private ListBox _listBox;

        /// <summary>
        /// Raised after the user clicks an item in the popup ListBox. Fires regardless of
        /// whether the click actually changed <see cref="SelectedSearchType"/> — clicking the
        /// already-selected entry should still be treated as the user committing to that
        /// search type, but <see cref="Selector.SelectionChanged"/> stays silent in that case.
        /// Hosts use this signal to close the popup and route keyboard focus to their
        /// downstream editor.
        /// </summary>
        public event EventHandler ItemChosen;

        public SearchTypeSelector()
        {
            RebuildAvailableSearchTypes();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_listBox != null)
            {
                _listBox.PreviewMouseLeftButtonUp -= OnListBoxPreviewMouseLeftButtonUp;
                _listBox = null;
            }

            _listBox = GetTemplateChild(PartListName) as ListBox;
            if (_listBox != null)
            {
                _listBox.PreviewMouseLeftButtonUp += OnListBoxPreviewMouseLeftButtonUp;
            }
        }

        private void OnListBoxPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_listBox == null) return;
            // Only treat the up event as a selection gesture when the user actually clicked
            // an item container — clicks inside the ListBox's padding / scrollbar shouldn't
            // count as a choice. ContainerFromElement walks up the visual tree from the click
            // target until it finds the row container (or returns null).
            var container = ItemsControl.ContainerFromElement(_listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (container == null) return;
            ItemChosen?.Invoke(this, EventArgs.Empty);
        }

        private static void OnFilteringInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((SearchTypeSelector)d).RebuildAvailableSearchTypes();

        private void RebuildAvailableSearchTypes()
        {
            // Per-editor-shape whitelist takes precedence — the column's EditSettings is the
            // authoritative source for "which operators apply to this editor's value shape".
            // The data-type registry remains the fallback for callers (legacy or test
            // harnesses) that surface the selector without an EditSettings context.
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
