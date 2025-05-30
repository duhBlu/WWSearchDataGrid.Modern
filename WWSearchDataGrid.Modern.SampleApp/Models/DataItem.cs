using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WWSearchDataGrid.Modern.SampleApp.Models
{
    /// <summary>
    /// Model representing various data types for testing DataGrid filtering
    /// </summary>
    public class DataItem : ObservableObject
    {
        private bool _boolValue;
        private bool? _nullableBoolValue;
        private DateTime _dateTimeValue;
        private DateTime? _nullableDateTimeValue;
        private decimal _decimalValue;
        private decimal? _nullableDecimalValue;
        private double _doubleValue;
        private double? _nullableDoubleValue;
        private float _floatValue;
        private float? _nullableFloatValue;
        private int _intValue;
        private int? _nullableIntValue;
        private long _longValue;
        private string _stringValue;
        private int _comboBoxValueId;
        private string _selectedComboBoxStringValue;

        /// <summary>
        /// Gets or sets the boolean value
        /// </summary>
        public bool BoolValue
        {
            get => _boolValue;
            set => SetProperty(ref _boolValue, value);
        }

        /// <summary>
        /// Gets or sets the nullable boolean value
        /// </summary>
        public bool? NullableBoolValue
        {
            get => _nullableBoolValue;
            set => SetProperty(ref _nullableBoolValue, value);
        }

        /// <summary>
        /// Gets or sets the DateTime value
        /// </summary>
        public DateTime DateTimeValue
        {
            get => _dateTimeValue;
            set => SetProperty(ref _dateTimeValue, value);
        }

        /// <summary>
        /// Gets or sets the nullable DateTime value
        /// </summary>
        public DateTime? NullableDateTimeValue
        {
            get => _nullableDateTimeValue;
            set => SetProperty(ref _nullableDateTimeValue, value);
        }

        /// <summary>
        /// Gets or sets the decimal value
        /// </summary>
        public decimal DecimalValue
        {
            get => _decimalValue;
            set => SetProperty(ref _decimalValue, value);
        }

        /// <summary>
        /// Gets or sets the nullable decimal value
        /// </summary>
        public decimal? NullableDecimalValue
        {
            get => _nullableDecimalValue;
            set => SetProperty(ref _nullableDecimalValue, value);
        }

        /// <summary>
        /// Gets or sets the double value
        /// </summary>
        public double DoubleValue
        {
            get => _doubleValue;
            set => SetProperty(ref _doubleValue, value);
        }

        /// <summary>
        /// Gets or sets the nullable double value
        /// </summary>
        public double? NullableDoubleValue
        {
            get => _nullableDoubleValue;
            set => SetProperty(ref _nullableDoubleValue, value);
        }

        /// <summary>
        /// Gets or sets the float value
        /// </summary>
        public float FloatValue
        {
            get => _floatValue;
            set => SetProperty(ref _floatValue, value);
        }

        /// <summary>
        /// Gets or sets the nullable float value
        /// </summary>
        public float? NullableFloatValue
        {
            get => _nullableFloatValue;
            set => SetProperty(ref _nullableFloatValue, value);
        }

        /// <summary>
        /// Gets or sets the integer value
        /// </summary>
        public int IntValue
        {
            get => _intValue;
            set => SetProperty(ref _intValue, value);
        }

        /// <summary>
        /// Gets or sets the nullable integer value
        /// </summary>
        public int? NullableIntValue
        {
            get => _nullableIntValue;
            set => SetProperty(ref _nullableIntValue, value);
        }

        /// <summary>
        /// Gets or sets the long value
        /// </summary>
        public long LongValue
        {
            get => _longValue;
            set => SetProperty(ref _longValue, value);
        }

        /// <summary>
        /// Gets or sets the string value
        /// </summary>
        public string StringValue
        {
            get => _stringValue;
            set => SetProperty(ref _stringValue, value);
        }

        /// <summary>
        /// Gets or sets the ComboBox value ID
        /// </summary>
        public int ComboBoxValueId
        {
            get => _comboBoxValueId;
            set => SetProperty(ref _comboBoxValueId, value);
        }

        /// <summary>
        /// Gets or sets the selected ComboBox string value
        /// </summary>
        public string SelectedComboBoxStringValue
        {
            get => _selectedComboBoxStringValue;
            set => SetProperty(ref _selectedComboBoxStringValue, value);
        }

        /// <summary>
        /// Gets or sets the collection of property tuples for complex filtering
        /// </summary>
        public List<Tuple<string, string>> PropertyValues { get; set; } = new();

        /// <summary>
        /// Gets or sets the dictionary of property values for dictionary-based filtering
        /// </summary>
        public Dictionary<string, object> PropertyDictionary { get; set; } = new();
    }
}
