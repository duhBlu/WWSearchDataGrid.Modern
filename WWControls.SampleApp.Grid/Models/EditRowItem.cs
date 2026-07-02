using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;

namespace WWControls.SampleApp.Grid.Models
{
    /// <summary>
    /// Row model for the Edit Entire Row sample. Implements <see cref="IEditableObject"/> so the
    /// grid's row transaction can revert every field as a unit when the user clicks Cancel:
    /// <see cref="BeginEdit"/> snapshots the editable fields, <see cref="CancelEdit"/> restores them
    /// (each setter raises <c>PropertyChanged</c> so the editors and the row update), and
    /// <see cref="EndEdit"/> drops the snapshot on commit.
    /// </summary>
    public partial class EditRowItem : ObservableObject, IEditableObject
    {
        [ObservableProperty] private int _id;
        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private string _department = string.Empty;
        [ObservableProperty] private int _priorityId;
        [ObservableProperty] private double _salary;
        [ObservableProperty] private DateTime _startDate;
        [ObservableProperty] private bool _isActive;

        private Snapshot? _backup;

        public void BeginEdit()
        {
            // WPF can raise BeginEdit more than once before End/Cancel — keep the first snapshot.
            _backup ??= new Snapshot(Name, Department, PriorityId, Salary, StartDate, IsActive);
        }

        public void CancelEdit()
        {
            if (_backup is not Snapshot s) return;
            Name = s.Name;
            Department = s.Department;
            PriorityId = s.PriorityId;
            Salary = s.Salary;
            StartDate = s.StartDate;
            IsActive = s.IsActive;
            _backup = null;
        }

        public void EndEdit() => _backup = null;

        private readonly record struct Snapshot(
            string Name, string Department, int PriorityId, double Salary, DateTime StartDate, bool IsActive);
    }
}
