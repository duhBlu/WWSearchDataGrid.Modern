using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Orange chip that selects which <see cref="GridColumn"/> a condition row targets.
    /// Click opens a popup ListBox bound to <see cref="AvailableColumns"/>; selecting an item
    /// updates <see cref="SelectedColumn"/> and dismisses the popup.
    /// </summary>
    [TemplatePart(Name = "PART_Toggle", Type = typeof(ToggleButton))]
    [TemplatePart(Name = "PART_List", Type = typeof(ListBox))]
    public class ColumnNameTokenEditor : Control
    {
        public static readonly DependencyProperty SelectedColumnProperty =
            DependencyProperty.Register(nameof(SelectedColumn), typeof(GridColumn), typeof(ColumnNameTokenEditor),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedColumnChanged));

        public static readonly DependencyProperty AvailableColumnsProperty =
            DependencyProperty.Register(nameof(AvailableColumns), typeof(IEnumerable<GridColumn>), typeof(ColumnNameTokenEditor),
                new PropertyMetadata(null));

        public static readonly DependencyProperty DisplayTextProperty =
            DependencyProperty.Register(nameof(DisplayText), typeof(string), typeof(ColumnNameTokenEditor),
                new PropertyMetadata("(column)"));

        private ToggleButton _toggle;
        private ListBox _list;

        public GridColumn SelectedColumn
        {
            get => (GridColumn)GetValue(SelectedColumnProperty);
            set => SetValue(SelectedColumnProperty, value);
        }

        public IEnumerable<GridColumn> AvailableColumns
        {
            get => (IEnumerable<GridColumn>)GetValue(AvailableColumnsProperty);
            set => SetValue(AvailableColumnsProperty, value);
        }

        public string DisplayText
        {
            get => (string)GetValue(DisplayTextProperty);
            set => SetValue(DisplayTextProperty, value);
        }

        public ColumnNameTokenEditor()
        {
            DefaultStyleKey = typeof(ColumnNameTokenEditor);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_list != null)
            {
                _list.PreviewMouseLeftButtonUp -= OnListItemClicked;
            }

            _toggle = GetTemplateChild("PART_Toggle") as ToggleButton;
            _list = GetTemplateChild("PART_List") as ListBox;

            if (_list != null)
            {
                _list.PreviewMouseLeftButtonUp += OnListItemClicked;
            }
        }

        private void OnListItemClicked(object sender, MouseButtonEventArgs e)
        {
            // Close the popup on any click inside the ListBox — covers picking a new item
            // and re-clicking the already-selected one (SelectionChanged wouldn't fire then).
            if (ItemsControl.ContainerFromElement(_list, e.OriginalSource as DependencyObject) is ListBoxItem item
                && item.DataContext is GridColumn column)
            {
                SelectedColumn = column;
                if (_toggle != null) _toggle.IsChecked = false;
            }
        }

        private static void OnSelectedColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColumnNameTokenEditor chip)
            {
                chip.DisplayText = chip.ResolveDisplayText();
            }
        }

        private string ResolveDisplayText()
        {
            var col = SelectedColumn;
            if (col == null) return "(column)";

            if (!string.IsNullOrEmpty(col.HeaderCaption)) return col.HeaderCaption;
            if (!string.IsNullOrEmpty(col.ColumnDisplayName)) return col.ColumnDisplayName;
            return col.FieldName ?? string.Empty;
        }
    }
}
