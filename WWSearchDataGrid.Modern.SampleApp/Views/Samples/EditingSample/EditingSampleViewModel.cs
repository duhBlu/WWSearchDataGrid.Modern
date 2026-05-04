using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.EditingSample
{
    public partial class EditingSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<TaskItem> _tasks = BuildTasks();

        public IReadOnlyList<PriorityOption> Priorities { get; } = new[]
        {
            new PriorityOption(1, "Low"),
            new PriorityOption(2, "Normal"),
            new PriorityOption(3, "High"),
            new PriorityOption(4, "Urgent"),
        };

        public IReadOnlyList<string> Assignees { get; } = new[]
        {
            "Alice", "Bob", "Carol", "Dan", "Erin", "Frank", "Grace", "Henry"
        };

        public IReadOnlyList<string> Statuses { get; } = new[]
        {
            "Backlog", "In Progress", "In Review", "Blocked", "Done"
        };

        public IReadOnlyList<string> Departments { get; } = new[]
        {
            "Engineering", "Product", "Design", "QA", "DevOps", "Support"
        };

        private static ObservableCollection<TaskItem> BuildTasks() => new()
        {
            new TaskItem { Id = 1, Title = "Draft proposal",       AssignedTo = "Alice",  IsComplete = false, DueDate = new DateTime(2026, 5,  3), HoursEstimate = 8,  PriorityId = 2, ContactPhone = "5551234567", Status = "In Progress", Department = "Engineering" },
            new TaskItem { Id = 2, Title = "Review pull request",  AssignedTo = "Bob",    IsComplete = true,  DueDate = new DateTime(2026, 4, 28), HoursEstimate = 2,  PriorityId = 1, ContactPhone = "5552345678", Status = "Done",        Department = "Engineering" },
            new TaskItem { Id = 3, Title = "Schedule kickoff",     AssignedTo = "Carol",  IsComplete = false, DueDate = new DateTime(2026, 5,  6), HoursEstimate = 1,  PriorityId = 3, ContactPhone = "5553456789", Status = "Backlog",     Department = "Product" },
            new TaskItem { Id = 4, Title = "Update documentation", AssignedTo = "Dan",    IsComplete = false, DueDate = new DateTime(2026, 5,  9), HoursEstimate = 4,  PriorityId = 2, ContactPhone = "5554567890", Status = "In Review",   Department = "Engineering" },
            new TaskItem { Id = 5, Title = "Deploy to staging",    AssignedTo = "Erin",   IsComplete = true,  DueDate = new DateTime(2026, 4, 30), HoursEstimate = 3,  PriorityId = 4, ContactPhone = "5555678901", Status = "Done",        Department = "DevOps" },
            new TaskItem { Id = 6, Title = "QA regression pass",   AssignedTo = "Frank",  IsComplete = false, DueDate = new DateTime(2026, 5, 12), HoursEstimate = 16, PriorityId = 3, ContactPhone = "5556789012", Status = "Blocked",     Department = "QA" },
        };
    }

    public partial class TaskItem : ObservableObject
    {
        [ObservableProperty] private int _id;
        [ObservableProperty] private string _title;
        [ObservableProperty] private string _assignedTo;
        [ObservableProperty] private bool _isComplete;
        [ObservableProperty] private DateTime _dueDate;
        [ObservableProperty] private double _hoursEstimate;
        [ObservableProperty] private int _priorityId;
        [ObservableProperty] private string _contactPhone;
        [ObservableProperty] private string _status;
        [ObservableProperty] private string _department;
    }

    public sealed record PriorityOption(int Id, string Name);
}
