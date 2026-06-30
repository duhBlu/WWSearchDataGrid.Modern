using System.Windows;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf
{
    /// <summary>
    /// DateTime-aware filter element: Year / Month / Day (/ Time when the display format shows it)
    /// checkbox tree. Reuses <see cref="FilterValueManager.DateTreeRoots"/> — the same hierarchy the
    /// default tabbed popup shows on its Filter Values tab — so date filters built here use the
    /// same contiguous-range BetweenDates / IsAnyOf rule shapes the values tab produces.
    /// </summary>
    public class DateTreeFilterElement : FilterElementBase
    {
        static DateTreeFilterElement()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DateTreeFilterElement),
                new FrameworkPropertyMetadata(typeof(DateTreeFilterElement)));
        }

        /// <summary>
        /// When <c>true</c> (default), a freshly-opened popup with no prior filter shows every
        /// leaf checked. Set to <c>false</c> to start with nothing checked.
        /// </summary>
        public static readonly DependencyProperty SelectAllWhenFilterIsNullProperty =
            DependencyProperty.Register(
                nameof(SelectAllWhenFilterIsNull),
                typeof(bool),
                typeof(DateTreeFilterElement),
                new PropertyMetadata(true));

        public bool SelectAllWhenFilterIsNull
        {
            get => (bool)GetValue(SelectAllWhenFilterIsNullProperty);
            set => SetValue(SelectAllWhenFilterIsNullProperty, value);
        }

        /// <summary>
        /// When <c>true</c> (default), each tree node shows its leaf-occurrence count in
        /// parentheses next to the display name. Set to <c>false</c> to hide counts.
        /// </summary>
        public static readonly DependencyProperty ShowCountsProperty =
            DependencyProperty.Register(
                nameof(ShowCounts),
                typeof(bool),
                typeof(DateTreeFilterElement),
                new PropertyMetadata(true));

        public bool ShowCounts
        {
            get => (bool)GetValue(ShowCountsProperty);
            set => SetValue(ShowCountsProperty, value);
        }

        private static readonly DependencyPropertyKey FilterValueManagerPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(FilterValueManager),
                typeof(FilterValueManager),
                typeof(DateTreeFilterElement),
                new PropertyMetadata(null));

        /// <summary>
        /// Read-only DP exposed so the templated style can bind via
        /// <c>{TemplateBinding FilterValueManager}</c>. Set from
        /// <see cref="OnContextAttached"/>; <c>null</c> while detached.
        /// </summary>
        public static readonly DependencyProperty FilterValueManagerProperty = FilterValueManagerPropertyKey.DependencyProperty;

        public FilterValueManager FilterValueManager => (FilterValueManager)GetValue(FilterValueManagerProperty);

        private ICommand _selectAllToggleCommand;

        /// <summary>
        /// Toggles the tri-state Select-All checkbox at the top of the tree. Drives
        /// <see cref="FilterValueManager.SelectAllCommand"/> when none / mixed are checked,
        /// and <see cref="FilterValueManager.ClearAllCommand"/> when all are checked.
        /// </summary>
        public ICommand SelectAllToggleCommand =>
            _selectAllToggleCommand ??= new RelayCommand(_ =>
            {
                var fvm = FilterValueManager;
                if (fvm == null) return;
                if (fvm.SelectAllState == true)
                    fvm.ClearAllCommand?.Execute(null);
                else
                    fvm.SelectAllCommand?.Execute(null);
            });

        protected override void OnContextAttached(FilterElementContext context)
        {
            SetValue(FilterValueManagerPropertyKey, context?.FilterValueManager);

            if (!SelectAllWhenFilterIsNull
                && context?.FilterValueManager != null
                && context.Controller != null
                && !context.Controller.HasCustomExpression)
            {
                context.FilterValueManager.ClearAllCommand?.Execute(null);
            }
        }

        protected override void OnContextDetached(FilterElementContext context)
        {
            SetValue(FilterValueManagerPropertyKey, null);
        }
    }
}
