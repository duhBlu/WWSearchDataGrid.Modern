using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using WWControls.SampleApp.Grid.Models;
using WWControls.SampleApp.Grid.SampleData;
using WWControls.SampleApp.Grid.SampleData.Generators;

namespace WWControls.SampleApp.Grid.Views.Samples.Columns
{
    public sealed partial class SaveRestoreViewSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<OrderItem> _items = new();

        /// <summary>
        /// The built-in <c>.sdgview</c> files shipped with the sample (embedded resources). Each one
        /// exercises a different save-mode: layout-only, layout with grouping, filters-only, or both.
        /// </summary>
        public IReadOnlyList<ViewPreset> Presets { get; } = new[]
        {
            new ViewPreset(
                "Compact — newest first",
                "Layout",
                "Five key columns with the newest orders on top. Hides product line, state, and the submitted flag. Leaves any filters alone.",
                "Compact.sdgview"),
            new ViewPreset(
                "Grouped by product line & status",
                "Layout",
                "Groups rows by product line, then status, and shows the group panel. Layout only — your filters stay put.",
                "GroupedByProductLine.sdgview"),
            new ViewPreset(
                "High-value active orders",
                "Filters",
                "Filters to orders >= $10,000 whose status is Submitted, In Production, or Shipped. Filters only — your columns don't move.",
                "HighValueActive.sdgview"),
            new ViewPreset(
                "Executive review",
                "Layout + Filters",
                "A focused layout (grouped by status, sorted by total) plus filters for non-cancelled orders >= $5,000 — both at once.",
                "ExecutiveReview.sdgview"),
        };

        public SaveRestoreViewSampleViewModel()
        {
            StartLoad(async () =>
            {
                var data = await Task.Run(() => SampleDataGenerator.Generate(400, OrderGenerator.Create));
                Items = new ObservableCollection<OrderItem>(data);
            });
        }
    }

    /// <summary>Metadata for one built-in preset: what it's called, what it changes, and its file.</summary>
    public sealed record ViewPreset(string Name, string Kind, string Description, string ResourceFile);
}
