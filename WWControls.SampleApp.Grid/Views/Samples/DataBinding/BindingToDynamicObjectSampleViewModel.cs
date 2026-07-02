using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using WWControls.SampleApp.Grid.SampleData;
using WWControls.SampleApp.Grid.SampleData.Lookups;

namespace WWControls.SampleApp.Grid.Views.Samples.DataBinding
{
    /// <summary>The kind of column a "Add Column" click creates.</summary>
    public enum DynamicColumnType
    {
        Integer,
        String,
        DateTime,
        Boolean,

        /// <summary>A string column whose editor is a ComboBox bound to a fixed choice list.</summary>
        ComboBox,
    }

    /// <summary>
    /// Describes a column the view-model just registered: the binding path, and — for a
    /// <see cref="DynamicColumnType.ComboBox"/> column — the choice list the code-behind wires into
    /// a <c>ComboBoxSettings.ItemsSource</c>. Keeps the WPF EditSettings construction in the
    /// view (which owns the grid) while the data shape stays here.
    /// </summary>
    public sealed record NewColumnSpec(string FieldName, IReadOnlyList<string>? ComboChoices);

    /// <summary>
    /// Backs the "Binding to Dynamic Object" sample. The grid is bound to an
    /// <see cref="ObservableCollection{T}"/> of <see cref="ExpandoObject"/> — a bag with no CLR
    /// properties — so every column reaches its value through an explicit
    /// <c>GridColumn.Binding</c> (e.g. <c>Binding="{Binding Id, Mode=TwoWay}"</c>) rather than a
    /// reflected <c>FieldName</c>. The three seed columns are declared in XAML; this view-model
    /// owns the rows and the field set, generates random values per the selected
    /// <see cref="DynamicColumnType"/>, and supports adding rows. Adding columns is driven from the
    /// code-behind (it needs the grid instance) via <see cref="AddDynamicColumn"/>.
    /// </summary>
    public sealed partial class BindingToDynamicObjectSampleViewModel : SampleViewModelBase
    {
        // A field present on every row: its name (the binding path) and a factory for fresh values
        // so newly added rows and back-filled columns stay populated.
        private sealed record DynamicField(string Name, Func<object> NewValue);

        // Choice list for ComboBox columns — also the value pool those columns are seeded from, so
        // the dropdown's items always cover the data already in the grid.
        private static readonly string[] StatusChoices = { "Backlog", "In Progress", "Review", "Blocked", "Done" };

        private readonly List<DynamicField> _fields;
        private readonly Random _rnd = new();
        private int _nextId;

        [ObservableProperty]
        private ObservableCollection<ExpandoObject> _rows = new();

        [ObservableProperty]
        private DynamicColumnType _selectedColumnType = DynamicColumnType.Integer;

        public BindingToDynamicObjectSampleViewModel()
        {
            // Seed fields match the three GridColumns declared in XAML. Id is sequential; names are
            // drawn from the shared lookup tables.
            _fields = new List<DynamicField>
            {
                new("Id", () => _nextId++),
                new("FirstName", RandomFirstName),
                new("LastName", RandomLastName),
            };

            for (int i = 0; i < 50; i++)
                Rows.Add(NewRow());
        }

        /// <summary>Appends a row populated with a value for every current field.</summary>
        [RelayCommand]
        private void AddRow() => Rows.Add(NewRow());

        /// <summary>
        /// Registers a new field for the currently selected <see cref="DynamicColumnType"/>,
        /// back-fills every existing row with a random value of that type, and returns a
        /// <see cref="NewColumnSpec"/>. The caller (code-behind) creates the matching
        /// <c>GridColumn</c> with a <c>Binding</c> to this field — and, for a ComboBox column, a
        /// <c>ComboBoxSettings</c> over the returned choices — then adds it to the grid.
        /// </summary>
        public NewColumnSpec AddDynamicColumn()
        {
            IReadOnlyList<string>? comboChoices = null;
            Func<object> generator;
            switch (SelectedColumnType)
            {
                case DynamicColumnType.Integer:
                    generator = () => _rnd.Next(0, 1000);
                    break;
                case DynamicColumnType.String:
                    generator = RandomWord;
                    break;
                case DynamicColumnType.DateTime:
                    generator = () => DateTime.Today.AddDays(-_rnd.Next(0, 365));
                    break;
                case DynamicColumnType.Boolean:
                    generator = () => _rnd.Next(2) == 0;
                    break;
                case DynamicColumnType.ComboBox:
                    comboChoices = StatusChoices;
                    generator = () => StatusChoices[_rnd.Next(StatusChoices.Length)];
                    break;
                default:
                    throw new NotSupportedException();
            }

            string name = UniqueFieldName(SelectedColumnType + " Column");
            _fields.Add(new DynamicField(name, generator));

            foreach (var row in Rows)
                ((IDictionary<string, object>)(object)row)[name] = generator();

            return new NewColumnSpec(name, comboChoices);
        }

        private ExpandoObject NewRow()
        {
            var row = new ExpandoObject();
            var bag = (IDictionary<string, object>)(object)row;
            foreach (var field in _fields)
                bag[field.Name] = field.NewValue();
            return row;
        }

        private string UniqueFieldName(string baseName)
        {
            int suffix = 1;
            string candidate = $"{baseName} {suffix}";
            while (_fields.Exists(f => f.Name == candidate))
                candidate = $"{baseName} {++suffix}";
            return candidate;
        }

        private object RandomFirstName() => Names.FirstNames[_rnd.Next(Names.FirstNames.Length)];

        private object RandomLastName() => Names.LastNames[_rnd.Next(Names.LastNames.Length)];

        private object RandomWord() => Names.LastNames[_rnd.Next(Names.LastNames.Length)];
    }
}
