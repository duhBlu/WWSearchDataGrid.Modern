using System;
using System.Windows;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Provides attached properties for configuring data grid columns with search and filtering capabilities.
    /// These properties work with all DataGridColumn types including TextColumn, CheckBoxColumn, TemplateColumn, and ComboBoxColumn.
    /// </summary>
    public static class GridColumn
    {
        #region EnableComplexFiltering Attached Property

        /// <summary>
        /// Identifies the EnableComplexFiltering attached property.
        /// Enables or disables complex filtering UI for a column.
        /// Default value: true
        /// </summary>
        public static readonly DependencyProperty EnableComplexFilteringProperty =
            DependencyProperty.RegisterAttached(
                "EnableComplexFiltering",
                typeof(bool),
                typeof(GridColumn),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Gets the value indicating whether complex filtering is enabled for the specified column.
        /// </summary>
        /// <param name="element">The column to query</param>
        /// <returns>True if complex filtering is enabled; otherwise, false</returns>
        public static bool GetEnableComplexFiltering(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return (bool)element.GetValue(EnableComplexFilteringProperty);
        }

        /// <summary>
        /// Sets whether complex filtering is enabled for the specified column.
        /// </summary>
        /// <param name="element">The column to configure</param>
        /// <param name="value">True to enable complex filtering; false to disable</param>
        public static void SetEnableComplexFiltering(DependencyObject element, bool value)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.SetValue(EnableComplexFilteringProperty, value);
        }

        #endregion

        #region CustomSearchTemplate Attached Property

        /// <summary>
        /// Identifies the CustomSearchTemplate attached property.
        /// Allows specifying a custom search template implementation for a column.
        /// Default value: typeof(SearchTemplate)
        /// </summary>
        public static readonly DependencyProperty CustomSearchTemplateProperty =
            DependencyProperty.RegisterAttached(
                "CustomSearchTemplate",
                typeof(Type),
                typeof(GridColumn),
                new FrameworkPropertyMetadata(typeof(SearchTemplate)));

        /// <summary>
        /// Gets the custom search template type for the specified column.
        /// </summary>
        /// <param name="element">The column to query</param>
        /// <returns>The Type of the custom search template</returns>
        public static Type GetCustomSearchTemplate(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return (Type)element.GetValue(CustomSearchTemplateProperty);
        }

        /// <summary>
        /// Sets the custom search template type for the specified column.
        /// </summary>
        /// <param name="element">The column to configure</param>
        /// <param name="value">The Type of the custom search template to use</param>
        public static void SetCustomSearchTemplate(DependencyObject element, Type value)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.SetValue(CustomSearchTemplateProperty, value);
        }

        #endregion

        #region AllowRuleValueFiltering Attached Property

        /// <summary>
        /// Identifies the AllowRuleValueFiltering attached property.
        /// Shows or hides the advanced filter button for a column.
        /// Default value: true
        /// </summary>
        public static readonly DependencyProperty AllowRuleValueFilteringProperty =
            DependencyProperty.RegisterAttached(
                "AllowRuleValueFiltering",
                typeof(bool),
                typeof(GridColumn),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// Gets the value indicating whether rule-based value filtering is allowed for the specified column.
        /// </summary>
        /// <param name="element">The column to query</param>
        /// <returns>True if rule-based value filtering is allowed; otherwise, false</returns>
        public static bool GetAllowRuleValueFiltering(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return (bool)element.GetValue(AllowRuleValueFilteringProperty);
        }

        /// <summary>
        /// Sets whether rule-based value filtering is allowed for the specified column.
        /// </summary>
        /// <param name="element">The column to configure</param>
        /// <param name="value">True to allow rule-based value filtering; false to disable</param>
        public static void SetAllowRuleValueFiltering(DependencyObject element, bool value)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.SetValue(AllowRuleValueFilteringProperty, value);
        }

        #endregion

        #region UseCheckBoxInSearchBox Attached Property

        /// <summary>
        /// Identifies the UseCheckBoxInSearchBox attached property.
        /// Explicitly enables checkbox filtering mode in the search box for a column.
        /// This replaces auto-detection logic and works with any column type.
        /// Default value: false
        /// </summary>
        public static readonly DependencyProperty UseCheckBoxInSearchBoxProperty =
            DependencyProperty.RegisterAttached(
                "UseCheckBoxInSearchBox",
                typeof(bool),
                typeof(GridColumn),
                new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets the value indicating whether checkbox filtering should be used in the search box for the specified column.
        /// </summary>
        /// <param name="element">The column to query</param>
        /// <returns>True if checkbox filtering should be used; otherwise, false</returns>
        public static bool GetUseCheckBoxInSearchBox(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return (bool)element.GetValue(UseCheckBoxInSearchBoxProperty);
        }

        /// <summary>
        /// Sets whether checkbox filtering should be used in the search box for the specified column.
        /// </summary>
        /// <param name="element">The column to configure</param>
        /// <param name="value">True to use checkbox filtering; false for text-based filtering</param>
        public static void SetUseCheckBoxInSearchBox(DependencyObject element, bool value)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.SetValue(UseCheckBoxInSearchBoxProperty, value);
        }

        #endregion

        #region FilterMemberPath Attached Property

        /// <summary>
        /// Identifies the FilterMemberPath attached property.
        /// Specifies the property path used for retrieving filter values from data objects.
        /// This path determines which property is used to populate filter dropdown values.
        ///
        /// Resolution priority:
        /// 1. FilterMemberPath (if explicitly set)
        /// 2. Column's SortMemberPath
        /// 3. Binding path extracted from DataGridBoundColumn.Binding
        ///
        /// Default value: null (falls back to SortMemberPath or Binding path)
        /// </summary>
        public static readonly DependencyProperty FilterMemberPathProperty =
            DependencyProperty.RegisterAttached(
                "FilterMemberPath",
                typeof(string),
                typeof(GridColumn),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets the filter member path for the specified column.
        /// Returns the explicit property path to use for filtering, or null to use fallback logic.
        /// </summary>
        /// <param name="element">The column to query</param>
        /// <returns>The property path for filtering, or null if not explicitly set</returns>
        public static string GetFilterMemberPath(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return (string)element.GetValue(FilterMemberPathProperty);
        }

        /// <summary>
        /// Sets the filter member path for the specified column.
        /// Use this to explicitly control which property is used for filter value retrieval.
        /// </summary>
        /// <param name="element">The column to configure</param>
        /// <param name="value">The property path to use for filtering (e.g., "Department.Name")</param>
        public static void SetFilterMemberPath(DependencyObject element, string value)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.SetValue(FilterMemberPathProperty, value);
        }

        #endregion
    }
}
