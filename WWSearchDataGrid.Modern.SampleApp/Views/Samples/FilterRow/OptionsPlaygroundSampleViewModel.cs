using CommunityToolkit.Mvvm.ComponentModel;
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

        public ObservableCollection<ColumnPlaygroundConfig> Columns { get; }

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
            SelectedColumnConfig = Columns[0];

            StartLoad(async () =>
            {
                var data = await Task.Run(() =>
                    SampleDataGenerator.Generate(500, OrderGenerator.Create));
                Items = new ObservableCollection<OrderItem>(data);
            });
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
