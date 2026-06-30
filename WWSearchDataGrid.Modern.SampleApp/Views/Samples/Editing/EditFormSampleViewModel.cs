using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Lookups;
using WWSearchDataGrid.Modern.WPF;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.Editing
{
    /// <summary>
    /// Backs the Inline Edit Form sample — the same employee roster as Edit Entire Row, but edited
    /// through a caption/editor form instead of the column-aligned strip. <see cref="SelectedShowMode"/>
    /// drives <see cref="SearchDataGrid.EditFormShowMode"/> (Inline vs InlineHideRow);
    /// <see cref="SelectedTrigger"/> still gates when the row promotes; <see cref="SelectedConfirmation"/>
    /// drives the focus-leave prompt. <see cref="EditRowItem"/> implements
    /// <see cref="System.ComponentModel.IEditableObject"/> so Cancel reverts the whole row.
    /// </summary>
    public sealed partial class EditFormSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<EditRowItem> _people = new();

        public IReadOnlyList<string> Departments { get; } = TaskLookups.Departments;

        public IReadOnlyList<PriorityOption> Priorities { get; } =
            Array.ConvertAll(TaskLookups.Priorities, p => new PriorityOption(p.Id, p.Name));

        public IReadOnlyList<EditFormShowMode> ShowModeChoices { get; } =
            (EditFormShowMode[])Enum.GetValues(typeof(EditFormShowMode));

        public IReadOnlyList<RowEditTrigger> TriggerChoices { get; } =
            (RowEditTrigger[])Enum.GetValues(typeof(RowEditTrigger));

        public IReadOnlyList<EditFormPostConfirmationMode> ConfirmationChoices { get; } =
            (EditFormPostConfirmationMode[])Enum.GetValues(typeof(EditFormPostConfirmationMode));

        /// <summary>Starts Inline so a cell click opens the form beneath the row.</summary>
        [ObservableProperty]
        private EditFormShowMode _selectedShowMode = EditFormShowMode.Inline;

        /// <summary>Starts OnCellEditorOpen so clicking a cell opens the form immediately.</summary>
        [ObservableProperty]
        private RowEditTrigger _selectedTrigger = RowEditTrigger.OnCellEditorOpen;

        /// <summary>Starts with no focus-leave prompt.</summary>
        [ObservableProperty]
        private EditFormPostConfirmationMode _selectedConfirmation = EditFormPostConfirmationMode.None;

        /// <summary>Last row-edit outcome, shown in the side panel.</summary>
        [ObservableProperty]
        private string _lastAction = "No row edited yet.";

        public EditFormSampleViewModel()
        {
            StartLoad(() =>
            {
                People = new ObservableCollection<EditRowItem>
                {
                    new() { Id = 1, Name = "Alice Nguyen",  Department = "Engineering", PriorityId = 3, Salary = 128000, StartDate = new DateTime(2019, 4, 15), IsActive = true },
                    new() { Id = 2, Name = "Bob Carter",    Department = "Product",     PriorityId = 2, Salary = 96000,  StartDate = new DateTime(2021, 9, 1),  IsActive = true },
                    new() { Id = 3, Name = "Carol Diaz",    Department = "Design",      PriorityId = 2, Salary = 89000,  StartDate = new DateTime(2020, 1, 20), IsActive = true },
                    new() { Id = 4, Name = "Dan Edwards",   Department = "QA",          PriorityId = 1, Salary = 78000,  StartDate = new DateTime(2022, 6, 6),  IsActive = false },
                    new() { Id = 5, Name = "Erin Foley",    Department = "DevOps",      PriorityId = 4, Salary = 134000, StartDate = new DateTime(2018, 11, 12),IsActive = true },
                    new() { Id = 6, Name = "Frank Gomez",   Department = "Support",     PriorityId = 2, Salary = 71000,  StartDate = new DateTime(2023, 2, 27), IsActive = true },
                    new() { Id = 7, Name = "Grace Hughes",  Department = "Engineering", PriorityId = 3, Salary = 119000, StartDate = new DateTime(2020, 8, 3),  IsActive = true },
                    new() { Id = 8, Name = "Henry Ito",     Department = "Product",     PriorityId = 1, Salary = 84000,  StartDate = new DateTime(2021, 3, 18), IsActive = false },
                };
                return Task.CompletedTask;
            });
        }

        /// <summary>Records a committed row edit for the status panel.</summary>
        public void NoteCommitted(EditRowItem? item) =>
            LastAction = item == null ? "Row updated." : $"Updated ✓  {item.Name} (#{item.Id}).";

        /// <summary>Records a cancelled row edit for the status panel.</summary>
        public void NoteCancelled(EditRowItem? item) =>
            LastAction = item == null ? "Edit cancelled." : $"Cancelled ✕  {item.Name} (#{item.Id}) — reverted.";
    }
}
