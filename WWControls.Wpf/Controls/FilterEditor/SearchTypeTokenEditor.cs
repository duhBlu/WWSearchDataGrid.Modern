using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Light-blue chip that selects which <see cref="SearchType"/> a condition row uses.
    /// Click opens a popup ListBox bound to the condition's <see cref="ValidSearchTypes"/>;
    /// selecting an item updates <see cref="SelectedSearchType"/> and dismisses the popup.
    /// </summary>
    [TemplatePart(Name = "PART_Toggle", Type = typeof(ToggleButton))]
    [TemplatePart(Name = "PART_List", Type = typeof(ListBox))]
    public class SearchTypeTokenEditor : Control
    {
        public static readonly DependencyProperty SelectedSearchTypeProperty =
            DependencyProperty.Register(nameof(SelectedSearchType), typeof(SearchType), typeof(SearchTypeTokenEditor),
                new FrameworkPropertyMetadata(SearchType.Contains,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedSearchTypeChanged));

        public static readonly DependencyProperty ValidSearchTypesProperty =
            DependencyProperty.Register(nameof(ValidSearchTypes), typeof(IEnumerable<SearchType>), typeof(SearchTypeTokenEditor),
                new PropertyMetadata(null));

        public static readonly DependencyProperty DisplayTextProperty =
            DependencyProperty.Register(nameof(DisplayText), typeof(string), typeof(SearchTypeTokenEditor),
                new PropertyMetadata(string.Empty));

        private ToggleButton _toggle;
        private ListBox _list;

        public SearchType SelectedSearchType
        {
            get => (SearchType)GetValue(SelectedSearchTypeProperty);
            set => SetValue(SelectedSearchTypeProperty, value);
        }

        public IEnumerable<SearchType> ValidSearchTypes
        {
            get => (IEnumerable<SearchType>)GetValue(ValidSearchTypesProperty);
            set => SetValue(ValidSearchTypesProperty, value);
        }

        public string DisplayText
        {
            get => (string)GetValue(DisplayTextProperty);
            set => SetValue(DisplayTextProperty, value);
        }

        public SearchTypeTokenEditor()
        {
            DefaultStyleKey = typeof(SearchTypeTokenEditor);
            DisplayText = SearchTypeRegistry.GetMetadata(SelectedSearchType)?.DisplayName ?? SelectedSearchType.ToString();
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
            // Close on any click inside the ListBox — covers re-selecting the current value too.
            if (ItemsControl.ContainerFromElement(_list, e.OriginalSource as DependencyObject) is ListBoxItem item
                && item.DataContext is SearchType st)
            {
                SelectedSearchType = st;
                if (_toggle != null) _toggle.IsChecked = false;
            }
        }

        private static void OnSelectedSearchTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchTypeTokenEditor chip)
            {
                var meta = SearchTypeRegistry.GetMetadata(chip.SelectedSearchType);
                chip.DisplayText = meta?.DisplayName ?? chip.SelectedSearchType.ToString();
            }
        }
    }
}
