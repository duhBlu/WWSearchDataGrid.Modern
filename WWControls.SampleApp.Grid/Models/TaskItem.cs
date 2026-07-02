using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace WWControls.SampleApp.Grid.Models
{
    public partial class TaskItem : ObservableObject
    {
        [ObservableProperty] private int _id;
        [ObservableProperty] private string _title = string.Empty;
        [ObservableProperty] private string _assignedTo = string.Empty;
        [ObservableProperty] private bool _isComplete;
        [ObservableProperty] private DateTime _dueDate;
        [ObservableProperty] private double _hoursEstimate;
        [ObservableProperty] private int _priorityId;
        [ObservableProperty] private string _contactPhone = string.Empty;
        [ObservableProperty] private string _status = string.Empty;
        [ObservableProperty] private string _department = string.Empty;

        public bool IsOverdue => !IsComplete && DueDate.Date < DateTime.Today;
        public string Initials => string.IsNullOrEmpty(AssignedTo) ? "?" : AssignedTo.Substring(0, 1).ToUpperInvariant();

        partial void OnAssignedToChanged(string value) => OnPropertyChanged(nameof(Initials));
        partial void OnIsCompleteChanged(bool value) => OnPropertyChanged(nameof(IsOverdue));
        partial void OnDueDateChanged(DateTime value) => OnPropertyChanged(nameof(IsOverdue));
    }

    public sealed record PriorityOption(int Id, string Name);
}
