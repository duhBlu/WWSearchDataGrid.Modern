using System.Windows;
using System.Windows.Controls;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// FilterPanel control for displaying and managing active filters
    /// </summary>
    public class FilterPanel : Control
    {
        #region Dependency Properties

        public static readonly DependencyProperty FilterPanelViewModelProperty =
            DependencyProperty.Register("FilterPanelViewModel", typeof(FilterPanelViewModel), typeof(FilterPanel),
                new PropertyMetadata(null, OnFilterPanelViewModelChanged));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the filter panel view model
        /// </summary>
        public FilterPanelViewModel FilterPanelViewModel
        {
            get => (FilterPanelViewModel)GetValue(FilterPanelViewModelProperty);
            set => SetValue(FilterPanelViewModelProperty, value);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the FilterPanel class
        /// </summary>
        public FilterPanel()
        {
            DefaultStyleKey = typeof(FilterPanel);
        }

        #endregion

        #region Property Changed Handlers

        /// <summary>
        /// Handles changes to the FilterPanelViewModel property
        /// </summary>
        private static void OnFilterPanelViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FilterPanel panel)
            {
                panel.DataContext = e.NewValue;
            }
        }

        #endregion

        #region Control Template Methods

        /// <summary>
        /// When the template is applied
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            
            // Set DataContext to the FilterPanelViewModel if available
            if (FilterPanelViewModel != null)
            {
                DataContext = FilterPanelViewModel;
            }
        }

        #endregion
    }
}