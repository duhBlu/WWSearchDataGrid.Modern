using System;
using System.Windows;
using System.Windows.Controls;
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

        #region ColumnDisplayName Attached Property

        /// <summary>
        /// Identifies the ColumnDisplayName attached property.
        /// Specifies the display name shown in Column Chooser, Filter Panel, and other UI components.
        ///
        /// When not explicitly set, falls back to extracting text from the column's Header property.
        /// The fallback logic handles complex scenarios where Header may be a template or FrameworkElement.
        ///
        /// Default value: null (uses Header as fallback)
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;DataGridTextColumn Header="CustomerID"
        ///                     Binding="{Binding CustomerId}"
        ///                     local:GridColumn.ColumnDisplayName="Customer ID"/&gt;
        /// </code>
        /// </example>
        public static readonly DependencyProperty ColumnDisplayNameProperty =
            DependencyProperty.RegisterAttached(
                "ColumnDisplayName",
                typeof(string),
                typeof(GridColumn),
                new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets the explicit column display name for the specified column.
        /// Returns null if not explicitly set (use GetEffectiveColumnDisplayName for fallback logic).
        /// </summary>
        /// <param name="element">The column to query</param>
        /// <returns>The explicit display name, or null if not set</returns>
        public static string GetColumnDisplayName(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return (string)element.GetValue(ColumnDisplayNameProperty);
        }

        /// <summary>
        /// Sets the column display name for the specified column.
        /// This name will be shown in Column Chooser, Filter Panel, and other UI components.
        /// </summary>
        /// <param name="element">The column to configure</param>
        /// <param name="value">The user-friendly display name</param>
        public static void SetColumnDisplayName(DependencyObject element, string value)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.SetValue(ColumnDisplayNameProperty, value);
        }

        #endregion

        #region DefaultSearchMode Attached Property

        /// <summary>
        /// Identifies the DefaultSearchMode attached property.
        /// Specifies the search type used when creating temporary search templates from simple textbox input.
        /// <para>
        /// This property controls which SearchType is used when users type directly in the column's
        /// simple search textbox. It only affects the default behavior of temporary search templates,
        /// not the available options in the advanced filter UI.
        /// </para>
        /// <para>
        /// Default value: DefaultSearchMode.Contains (standard text search behavior)
        /// </para>
        /// <para>
        /// Available modes:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Contains: Finds matches anywhere in the value (default)</description></item>
        /// <item><description>StartsWith: ID columns, part numbers, customer codes where users know the prefix</description></item>
        /// <item><description>EndsWith: File extensions, domain suffixes, or similar patterns</description></item>
        /// <item><description>Equals: Status codes, enum values, or scenarios requiring exact matches</description></item>
        /// </list>
        /// <para>
        /// Note: This only affects simple textbox search behavior. The advanced filter button
        /// still shows all valid search types for the column's data type.
        /// </para>
        /// </summary>
        /// <example>
        /// <code language="xaml">
        /// &lt;!-- StartsWith for ID columns --&gt;
        /// &lt;DataGridTextColumn Header="Customer ID"
        ///                     Binding="{Binding CustomerId}"
        ///                     local:GridColumn.DefaultSearchMode="StartsWith"/&gt;
        ///
        /// &lt;!-- Equals for exact code matching --&gt;
        /// &lt;DataGridTextColumn Header="Status Code"
        ///                     Binding="{Binding StatusCode}"
        ///                     local:GridColumn.DefaultSearchMode="Equals"/&gt;
        ///
        /// &lt;!-- EndsWith for file extensions --&gt;
        /// &lt;DataGridTextColumn Header="File Name"
        ///                     Binding="{Binding FileName}"
        ///                     local:GridColumn.DefaultSearchMode="EndsWith"/&gt;
        /// </code>
        /// </example>
        public static readonly DependencyProperty DefaultSearchModeProperty =
            DependencyProperty.RegisterAttached(
                "DefaultSearchMode",
                typeof(DefaultSearchMode),
                typeof(GridColumn),
                new FrameworkPropertyMetadata(DefaultSearchMode.Contains));

        /// <summary>
        /// Gets the default search mode for the specified column.
        /// Returns the default search mode to use when creating temporary search templates from textbox input.
        /// </summary>
        /// <param name="element">The column to query</param>
        /// <returns>The default search mode for simple textbox searches</returns>
        public static DefaultSearchMode GetDefaultSearchMode(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return (DefaultSearchMode)element.GetValue(DefaultSearchModeProperty);
        }

        /// <summary>
        /// Sets the default search mode for the specified column.
        /// Specifies which search mode to use when users type in the simple search textbox.
        /// </summary>
        /// <param name="element">The column to configure</param>
        /// <param name="value">The search mode to use for simple textbox searches</param>
        public static void SetDefaultSearchMode(DependencyObject element, DefaultSearchMode value)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.SetValue(DefaultSearchModeProperty, value);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the effective display name for a column, using fallback logic.
        ///
        /// Resolution priority:
        /// 1. Explicit ColumnDisplayName attached property (if set and non-empty)
        /// 2. Extracted text from column Header (handles templates, FrameworkElements, and strings)
        ///
        /// This is the recommended method for retrieving column display names across all components.
        /// </summary>
        /// <param name="column">The column to get the display name for</param>
        /// <returns>The effective display name, or null if column is null</returns>
        /// <example>
        /// <code>
        /// string displayName = GridColumn.GetEffectiveColumnDisplayName(column);
        /// </code>
        /// </example>
        public static string GetEffectiveColumnDisplayName(DataGridColumn column)
        {
            if (column == null)
                return null;

            // Check for explicit ColumnDisplayName
            string explicitName = GetColumnDisplayName(column);
            if (!string.IsNullOrEmpty(explicitName))
                return explicitName;

            // Extract from Header using existing helper
            return SearchDataGrid.ExtractColumnHeaderText(column);
        }

        #endregion
    }
}
