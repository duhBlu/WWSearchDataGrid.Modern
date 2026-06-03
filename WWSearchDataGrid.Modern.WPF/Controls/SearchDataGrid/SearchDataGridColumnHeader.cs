using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Custom column header. Subclassing is needed because the stock header is a
    /// <see cref="ButtonBase"/> that triggers sort on every click — including clicks on
    /// descendant buttons (filter popup, etc.). <see cref="OnClick"/> suppresses sort when
    /// the click came from a descendant button. Also hosts the attached property
    /// <see cref="DescriptorProperty"/>, which the column generator pins on each created
    /// <see cref="DataGridColumn"/> so the header template can bind directly to its matching
    /// <see cref="ColumnDataBase"/> descriptor — e.g. <c>Column.(sdg:SearchDataGridColumnHeader.Descriptor).IsFiltered</c>
    /// for the "filtered column" highlight, without any C# subscription wiring.
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
        /// Attached on the generated <see cref="DataGridColumn"/> (the header's <c>Column</c>)
        /// during <see cref="ColumnDataBase.CreateDataGridColumn"/> and pointing at the originating
        /// descriptor. Used by the header template to bind directly against descriptor-side DPs
        /// such as <see cref="ColumnDataBase.IsFiltered"/>.
        /// </summary>
        public static readonly DependencyProperty DescriptorProperty =
            DependencyProperty.RegisterAttached(
                "Descriptor",
                typeof(ColumnDataBase),
                typeof(SearchDataGridColumnHeader),
                new PropertyMetadata(null));

        public static ColumnDataBase GetDescriptor(DependencyObject obj) =>
            (ColumnDataBase)obj.GetValue(DescriptorProperty);

        public static void SetDescriptor(DependencyObject obj, ColumnDataBase value) =>
            obj.SetValue(DescriptorProperty, value);

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
