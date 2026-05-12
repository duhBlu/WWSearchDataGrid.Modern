using System;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Generators;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.AnimationPerformance
{
    public sealed class LargeDatasetsSampleViewModel : GeneratableSampleViewModel<OrderItem>
    {
        public LargeDatasetsSampleViewModel()
        {
            RowCount = 10000;
        }

        protected override OrderItem CreateItem(Random rnd, int index) => OrderGenerator.Create(rnd, index);
    }
}
