using CommunityToolkit.Mvvm.ComponentModel;
using WWSearchDataGrid.Modern.WPF;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.AutoFilterRow
{
    /// <summary>
    /// Runtime-tweakable mirror of one <see cref="GridColumn"/>'s auto-filter-row settings. The
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
        /// <c>null</c> = inherit from the grid's <see cref="SearchDataGrid.ShowCriteriaInAutoFilterRow"/>.
        /// <c>true</c> / <c>false</c> = override.
        /// </summary>
        [ObservableProperty]
        private bool? _showCriteriaOverride;

        [ObservableProperty]
        private bool _immediateUpdateAutoFilter = true;

        [ObservableProperty]
        private DefaultSearchType _defaultSearchType = DefaultSearchType.StartsWith;

        /// <summary>
        /// Attach the live <see cref="GridColumn"/> after the view is loaded. Seeds the config's
        /// observable state from the column's current values without re-triggering write-through.
        /// </summary>
        public void Attach(GridColumn column)
        {
            _backingColumn = null;

            AllowFiltering = column.AllowFiltering;
            AllowAutoFilter = column.AllowAutoFilter;
            ShowCriteriaOverride = column.ShowCriteriaInAutoFilterRow;
            ImmediateUpdateAutoFilter = column.ImmediateUpdateAutoFilter;
            DefaultSearchType = column.DefaultSearchType;

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
            if (_backingColumn != null) _backingColumn.ShowCriteriaInAutoFilterRow = value;
        }

        partial void OnImmediateUpdateAutoFilterChanged(bool value)
        {
            if (_backingColumn != null) _backingColumn.ImmediateUpdateAutoFilter = value;
        }

        partial void OnDefaultSearchTypeChanged(DefaultSearchType value)
        {
            if (_backingColumn != null) _backingColumn.DefaultSearchType = value;
        }
    }
}
