using System;
using WWControls.SampleApp.Models;
using WWControls.SampleApp.SampleData;
using WWControls.SampleApp.SampleData.Generators;

namespace WWControls.SampleApp.Views.Samples.AnimationPerformance
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
