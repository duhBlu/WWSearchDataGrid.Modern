using System;
using WWControls.SampleApp.Grid.Models;
using WWControls.SampleApp.Grid.SampleData.Lookups;

namespace WWControls.SampleApp.Grid.SampleData.Generators
{
    public static class TaskGenerator
    {
        public static TaskItem Create(Random rnd, int index)
        {
            var dueOffsetDays = rnd.Next(-7, 60);
            var hours = Math.Round(0.5 + rnd.NextDouble() * 39.5, 1) * 0.5; // half-hour increments
            var priority = TaskLookups.Priorities[rnd.Next(TaskLookups.Priorities.Length)];

            return new TaskItem
            {
                Id = 1 + index,
                Title = TaskLookups.TaskTitles[rnd.Next(TaskLookups.TaskTitles.Length)],
                AssignedTo = TaskLookups.Assignees[rnd.Next(TaskLookups.Assignees.Length)],
                IsComplete = rnd.NextDouble() < 0.35,
                DueDate = DateTime.Today.AddDays(dueOffsetDays),
                HoursEstimate = hours,
                PriorityId = priority.Id,
                ContactPhone = $"555{rnd.Next(1000000, 9999999)}",
                Status = TaskLookups.TaskStatuses[rnd.Next(TaskLookups.TaskStatuses.Length)],
                Department = TaskLookups.Departments[rnd.Next(TaskLookups.Departments.Length)]
            };
        }
    }
}
