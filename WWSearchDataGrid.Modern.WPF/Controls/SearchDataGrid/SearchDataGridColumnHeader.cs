using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Custom column header. Subclassing is needed because the stock header is a
    /// <see cref="ButtonBase"/> that triggers sort on every click — including clicks on
    /// descendant buttons (filter popup, etc.). <see cref="OnClick"/> suppresses sort when
    /// the click came from a descendant button. Also exposes <see cref="HasAdvancedFilter"/> /
    /// <see cref="HasActiveFilter"/> DPs the header template binds against.
    /// </summary>
    public class SearchDataGridColumnHeader : DataGridColumnHeader
    {
        static SearchDataGridColumnHeader()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(SearchDataGridColumnHeader),
                new FrameworkPropertyMetadata(typeof(SearchDataGridColumnHeader)));
        }

        /// <summary>
        /// True when the column has a committed rule-filter. Bindable from the header template
        /// for a persistent "advanced filter active" highlight.
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
        /// True when the column has any active filter affecting row visibility. Bindable from
        /// the header template; distinct from the advanced-filter-only <see cref="HasAdvancedFilter"/>.
        /// </summary>
        public static readonly DependencyProperty HasActiveFilterProperty =
            DependencyProperty.Register(nameof(HasActiveFilter), typeof(bool), typeof(SearchDataGridColumnHeader),
                new PropertyMetadata(false));

        public bool HasActiveFilter
        {
            get => (bool)GetValue(HasActiveFilterProperty);
            set => SetValue(HasActiveFilterProperty, value);
        }

        // Captured in OnPreviewMouseLeftButtonDown, consulted (and reset) in OnClick.
        private bool _clickFromChildButton;

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            _clickFromChildButton = IsClickFromChildButton(e.OriginalSource as DependencyObject);
            base.OnPreviewMouseLeftButtonDown(e);
        }

        /// <summary>
        /// Skips base.OnClick (and thus the sort gesture) when the click came from a descendant
        /// button. The descendant's own Click has already raised through its own machinery.
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
