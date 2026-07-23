using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Two-segment AND | OR toggle for a filter group's <see cref="LogicalOperator"/>. The active
    /// segment is highlighted; clicking a segment sets the group's combiner. Only the non-negated
    /// operators are offered — negation in the Filter Editor lives on the condition operator
    /// (NotEquals, DoesNotContain, IsNoneOf, …), not on the group.
    /// </summary>
    public class GroupOperatorChip : Control
    {
        public static readonly DependencyProperty OperatorProperty =
            DependencyProperty.Register(nameof(Operator), typeof(LogicalOperator), typeof(GroupOperatorChip),
                new FrameworkPropertyMetadata(LogicalOperator.And,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnOperatorChanged));

        private static readonly DependencyPropertyKey IsAndPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsAnd), typeof(bool), typeof(GroupOperatorChip),
                new PropertyMetadata(true));

        public static readonly DependencyProperty IsAndProperty = IsAndPropertyKey.DependencyProperty;

        private static readonly DependencyPropertyKey IsOrPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(IsOr), typeof(bool), typeof(GroupOperatorChip),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsOrProperty = IsOrPropertyKey.DependencyProperty;

        private ICommand setAndCommand;
        private ICommand setOrCommand;

        public GroupOperatorChip()
        {
            DefaultStyleKey = typeof(GroupOperatorChip);
        }

        public LogicalOperator Operator
        {
            get => (LogicalOperator)GetValue(OperatorProperty);
            set => SetValue(OperatorProperty, value);
        }

        /// <summary>True when the group's combiner is the And segment (And / NotAnd).</summary>
        public bool IsAnd
        {
            get => (bool)GetValue(IsAndProperty);
            private set => SetValue(IsAndPropertyKey, value);
        }

        /// <summary>True when the group's combiner is the Or segment (Or / NotOr).</summary>
        public bool IsOr
        {
            get => (bool)GetValue(IsOrProperty);
            private set => SetValue(IsOrPropertyKey, value);
        }

        public ICommand SetAndCommand =>
            setAndCommand ?? (setAndCommand = new RelayCommand(_ => Operator = LogicalOperator.And));

        public ICommand SetOrCommand =>
            setOrCommand ?? (setOrCommand = new RelayCommand(_ => Operator = LogicalOperator.Or));

        private static void OnOperatorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is GroupOperatorChip chip)
            {
                var op = (LogicalOperator)e.NewValue;
                bool isOr = op == LogicalOperator.Or || op == LogicalOperator.NotOr;
                chip.IsAnd = !isOr;
                chip.IsOr = isOr;
            }
        }
    }
}
