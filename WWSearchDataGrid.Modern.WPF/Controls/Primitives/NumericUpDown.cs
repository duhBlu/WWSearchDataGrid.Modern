using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Simple NumericUpDown control for numeric input
    /// </summary>
    public class NumericUpDown : Control
    {
        #region Dependency Properties

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(NumericUpDown),
                new PropertyMetadata(0, OnValueChanged, CoerceValue));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(int), typeof(NumericUpDown),
                new PropertyMetadata(int.MinValue));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int), typeof(NumericUpDown),
                new PropertyMetadata(int.MaxValue));

        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register("Increment", typeof(int), typeof(NumericUpDown),
                new PropertyMetadata(1));

        #endregion

        #region Properties

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Gets the value as an integer for internal calculations
        /// </summary>
        private int IntValue
        {
            get
            {
                if (Value == null)
                    return 0;
                
                if (int.TryParse(Value.ToString(), out int result))
                    return Math.Max(Minimum, Math.Min(Maximum, result));
                
                return 0;
            }
        }

        public int Minimum
        {
            get => (int)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public int Maximum
        {
            get => (int)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public int Increment
        {
            get => (int)GetValue(IncrementProperty);
            set => SetValue(IncrementProperty, value);
        }

        #endregion

        #region Commands

        public ICommand IncreaseCommand { get; }
        public ICommand DecreaseCommand { get; }

        #endregion

        #region Constructor

        static NumericUpDown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDown),
                new FrameworkPropertyMetadata(typeof(NumericUpDown)));
        }

        public NumericUpDown()
        {
            IncreaseCommand = new RelayCommand(_ => IncreaseValue());
            DecreaseCommand = new RelayCommand(_ => DecreaseValue());
        }

        #endregion

        #region Methods

        private void IncreaseValue()
        {
            var newValue = IntValue + Increment;
            if (newValue <= Maximum)
            {
                Value = newValue;
            }
        }

        private void DecreaseValue()
        {
            var newValue = IntValue - Increment;
            if (newValue >= Minimum)
            {
                Value = newValue;
            }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NumericUpDown control)
            {
                // The value will be coerced to be within bounds by the CoerceValue method
                control.OnPropertyChanged(nameof(control.IntValue));
            }
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            if (d is NumericUpDown control)
            {
                if (baseValue == null)
                    return 0;

                if (int.TryParse(baseValue.ToString(), out int intValue))
                {
                    // Coerce to be within bounds
                    return Math.Max(control.Minimum, Math.Min(control.Maximum, intValue));
                }

                return 0;
            }

            return baseValue;
        }

        private void OnPropertyChanged(string propertyName)
        {
            // This would require implementing INotifyPropertyChanged if needed
            // For now, we'll rely on the dependency property system
        }

        #endregion
    }
}
