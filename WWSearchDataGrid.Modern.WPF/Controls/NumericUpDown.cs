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
            DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDown),
                new PropertyMetadata(0, OnValueChanged));

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

        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
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
            var newValue = Value + Increment;
            if (newValue <= Maximum)
            {
                Value = newValue;
            }
        }

        private void DecreaseValue()
        {
            var newValue = Value - Increment;
            if (newValue >= Minimum)
            {
                Value = newValue;
            }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NumericUpDown control)
            {
                var newValue = (int)e.NewValue;
                if (newValue < control.Minimum)
                {
                    control.Value = control.Minimum;
                }
                else if (newValue > control.Maximum)
                {
                    control.Value = control.Maximum;
                }
            }
        }

        #endregion
    }
}
