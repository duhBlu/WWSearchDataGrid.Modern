using System.Windows;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Excel-style filter element: search box + Select All toggle + checkbox list of distinct
    /// column values. Backed by the column's <see cref="FilterValueManager"/>, which translates
    /// checkbox selections into <c>IsAnyOf</c> / <c>IsNoneOf</c> filter rules.
    /// </summary>
    public class CheckedListFilterElement : FilterElementBase
    {
        static CheckedListFilterElement()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(CheckedListFilterElement),
                new FrameworkPropertyMetadata(typeof(CheckedListFilterElement)));
        }

        /// <summary>
        /// When <c>true</c> (default), a freshly-opened popup with no prior filter shows all
        /// values checked — matching DevExpress's <c>SelectAllWhenFilterIsNull</c> semantics
        /// where "no filter" is rendered as "everything selected." Set to <c>false</c> to
        /// start with nothing checked (filter excludes everything until the user picks values).
        /// </summary>
        public static readonly DependencyProperty SelectAllWhenFilterIsNullProperty =
            DependencyProperty.Register(
                nameof(SelectAllWhenFilterIsNull),
                typeof(bool),
                typeof(CheckedListFilterElement),
                new PropertyMetadata(true));

        public bool SelectAllWhenFilterIsNull
        {
            get => (bool)GetValue(SelectAllWhenFilterIsNullProperty);
            set => SetValue(SelectAllWhenFilterIsNullProperty, value);
        }

        /// <summary>
        /// When <c>true</c> (default), each checkbox row shows the per-value occurrence count
        /// in parentheses next to the display text. Set to <c>false</c> to hide the count
        /// column entirely — useful when the count is uninteresting (boolean / enum columns)
        /// or when the list is large and the extra column adds visual noise.
        /// </summary>
        public static readonly DependencyProperty ShowCountsProperty =
            DependencyProperty.Register(
                nameof(ShowCounts),
                typeof(bool),
                typeof(CheckedListFilterElement),
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
                typeof(CheckedListFilterElement),
                new PropertyMetadata(null));

        /// <summary>
        /// Read-only DP exposed so the templated style can bind via
        /// <c>{Binding FilterValueManager, RelativeSource={RelativeSource TemplatedParent}}</c>
        /// or <c>{TemplateBinding FilterValueManager}</c>. Set from
        /// <see cref="OnContextAttached"/> whenever a new context arrives; <c>null</c> while
        /// detached.
        /// </summary>
        public static readonly DependencyProperty FilterValueManagerProperty = FilterValueManagerPropertyKey.DependencyProperty;

        public FilterValueManager FilterValueManager => (FilterValueManager)GetValue(FilterValueManagerProperty);

        private ICommand _selectAllToggleCommand;

        /// <summary>
        /// Toggles the tri-state Select-All checkbox. Drives
        /// <see cref="FilterValueManager.SelectAllCommand"/> when none / mixed are checked,
        /// and <see cref="FilterValueManager.ClearAllCommand"/> when all are checked — same
        /// cycle <see cref="ColumnFilterPopup"/> uses for its values tab.
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

            // SelectAllWhenFilterIsNull=false semantics: when the popup opens against a column
            // that has no active filter, start with every checkbox UNchecked instead of the
            // default "all checked = no filter" rendering. Toggling them clear causes
            // FilterValueManager.SyncToRulesAndApply to emit the matching exclusion rule, so
            // the grid immediately filters out every row until the user picks values.
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
