using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWControls.SampleApp.Grid.Models;
using WWControls.SampleApp.Grid.SampleData;
using WWControls.SampleApp.Grid.SampleData.Generators;
using WWControls.SampleApp.Grid.SampleData.Lookups;

namespace WWControls.SampleApp.Grid.Views.Samples.Editing
{
    public sealed partial class EditorCustomizationSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<TaskItem> _tasks = new();

        public IReadOnlyList<string> Statuses { get; } = TaskLookups.TaskStatuses;
        public IReadOnlyList<string> Assignees { get; } = TaskLookups.Assignees;

        public IReadOnlyList<PriorityOption> Priorities { get; } =
            Array.ConvertAll(TaskLookups.Priorities, p => new PriorityOption(p.Id, p.Name));

        public EditorCustomizationSampleViewModel()
        {
            StartLoad(async () =>
            {
                var data = await Task.Run(() =>
                    SampleDataGenerator.Generate(50, TaskGenerator.Create));
                Tasks = new ObservableCollection<TaskItem>(data);
            });
        }
    }
}
