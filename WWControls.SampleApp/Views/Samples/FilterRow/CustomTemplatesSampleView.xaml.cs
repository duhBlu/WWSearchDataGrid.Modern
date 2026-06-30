using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using WWControls.Wpf;

namespace WWControls.SampleApp.Views.Samples.FilterRow
{
    public partial class CustomTemplatesSampleView : UserControl
    {
        public CustomTemplatesSampleView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Radio-button group templates can't bind <c>IsChecked</c> back to a single shared
        /// <c>Value</c> cleanly — each radio is bound to its own data item. So when a radio is
        /// clicked, walk up the visual tree to the <see cref="ItemsControl"/> root (whose
        /// DataContext is the <see cref="EditGridCellData"/> handed to the template) and write
        /// the clicked radio's bound item into <c>Value</c>. That flows through to the host's
        /// <c>SearchValue</c> via the two-way binding established in
        /// <c>ColumnFilterControl.EnsureFilterCellData</c>.
        /// </summary>
        private void StatusRadio_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton rb) return;

            var ancestor = FindAncestor<ItemsControl>(rb);
            if (ancestor?.DataContext is EditGridCellData cellData)
                cellData.Value = rb.DataContext;
        }

        private static T? FindAncestor<T>(DependencyObject start) where T : DependencyObject
        {
            DependencyObject? current = start;
            while (current != null)
            {
                if (current is T match) return match;
                current = current is Visual
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
