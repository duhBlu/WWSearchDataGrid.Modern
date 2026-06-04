using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Generators;
using WWSearchDataGrid.Modern.WPF;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.FilterRow
{
    /// <summary>
    /// Backs the filter-row options playground. Maintains one
    /// <see cref="ColumnPlaygroundConfig"/> per registered grid column plus the choice
    /// collections the sidebar binds to. Grid-level DPs (FilterRowDelay,
    /// FilterClearButtonMode, ShowCriteriaInFilterRow) bind directly to the grid via
    /// ElementName, so the VM doesn't own them.
    /// </summary>
    public sealed partial class OptionsPlaygroundSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<OrderItem> _items = new();

        [ObservableProperty]
        private ColumnPlaygroundConfig? _selectedColumnConfig;

        /// <summary>
        /// Canonical, immutable column list — backs the column-picker combobox and the
        /// RegisterColumn lookup. Stays in display order. Reorder the
        /// <see cref="NavigationOrder"/> collection (not this one) to change Tab traversal.
        /// </summary>
        public ObservableCollection<ColumnPlaygroundConfig> Columns { get; }

        /// <summary>
        /// User-orderable mirror of <see cref="Columns"/>. The drag-reorder listbox in the
        /// sidebar binds to this; dragging an item calls <see cref="ApplyNavigationOrder"/>
        /// which writes each entry's new index into <see cref="ColumnPlaygroundConfig.NavigationIndex"/>.
        /// Reset via <see cref="ResetNavigationOrderCommand"/> — clears every NavigationIndex
        /// back to <c>-1</c> (natural display-order traversal) and restores list ordering.
        /// </summary>
        public ObservableCollection<ColumnPlaygroundConfig> NavigationOrder { get; }

        public IReadOnlyList<FilterClearButtonMode> FilterClearButtonModeChoices { get; } = new[]
        {
            FilterClearButtonMode.Never,
            FilterClearButtonMode.Always,
            FilterClearButtonMode.Display,
            FilterClearButtonMode.Edit,
        };

        public IReadOnlyList<DefaultSearchType> DefaultSearchTypeChoices { get; } = new[]
        {
            DefaultSearchType.Contains,
            DefaultSearchType.StartsWith,
            DefaultSearchType.EndsWith,
            DefaultSearchType.Equals,
        };

        public IReadOnlyList<ShowCriteriaOverrideOption> ShowCriteriaOverrideChoices { get; } = new[]
        {
            new ShowCriteriaOverrideOption("Inherit (null)", null),
            new ShowCriteriaOverrideOption("Show (true)", true),
            new ShowCriteriaOverrideOption("Hide (false)", false),
        };

        public IReadOnlyList<ShowCriteriaOverrideOption> EnableLiveFilteringChoices { get; } = new[]
        {
            new ShowCriteriaOverrideOption("Inherit grid (null)", null),
            new ShowCriteriaOverrideOption("Live (true)", true),
            new ShowCriteriaOverrideOption("Deferred (false)", false),
        };

        public OptionsPlaygroundSampleViewModel()
        {
            Columns = new ObservableCollection<ColumnPlaygroundConfig>
            {
                new("OrderNumber",          "Order #"),
                new("CustomerName",         "Customer"),
                new("OrderStatusName",      "Status"),
                new("OrderDate",            "Order Date"),
                new("OrderItemsTotalPrice", "Total"),
                new("OrderCancelled",       "Cancelled"),
            };
            NavigationOrder = new ObservableCollection<ColumnPlaygroundConfig>(Columns);
            SelectedColumnConfig = Columns[0];

            StartLoad(async () =>
            {
                var data = await Task.Run(() =>
                    SampleDataGenerator.Generate(500, OrderGenerator.Create));
                Items = new ObservableCollection<OrderItem>(data);
            });
        }

        /// <summary>
        /// Handles a drag-reorder from the navigation listbox. Moves the item in
        /// <see cref="NavigationOrder"/> and assigns each entry's <see cref="ColumnPlaygroundConfig.NavigationIndex"/>
        /// to its new position. After the first reorder every column has an explicit index,
        /// so Tab walks the listbox order strictly.
        /// </summary>
        public void ApplyNavigationOrder(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= NavigationOrder.Count) return;
            if (newIndex < 0 || newIndex >= NavigationOrder.Count) return;
            if (oldIndex == newIndex) return;

            NavigationOrder.Move(oldIndex, newIndex);

            for (int i = 0; i < NavigationOrder.Count; i++)
                NavigationOrder[i].NavigationIndex = i;
        }

        /// <summary>
        /// Restores the listbox to the canonical display order and clears every
        /// <see cref="ColumnPlaygroundConfig.NavigationIndex"/> back to <c>-1</c>, so Tab
        /// falls back to native DisplayIndex traversal.
        /// </summary>
        [RelayCommand]
        private void ResetNavigationOrder()
        {
            // Clear indices first so any in-flight Tab handler that runs between the moves
            // never sees a partially-renumbered state.
            foreach (var config in NavigationOrder)
                config.NavigationIndex = -1;

            // Re-sequence NavigationOrder to match the canonical Columns order without
            // throwing away the existing instances (the listbox binding stays attached).
            for (int i = 0; i < Columns.Count; i++)
            {
                int currentIdx = NavigationOrder.IndexOf(Columns[i]);
                if (currentIdx != i && currentIdx >= 0)
                    NavigationOrder.Move(currentIdx, i);
            }
        }

        /// <summary>
        /// Called from the view's code-behind once the grid has materialized its columns. Hands
        /// each live <see cref="GridColumn"/> to the matching config so the sidebar's writes flow
        /// through to the grid.
        /// </summary>
        public void RegisterColumn(string fieldName, GridColumn column)
        {
            foreach (var config in Columns)
            {
                if (config.FieldName == fieldName)
                {
                    config.Attach(column);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Tri-state label/value pair for the <c>ShowCriteriaInFilterRow</c> override combobox.
    /// Using a dedicated POCO so the combobox can bind <c>SelectedValue</c> through to the
    /// nullable bool on the config — WPF's combobox doesn't bind to a raw <c>null</c> entry cleanly.
    /// </summary>
    public sealed class ShowCriteriaOverrideOption
    {
        public ShowCriteriaOverrideOption(string label, bool? value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }
        public bool? Value { get; }

        public override string ToString() => Label;
    }
}
