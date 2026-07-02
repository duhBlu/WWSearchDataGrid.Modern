using System;
using WWControls.SampleApp.Grid.Models;
using WWControls.SampleApp.Grid.SampleData;
using WWControls.SampleApp.Grid.SampleData.Generators;

namespace WWControls.SampleApp.Grid.Views.Samples.AnimationPerformance
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
