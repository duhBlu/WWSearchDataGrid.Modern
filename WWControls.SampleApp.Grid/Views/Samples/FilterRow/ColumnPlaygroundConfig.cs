using CommunityToolkit.Mvvm.ComponentModel;
using WWControls.Wpf;

namespace WWControls.SampleApp.Grid.Views.Samples.FilterRow
{
    /// <summary>
    /// Runtime-tweakable mirror of one <see cref="GridColumn"/>'s filter-row settings. The
    /// sidebar binds to this; each setter writes the new value through to the backing column so
    /// the grid reflects changes immediately.
    /// </summary>
    public sealed partial class ColumnPlaygroundConfig : ObservableObject
    {
        private GridColumn? _backingColumn;

        public ColumnPlaygroundConfig(string fieldName, string displayLabel)
        {
            FieldName = fieldName;
            DisplayLabel = displayLabel;
        }

        /// <summary>Field name on <see cref="Models.OrderItem"/>.</summary>
        public string FieldName { get; }

        /// <summary>Human-readable label shown in the column-picker combobox.</summary>
        public string DisplayLabel { get; }

        // The picker combobox sets DisplayMemberPath="DisplayLabel", which renders the
        // dropdown items correctly but not the selection box: WPF's SelectionBoxItemTemplate
        // only mirrors an explicit ItemTemplate, not the path-generated one. ContentPresenter
        // then falls back to ToString() on the raw item — without this override the selection
        // box would show the CLR type name. Mirrors ShowCriteriaOverrideOption.ToString().
        public override string ToString() => DisplayLabel;

        [ObservableProperty]
        private bool _allowFiltering = true;

        [ObservableProperty]
        private bool _allowAutoFilter = true;

        /// <summary>
        /// <c>null</c> = inherit from the grid's <see cref="SearchDataGrid.ShowCriteriaInFilterRow"/>.
        /// <c>true</c> / <c>false</c> = override.
        /// </summary>
        [ObservableProperty]
        private bool? _showCriteriaOverride;

        [ObservableProperty]
        private DefaultSearchType _defaultSearchType = DefaultSearchType.StartsWith;

        /// <summary>
        /// <c>null</c> = inherit from the grid's <see cref="SearchDataGrid.EnableLiveFiltering"/>.
        /// <c>true</c> / <c>false</c> = override live-filtering for this column only.
        /// </summary>
        [ObservableProperty]
        private bool? _enableLiveFiltering;

        /// <summary>column-level focus permission.</summary>
        [ObservableProperty]
        private bool _allowFocus = true;

        /// <summary>Tab traversal stop bit.</summary>
        [ObservableProperty]
        private bool _tabStop = true;

        /// <summary>custom Tab order. <c>-1</c> = natural display order.</summary>
        [ObservableProperty]
        private int _navigationIndex = -1;

        /// <summary>
        /// Attach the live <see cref="GridColumn"/> after the view is loaded. Seeds the config's
        /// observable state from the column's current values without re-triggering write-through.
        /// </summary>
        public void Attach(GridColumn column)
        {
            _backingColumn = null;

            AllowFiltering = column.AllowFiltering;
            AllowAutoFilter = column.AllowAutoFilter;
            ShowCriteriaOverride = column.ShowCriteriaInFilterRow;
            DefaultSearchType = column.DefaultSearchType;
            EnableLiveFiltering = column.EnableLiveFiltering;
            AllowFocus = column.AllowFocus;
            TabStop = column.TabStop;
            NavigationIndex = column.NavigationIndex;

            _backingColumn = column;
        }

        partial void OnAllowFilteringChanged(bool value)
        {
            if (_backingColumn != null) _backingColumn.AllowFiltering = value;
        }

        partial void OnAllowAutoFilterChanged(bool value)
        {
            if (_backingColumn != null) _backingColumn.AllowAutoFilter = value;
        }

        partial void OnShowCriteriaOverrideChanged(bool? value)
        {
            if (_backingColumn != null) _backingColumn.ShowCriteriaInFilterRow = value;
        }

        partial void OnDefaultSearchTypeChanged(DefaultSearchType value)
        {
            if (_backingColumn != null) _backingColumn.DefaultSearchType = value;
        }

        partial void OnEnableLiveFilteringChanged(bool? value)
        {
            if (_backingColumn != null) _backingColumn.EnableLiveFiltering = value;
        }

        partial void OnAllowFocusChanged(bool value)
        {
            if (_backingColumn != null) _backingColumn.AllowFocus = value;
        }

        partial void OnTabStopChanged(bool value)
        {
            if (_backingColumn != null) _backingColumn.TabStop = value;
        }

        partial void OnNavigationIndexChanged(int value)
        {
            if (_backingColumn != null) _backingColumn.NavigationIndex = value;
        }
    }
}
