using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Lookups;
using WWSearchDataGrid.Modern.WPF;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.Editing
{
    /// <summary>
    /// Backs the New Item Row sample — a writable task list whose new-item row position is driven by
    /// the grid's <see cref="NewRowPosition"/> (Top / Bottom / None). <see cref="PrepareNewTask"/>
    /// seeds each freshly-added row with the next sequential Id and a default due date so the row
    /// commits with usable data; the code-behind calls it from the grid's InitializingNewItem event.
    /// </summary>
    public sealed partial class NewItemRowSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<TaskItem> _tasks = new();

        public IReadOnlyList<PriorityOption> Priorities { get; } =
            Array.ConvertAll(TaskLookups.Priorities, p => new PriorityOption(p.Id, p.Name));

        public IReadOnlyList<string> Assignees { get; } = TaskLookups.Assignees;

        public IReadOnlyList<NewRowPosition> NewRowPositionChoices { get; } =
            (NewRowPosition[])Enum.GetValues(typeof(NewRowPosition));

        /// <summary>
        /// Starts on <see cref="NewRowPosition.Top"/> so the non-default capability — a new-item row
        /// above the data — is visible the moment the sample opens.
        /// </summary>
        [ObservableProperty]
        private NewRowPosition _newRowPosition = NewRowPosition.Bottom;

        /// <summary>
        /// Whether <see cref="PrepareNewTask"/> seeds freshly-added rows. Drives the sample's
        /// "Seed new rows" toggle so users can see that auto-Id / default date is application code
        /// (the grid's <c>InitializingNewItem</c> handler), not built-in grid behaviour — switching
        /// it off lets the grid add a raw, unseeded row.
        /// </summary>
        [ObservableProperty]
        private bool _seedNewRows = true;

        public NewItemRowSampleViewModel()
        {
            StartLoad(() =>
            {
                Tasks = new ObservableCollection<TaskItem>
                {
                    new() { Id = 1, Title = "Draft proposal",       AssignedTo = "Alice", PriorityId = 2, DueDate = DateTime.Today.AddDays(3),  HoursEstimate = 4.0,  IsComplete = false },
                    new() { Id = 2, Title = "Review pull request",   AssignedTo = "Bob",   PriorityId = 3, DueDate = DateTime.Today.AddDays(1),  HoursEstimate = 1.5,  IsComplete = false },
                    new() { Id = 3, Title = "Update documentation",  AssignedTo = "Carol", PriorityId = 1, DueDate = DateTime.Today.AddDays(7),  HoursEstimate = 3.0,  IsComplete = true  },
                    new() { Id = 4, Title = "Deploy to staging",     AssignedTo = "Dan",   PriorityId = 4, DueDate = DateTime.Today,             HoursEstimate = 2.0,  IsComplete = false },
                    new() { Id = 5, Title = "QA regression pass",    AssignedTo = "Erin",  PriorityId = 3, DueDate = DateTime.Today.AddDays(5),  HoursEstimate = 6.0,  IsComplete = false },
                };
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Initialises a row the user just started adding through the new-item row: assigns the next
        /// sequential Id and defaults the due date a week out so the row isn't blank on commit. This
        /// runs only because the view wires it to the grid's <c>InitializingNewItem</c> event — the
        /// grid itself just adds a blank item. Honours <see cref="SeedNewRows"/> so the toggle can
        /// demonstrate the unseeded, raw-grid behaviour.
        /// </summary>
        public void PrepareNewTask(TaskItem task)
        {
            if (task == null || !SeedNewRows)
                return;

            task.Id = Tasks.Count == 0 ? 1 : Tasks.Max(t => t.Id) + 1;

            if (task.DueDate == default)
                task.DueDate = DateTime.Today.AddDays(7);
        }
    }
}
