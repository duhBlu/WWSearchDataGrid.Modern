using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWSearchDataGrid.Modern.SampleApp.Models;
using WWSearchDataGrid.Modern.SampleApp.SampleData;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Generators;
using WWSearchDataGrid.Modern.SampleApp.SampleData.Lookups;
using WWSearchDataGrid.Modern.WPF;

namespace WWSearchDataGrid.Modern.SampleApp.Views.Samples.Editing
{
    /// <summary>
    /// Three options exercised by the sample's IsComplete column — swaps the
    /// <c>EditSettings</c> instance bound to that column at runtime so users see how each editor
    /// renders and parses a boolean field.
    /// </summary>
    public enum BooleanEditorKind
    {
        CheckEdit,
        TextEdit,
        ComboBoxEdit,
    }

    public sealed partial class EditorTypesSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<TaskItem> _tasks = new();

        public IReadOnlyList<PriorityOption> Priorities { get; } =
            Array.ConvertAll(TaskLookups.Priorities, p => new PriorityOption(p.Id, p.Name));

        public IReadOnlyList<string> Assignees { get; } = TaskLookups.Assignees;

        /// <summary>
        /// Hard-coded {True, False} options used by the BooleanEditor=ComboBoxEdit case so the
        /// dropdown populates without needing a separate data source.
        /// </summary>
        public IReadOnlyList<bool> BooleanOptions { get; } = new[] { true, false };

        public IReadOnlyList<BooleanEditorKind> BooleanEditorChoices { get; } =
            (BooleanEditorKind[])Enum.GetValues(typeof(BooleanEditorKind));

        public IReadOnlyList<EditorShowMode> EditorShowModeChoices { get; } =
            (EditorShowMode[])Enum.GetValues(typeof(EditorShowMode));

        public IReadOnlyList<EditorButtonShowMode> EditorButtonShowModeChoices { get; } =
            (EditorButtonShowMode[])Enum.GetValues(typeof(EditorButtonShowMode));

        public IReadOnlyList<SelectAllScope> SelectAllScopeChoices { get; } =
            (SelectAllScope[])Enum.GetValues(typeof(SelectAllScope));

        [ObservableProperty]
        private BooleanEditorKind _booleanEditor = BooleanEditorKind.CheckEdit;

        [ObservableProperty]
        private EditorShowMode _editorShowMode = EditorShowMode.MouseDown;

        [ObservableProperty]
        private EditorButtonShowMode _editorButtonShowMode = EditorButtonShowMode.ShowOnlyInEditor;

        [ObservableProperty]
        private SelectAllScope _selectAllScope = SelectAllScope.FilteredRows;

        public EditorTypesSampleViewModel()
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
