using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Custom column header for <see cref="SearchDataGrid"/>. Subclassing
    /// <see cref="DataGridColumnHeader"/> is necessary because the stock header is itself a
    /// <see cref="ButtonBase"/> whose <see cref="ButtonBase.Click"/> triggers column sort —
    /// clicks on any interactive child (filter-popup button, future Header-mode editors) would
    /// otherwise cycle the sort direction alongside the child's own action. Owning the header
    /// type lets us override <see cref="OnClick"/> to inspect the click's
    /// <see cref="RoutedEventArgs.OriginalSource"/> and skip the sort when the source is a
    /// descendant <see cref="ButtonBase"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The subclass also adds bindable filter-state DPs (<see cref="HasAdvancedFilter"/>,
    /// <see cref="HasActiveFilter"/>) so the header template can drive visual indicators —
    /// e.g. always-show the filter-popup button when an advanced filter is committed — without
    /// chasing the column's filter host through an attached-property bridge.
    /// </para>
    /// <para>
    /// Materialized by <see cref="SearchDataGridColumnHeadersPresenter"/>. The
    /// <see cref="SearchDataGrid"/> template references the custom presenter explicitly so the
    /// container type is guaranteed; the <c>ColumnHeaderStyle</c> on the grid targets this
    /// type so consumer style overrides resolve through the same theme-key chain.
    /// </para>
    /// </remarks>
    public class SearchDataGridColumnHeader : DataGridColumnHeader
    {
        static SearchDataGridColumnHeader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SearchDataGridColumnHeader),
                new FrameworkPropertyMetadata(typeof(SearchDataGridColumnHeader)));
        }

        /// <summary>
        /// True when the column has a committed rule-filter (i.e. the column's
        /// <c>IColumnFilterHost.HasAdvancedFilter</c> is true). Bindable from the header
        /// template to drive a persistent "filter is active" highlight on the filter-popup
        /// button. Driven from <see cref="SearchDataGrid"/> as filter state changes.
        /// </summary>
        public static readonly DependencyProperty HasAdvancedFilterProperty =
            DependencyProperty.Register(nameof(HasAdvancedFilter), typeof(bool), typeof(SearchDataGridColumnHeader),
                new PropertyMetadata(false));

        public bool HasAdvancedFilter
        {
            get => (bool)GetValue(HasAdvancedFilterProperty);
            set => SetValue(HasAdvancedFilterProperty, value);
        }

        /// <summary>
        /// True when the column has ANY active filter (text, checkbox, advanced — anything that
        /// affects row visibility). Bindable from the header template for header-level visual
        /// cues distinct from the advanced-filter-specific indicator.
        /// </summary>
        public static readonly DependencyProperty HasActiveFilterProperty =
            DependencyProperty.Register(nameof(HasActiveFilter), typeof(bool), typeof(SearchDataGridColumnHeader),
                new PropertyMetadata(false));

        public bool HasActiveFilter
        {
            get => (bool)GetValue(HasActiveFilterProperty);
            set => SetValue(HasActiveFilterProperty, value);
        }

        // Press cycle starts in OnPreviewMouseLeftButtonDown, completes in OnClick. We record
        // whether the press originated on a ButtonBase descendant in the down phase and check
        // it in the click phase. The default value (false) is reset after each consultation so
        // a stray sort attempt later doesn't carry over.
        private bool _clickFromChildButton;

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _clickFromChildButton = IsClickFromChildButton(e.OriginalSource as DependencyObject);
            base.OnPreviewMouseLeftButtonDown(e);
        }

        /// <summary>
        /// Override the sort gesture. <see cref="DataGridColumnHeader.OnClick"/> raises the
        /// <see cref="ButtonBase.ClickEvent"/> and then calls <c>DataGrid.PerformSort</c>.
        /// Skipping <see cref="base.OnClick"/> when the click originated from a descendant
        /// button suppresses both — the descendant's own <see cref="ButtonBase.Click"/> has
        /// already raised through its own ButtonBase machinery, so its command is unaffected.
        /// </summary>
        protected override void OnClick()
        {
            if (_clickFromChildButton)
            {
                _clickFromChildButton = false;
                return;
            }
            base.OnClick();
        }

        private bool IsClickFromChildButton(DependencyObject source)
        {
            var cursor = source;
            while (cursor != null && !ReferenceEquals(cursor, this))
            {
                if (cursor is ButtonBase) return true;
                cursor = VisualTreeHelper.GetParent(cursor) ?? LogicalTreeHelper.GetParent(cursor);
            }
            return false;
        }
    }
}
