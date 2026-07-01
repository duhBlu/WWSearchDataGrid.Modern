using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// A placeholder element for use inside a <see cref="SearchDataGrid.EditFormTemplate"/>: drops
    /// the editor for the column named by <see cref="FieldName"/> into a custom form layout, reusing
    /// that column's effective edit template (display template when the column is read-only).
    /// Mirrors DevExpress's <c>dxg:EditFormEditor FieldName="..."</c>.
    /// </summary>
    /// <remarks>
    /// Resolution walks up to the owning <see cref="SearchDataGrid"/> from the visual tree, so this
    /// is valid wherever the form is hosted inside the grid (the inline / hide-row row-details host).
    /// The inherited <c>DataContext</c> is the editing row item; the editor's two-way bindings write
    /// straight to that item under the grid's open row transaction.
    /// </remarks>
    public class EditFormEditor : ContentControl
    {
        static EditFormEditor()
        {
            // Use the stock ContentControl template (a ContentPresenter) so the resolved editor
            // renders without depending on a theme-provided style for EditFormEditor itself.
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(EditFormEditor),
                new FrameworkPropertyMetadata(typeof(ContentControl)));
        }

        public EditFormEditor()
        {
            Focusable = false;
            HorizontalContentAlignment = HorizontalAlignment.Stretch;
            VerticalContentAlignment = VerticalAlignment.Center;
            Loaded += (_, _) => Resolve();
        }

        /// <summary>The <see cref="ColumnDataBase.FieldName"/> of the column whose editor to render.</summary>
        public static readonly DependencyProperty FieldNameProperty =
            DependencyProperty.Register(nameof(FieldName), typeof(string), typeof(EditFormEditor),
                new PropertyMetadata(null, OnFieldNameChanged));

        public string FieldName
        {
            get => (string)GetValue(FieldNameProperty);
            set => SetValue(FieldNameProperty, value);
        }

        private static void OnFieldNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as EditFormEditor)?.Resolve();

        /// <summary>
        /// Binds this editor's content to the editing item (the inherited DataContext) and sets the
        /// content template to the resolved column's effective edit-form template. No-op until the
        /// owning grid and a matching column are reachable. The editor renders its own border via the
        /// inherited <see cref="EditorChrome.ShowEditorBorderProperty"/> the form sets; to suppress
        /// it on a specific field set <c>sdg:EditorChrome.ShowEditorBorder="False"</c> here.
        /// </summary>
        private void Resolve()
        {
            if (string.IsNullOrEmpty(FieldName))
                return;

            var grid = VisualTreeHelperMethods.FindVisualAncestor<SearchDataGrid>(this);
            var descriptor = grid?.GridColumns?.FirstOrDefault(c => c?.FieldName == FieldName);
            if (descriptor == null)
                return; // Not reachable yet — retried on Loaded.

            bool readOnly = descriptor.InternalColumn?.IsReadOnly ?? descriptor.IsReadOnly;
            DataTemplate template = readOnly
                ? descriptor.ResolveEffectiveCellDisplayTemplate()
                : descriptor.ResolveEffectiveEditFormCellTemplate();
            template ??= descriptor.ResolveEffectiveCellDisplayTemplate();

            // Content = the inherited DataContext (the editing row item), so the editor template
            // binds its field path against the row exactly as an in-grid cell editor would.
            SetBinding(ContentProperty, new Binding());
            ContentTemplate = template;
        }
    }
}
