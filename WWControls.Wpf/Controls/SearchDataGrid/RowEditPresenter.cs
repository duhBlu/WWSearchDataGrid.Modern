using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace WWControls.Wpf
{
    /// <summary>
    /// The bright, column-aligned editor strip shown over a single row while the grid is in
    /// full-row ("edit entire row") edit mode. Hosts one editor per data column — the column's own
    /// <see cref="BaseEditSettings"/> edit template, bound to <see cref="EditingItem"/> — laid out
    /// against the grid's live column geometry by the inherited <see cref="ColumnAlignedRowPresenter"/>
    /// (the same alignment engine behind the filter and total-summary rows). A docked action bar
    /// carries the row-scoped Update / Cancel buttons, which call back into the owning
    /// <see cref="SearchDataGrid"/>.
    /// </summary>
    /// <remarks>
    /// Each per-column cell binds its <see cref="ContentControl.Content"/> to <see cref="EditingItem"/>
    /// (so the editor's <c>DataContext</c> is the row item and its two-way bindings write straight to
    /// the item under the grid's open <see cref="System.ComponentModel.IEditableObject"/> transaction).
    /// Because the bind is live, swapping <see cref="EditingItem"/> re-targets every editor without a
    /// rebuild — only a column change rebuilds the cells.
    /// </remarks>
    public class RowEditPresenter : ColumnAlignedRowPresenter
    {
        static RowEditPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(RowEditPresenter),
                new FrameworkPropertyMetadata(typeof(RowEditPresenter)));
        }

        /// <summary>Template part name of the hosted column-aligned editor panel.</summary>
        public const string PartRowEditPanelName = "PART_RowEditPanel";

        /// <summary>Template part name of the commit button.</summary>
        public const string PartUpdateButtonName = "PART_UpdateButton";

        /// <summary>Template part name of the cancel button.</summary>
        public const string PartCancelButtonName = "PART_CancelButton";

        private ButtonBase _updateButton;
        private ButtonBase _cancelButton;

        /// <summary>
        /// The row item currently being edited. Every per-column editor binds its content to this,
        /// so the editors re-target whenever it changes (no rebuild needed).
        /// </summary>
        public static readonly DependencyProperty EditingItemProperty =
            DependencyProperty.Register(nameof(EditingItem), typeof(object), typeof(RowEditPresenter),
                new PropertyMetadata(null));

        /// <summary>CLR accessor for <see cref="EditingItemProperty"/>.</summary>
        public object EditingItem
        {
            get => GetValue(EditingItemProperty);
            set => SetValue(EditingItemProperty, value);
        }

        protected override string PanelPartName => PartRowEditPanelName;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_updateButton != null) _updateButton.Click -= OnUpdateClick;
            if (_cancelButton != null) _cancelButton.Click -= OnCancelClick;

            _updateButton = GetTemplateChild(PartUpdateButtonName) as ButtonBase;
            _cancelButton = GetTemplateChild(PartCancelButtonName) as ButtonBase;

            if (_updateButton != null) _updateButton.Click += OnUpdateClick;
            if (_cancelButton != null) _cancelButton.Click += OnCancelClick;
        }

        private void OnUpdateClick(object sender, RoutedEventArgs e) => OwnerGrid?.CommitRowEdit();

        private void OnCancelClick(object sender, RoutedEventArgs e) => OwnerGrid?.CancelRowEdit();

        /// <summary>
        /// Produces the editor cell for <paramref name="column"/>: the column descriptor's edit
        /// template (display template when the column is read-only), bound to <see cref="EditingItem"/>.
        /// Columns with no descriptor or resolvable template render a blank, width-holding cell so the
        /// strip stays column-aligned.
        /// </summary>
        protected override UIElement ResolveChildForColumn(DataGridColumn column)
        {
            var cell = new ContentControl
            {
                Focusable = false,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                // Tagged so FocusEditorForColumn can find the cell that owns the clicked column.
                Tag = column,
            };
            cell.SetBinding(ContentControl.ContentProperty, new Binding(nameof(EditingItem)) { Source = this });

            var descriptor = OwnerGrid?.FindGridColumnDescriptor(column);
            DataTemplate template = null;
            if (descriptor != null)
            {
                // Read-only columns show their value (display template); editable columns get the
                // real editor. column.IsReadOnly reflects the effective read-only resolution the
                // descriptor pushes onto its internal column.
                if (!column.IsReadOnly)
                    template = descriptor.ResolveEffectiveCellEditTemplate();
                template ??= descriptor.ResolveEffectiveCellDisplayTemplate();
            }

            if (template != null)
                cell.ContentTemplate = template;

            return cell;
        }

        /// <summary>
        /// Moves keyboard focus to the editor hosting <paramref name="column"/> — used to hand off
        /// from the cell the user clicked to its counterpart in the strip, so typing continues
        /// uninterrupted. No-op if the strip isn't built yet or the column has no focusable editor.
        /// </summary>
        internal void FocusEditorForColumn(DataGridColumn column)
        {
            var panel = HostPanel;
            if (panel == null) return;

            ContentControl target = null;
            foreach (UIElement child in panel.Children)
            {
                if (child is ContentControl cc && ReferenceEquals(cc.Tag, column))
                {
                    target = cc;
                    break;
                }
            }

            target ??= FirstEditorCell(panel);
            if (target != null)
                FocusFirstFocusable(target);
        }

        private static ContentControl FirstEditorCell(Panel panel)
        {
            foreach (UIElement child in panel.Children)
                if (child is ContentControl cc)
                    return cc;
            return null;
        }

        /// <summary>Focuses the first focusable input element in <paramref name="root"/>'s visual subtree.</summary>
        private static void FocusFirstFocusable(DependencyObject root)
        {
            var editor = FindFocusable(root);
            editor?.Focus();
        }

        private static IInputElement FindFocusable(DependencyObject root)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                // Known editor types first; then any tab-stoppable Control (covers custom editors
                // like NumericUpDown / the segmented date editor) — but not the bare focusable
                // containers WPF stamps between the cell and its real input element.
                if (child is UIElement el && el.Focusable && el.IsEnabled
                    && (el is TextBox || el is ComboBox || el is CheckBox || el is DatePicker
                        || (el is Control c && c.IsTabStop)))
                {
                    return el;
                }
                var nested = FindFocusable(child);
                if (nested != null) return nested;
            }
            return null;
        }
    }
}
