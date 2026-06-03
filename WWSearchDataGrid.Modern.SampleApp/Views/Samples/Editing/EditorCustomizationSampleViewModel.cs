using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Generators;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Lookups;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.Editing
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
