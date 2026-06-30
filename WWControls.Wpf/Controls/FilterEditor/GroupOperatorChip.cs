using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using WWControls.Core;

namespace WWControls.Wpf
{
    /// <summary>
    /// Dropdown chip rendering a group's <see cref="LogicalOperator"/> (And / Or / NotAnd / NotOr).
    /// The chip text always shows the operator's friendly name; clicking opens a popup ListBox
    /// with the four options.
    /// </summary>
    public class GroupOperatorChip : Control
    {
        public static readonly DependencyProperty OperatorProperty =
            DependencyProperty.Register(nameof(Operator), typeof(LogicalOperator), typeof(GroupOperatorChip),
                new FrameworkPropertyMetadata(LogicalOperator.And,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnOperatorChanged));

        public static readonly DependencyProperty DisplayTextProperty =
            DependencyProperty.Register(nameof(DisplayText), typeof(string), typeof(GroupOperatorChip),
                new PropertyMetadata(LogicalOperator.And.DisplayText()));

        private static readonly DependencyPropertyKey AvailableOperatorsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(AvailableOperators), typeof(IReadOnlyList<LogicalOperator>),
                typeof(GroupOperatorChip),
                new PropertyMetadata(new[]
                {
                    LogicalOperator.And,
                    LogicalOperator.Or,
                    LogicalOperator.NotAnd,
                    LogicalOperator.NotOr
                }));

        public static readonly DependencyProperty AvailableOperatorsProperty = AvailableOperatorsPropertyKey.DependencyProperty;

        public LogicalOperator Operator
        {
            get => (LogicalOperator)GetValue(OperatorProperty);
            set => SetValue(OperatorProperty, value);
        }

        public string DisplayText
        {
            get => (string)GetValue(DisplayTextProperty);
            private set => SetValue(DisplayTextProperty, value);
        }

        public IReadOnlyList<LogicalOperator> AvailableOperators =>
            (IReadOnlyList<LogicalOperator>)GetValue(AvailableOperatorsProperty);

        public GroupOperatorChip()
        {
            DefaultStyleKey = typeof(GroupOperatorChip);
        }

        private static void OnOperatorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GroupOperatorChip chip)
            {
                chip.DisplayText = chip.Operator.DisplayText();
            }
        }
    }
}
