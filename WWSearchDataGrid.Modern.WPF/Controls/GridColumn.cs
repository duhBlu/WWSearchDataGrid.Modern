using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Event arguments for the ColumnDisplayNameChanged event.
    /// </summary>
    public class ColumnDisplayNameChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the column whose display name changed.
        /// </summary>
        public DataGridColumn Column { get; }

        /// <summary>
        /// Gets the old display name value.
        /// </summary>
        public string OldValue { get; }

        /// <summary>
        /// Gets the new display name value.
        /// </summary>
        public string NewValue { get; }

        /// <summary>
        /// Initializes a new instance of the ColumnDisplayNameChangedEventArgs class.
        /// </summary>
        public ColumnDisplayNameChangedEventArgs(DataGridColumn column, string oldValue, string newValue)
        {
            Column = column;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// Specifies the default search mode for simple textbox searches in column filters.
    /// This enum provides a safe subset of SearchType values appropriate for temporary search templates.
    /// </summary>
    public enum DefaultSearchMode
    {
        /// <summary>
        /// Finds matches anywhere in the value (default behavior).
        /// Best for general text search scenarios.
        /// </summary>
        Contains = 0,

        /// <summary>
        /// Finds matches that start with the search text.
        /// Best for ID columns, part numbers, or customer codes where users know the prefix.
        /// </summary>
        StartsWith = 1,

        /// <summary>
        /// Finds matches that end with the search text.
        /// Best for file extensions, domain suffixes, or similar patterns.
        /// </summary>
        EndsWith = 2,

        /// <summary>
        /// Finds exact matches only.
        /// Best for status codes, enum values, or scenarios requiring exact matches.
        /// </summary>
        Equals = 3
    }

    /// <summary>
    /// Defines the scope of items affected by the select-all checkbox operation.
    /// </summary>
    public enum SelectAllScope
    {
        /// <summary>
        /// Affects only the currently filtered/visible rows in the data grid.
        /// This is the default behavior and respects any active column filters.
        /// </summary>
        FilteredRows = 0,

        /// <summary>
        /// Affects only the currently selected rows (or rows containing selected cells when SelectionUnit is Cell).
        /// When this scope is active, the select-all checkbox will show the count of affected rows.
        /// </summary>
        SelectedRows = 1,

        /// <summary>
        /// Affects all items in the ItemsSource regardless of filtering or selection state.
        /// This operates on the complete dataset, bypassing any active filters.
        /// </summary>
        AllItems = 2
    }

    /// <summary>
    /// Provides attached properties for configuring data grid columns with search and filtering capabilities.
    /// These properties work with all DataGridColumn types including TextColumn, CheckBoxColumn, TemplateColumn, and ComboBoxColumn.
    /// </summary>
    public static class GridColumn
    {
        #region EnableRuleFiltering Attached Property

        /// <summary>
        /// Identifies the EnableRuleFiltering attached property.
        /// Enables or disables complex filtering UI for a column.
        /// Default value: true
        /// </summary>
        public static readonly DependencyProperty EnableRuleFilteringProperty =
            DependencyProperty.RegisterAttached(
                "EnableRuleFiltering",
                typeof(bool),
                typeof(GridColumn),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits, OnEnableRuleFilteringChanged));

        /// <summary>
        /// Gets the value indicating whether complex filtering is enabled for the specified column.
        /// </summary>
        /// <param name="element">The column to query</param>
        /// <returns>True if complex filtering is enabled; otherwise, false</returns>
        public static bool GetEnableRuleFiltering(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return (bool)element.GetValue(EnableRuleFilteringProperty);
        }

        /// <summary>
        /// Sets whether complex filtering is enabled for the specified column.
        /// </summary>
        /// <param name="element">The column to configure</param>
        /// <param name="value">True to enable complex filtering; false to disable</param>
        public static void SetEnableRuleFiltering(DependencyObject element, bool value)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.SetValue(EnableRuleFilteringProperty, value);
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

        #region UseCheckBoxInSearchBox Attached Property

        /// <summary>
        /// Identifies the <c>UseCheckBoxInSearchBox</c> attached property.  
        /// Explicitly enables checkbox filtering mode within a column's search box,  
        /// overriding the default auto-detection logic.  
        /// </summary>
        /// <remarks>
        /// <para>
        /// When enabled, the search box displays a checkbox selector instead of a text box,  
        /// allowing users to filter boolean or flag-based values more intuitively.  
        /// This property can be applied to any column type, even when automatic type detection  
        /// would not normally enable checkbox mode.
        /// </para>
        ///
        /// <para><b>Default value:</b> <c>false</c></para>
        /// </remarks>

        public static readonly DependencyProperty UseCheckBoxInSearchBoxProperty =
            DependencyProperty.RegisterAttached(
                "UseCheckBoxInSearchBox",
                typeof(bool),
                typeof(GridColumn),
                new PropertyMetadata(false, OnUseCheckBoxInSearchBoxChanged));

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
        /// Identifies the <c>FilterMemberPath</c> attached property.  
        /// Specifies the property path used to retrieve filter values from data objects.  
        /// This path determines which property is used to populate filter dropdown values.
        /// </summary>
        /// <remarks>
        /// <para><b>Resolution priority:</b></para>
        /// <list type="number">
        ///   <item><description><c>FilterMemberPath</c> — if explicitly set.</description></item>
        ///   <item><description><see cref="System.Windows.Controls.DataGridColumn.SortMemberPath"/> — if available.</description></item>
        ///   <item><description>Binding path extracted from <see cref="System.Windows.Controls.DataGridBoundColumn.Binding"/>.</description></item>
        /// </list>
        ///
        /// <para><b>Default value:</b> <c>null</c> (falls back to <see cref="System.Windows.Controls.DataGridColumn.SortMemberPath"/> or the column's binding path).</para>
        /// </remarks>

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
        /// Event raised when the ColumnDisplayName attached property changes on any column.
        /// This allows external controls to subscribe and refresh their UI when column display names change.
        /// </summary>
        public static event EventHandler<ColumnDisplayNameChangedEventArgs> ColumnDisplayNameChanged;

        /// <summary>
        /// Specifies the display name shown in Column Chooser, Filter Panel, and other UI components.
        ///
        /// When not explicitly set, falls back to extracting text from the column's Header property.
        /// The fallback logic handles complex scenarios where Header may be a template or FrameworkElement.
        ///
        /// Default value: null (uses Header as fallback)
        public static readonly DependencyProperty ColumnDisplayNameProperty =
            DependencyProperty.RegisterAttached(
                "ColumnDisplayName",
                typeof(string),
                typeof(GridColumn),
                new FrameworkPropertyMetadata(null, OnColumnDisplayNameChanged));

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
        /// <item><description><see cref="DefaultSearchMode.Contains"/> – Finds matches anywhere in the value (default)</description></item>
        /// <item><description><see cref="DefaultSearchMode.StartsWith"/> – For ID columns, part numbers, or customer codes where users know the prefix</description></item>
        /// <item><description><see cref="DefaultSearchMode.EndsWith"/> – For file extensions, domain suffixes, or similar patterns</description></item>
        /// <item><description><see cref="DefaultSearchMode.Equals"/> – For status codes, enum values, or scenarios requiring exact matches</description></item>        /// </list>
        /// <para>
        /// Note: This only affects simple textbox search behavior. The advanced filter button
        /// still shows all valid search types for the column's data type.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty DefaultSearchModeProperty =
            DependencyProperty.RegisterAttached(
                "DefaultSearchMode",
                typeof(DefaultSearchMode),
                typeof(GridColumn),
                new PropertyMetadata(DefaultSearchMode.Contains));

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

        #region IsSelectAllColumn Attached Property

        /// <summary>
        /// Identifies the IsSelectAllColumn attached property.
        /// Enables a "Select All" checkbox in the column header that toggles boolean values across all visible rows.
        /// <para>
        /// This property only functions correctly when the column's data type is boolean (bool or bool?).
        /// If set on a non-boolean column, it will be automatically disabled.
        /// </para>
        /// <para>
        /// Behavior: The select-all checkbox cycles between true and false values only.
        /// </para>
        /// <para>
        /// The checkbox displays three states:
        /// </para>
        /// <list type="bullet">
        /// <item><description>Checked: All non-null values in visible rows are true</description></item>
        /// <item><description>Unchecked: All non-null values in visible rows are false</description></item>
        /// <item><description>Indeterminate: Mixed state (some true, some false among visible rows)</description></item>
        /// </list>
        /// <para>
        /// When clicked, the checkbox toggles all non-null boolean values to the opposite state:
        /// - If currently all true → sets all to false
        /// - If currently all false → sets all to true
        /// - If mixed (indeterminate) → sets all to true
        /// </para>
        /// <para>
        /// Default value: false
        /// </para>
        /// </summary>
        public static readonly DependencyProperty IsSelectAllColumnProperty =
            DependencyProperty.RegisterAttached(
                "IsSelectAllColumn",
                typeof(bool),
                typeof(GridColumn),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits, OnIsSelectAllColumnChanged));

        /// <summary>
        /// Gets the value indicating whether select-all functionality is enabled for the specified column.
        /// </summary>
        /// <param name="element">The column to query</param>
        /// <returns>True if select-all functionality is enabled; otherwise, false</returns>
        public static bool GetIsSelectAllColumn(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return (bool)element.GetValue(IsSelectAllColumnProperty);
        }

        /// <summary>
        /// Sets whether select-all functionality is enabled for the specified column.
        /// Note: This only works correctly with boolean-typed columns.
        /// </summary>
        /// <param name="element">The column to configure</param>
        /// <param name="value">True to enable select-all functionality; false to disable</param>
        public static void SetIsSelectAllColumn(DependencyObject element, bool value)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.SetValue(IsSelectAllColumnProperty, value);
        }

        #endregion

        #region SelectAllScope Attached Property

        /// <summary>
        /// Identifies the SelectAllScope attached property.
        /// Determines which items are affected when the select-all checkbox is toggled.
        /// <para>
        /// This property works in conjunction with IsSelectAllColumn and defines the scope
        /// of rows that will be affected by the select-all checkbox operation.
        /// </para>
        /// <para>
        /// Available scopes:
        /// </para>
        /// <list type="table">
        /// <item><description>FilteredRows: Affects only currently visible/filtered rows</description></item>
        /// <item><description>SelectedRows: Affects only selected rows (or rows with selected cells). Shows row count in header.</description></item>
        /// <item><description>AllItems (default): Affects all items in ItemsSource regardless of filtering or selection</description></item>
        /// </list>
        /// <para>
        /// Default value: SelectAllScope.AllItems
        /// </para>
        /// <para>
        /// Recommendation: Disable column sorting on these columns. The grid reapplies the sort order/filter after the values change, causing them to 'disappear' from the grid when their values change.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty SelectAllScopeProperty =
            DependencyProperty.RegisterAttached(
                "SelectAllScope",
                typeof(SelectAllScope),
                typeof(GridColumn),
                new FrameworkPropertyMetadata(SelectAllScope.AllItems, FrameworkPropertyMetadataOptions.Inherits, OnSelectAllScopeChanged));

        /// <summary>
        /// Gets the select-all scope for the specified column.
        /// </summary>
        /// <param name="element">The column to query</param>
        /// <returns>The SelectAllScope value determining which items are affected by select-all operations</returns>
        public static SelectAllScope GetSelectAllScope(DependencyObject element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            return (SelectAllScope)element.GetValue(SelectAllScopeProperty);
        }

        /// <summary>
        /// Sets the select-all scope for the specified column.
        /// </summary>
        /// <param name="element">The column to configure</param>
        /// <param name="value">The scope of items to affect with select-all operations</param>
        public static void SetSelectAllScope(DependencyObject element, SelectAllScope value)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            element.SetValue(SelectAllScopeProperty, value);
        }

        #endregion

       #region Helper Methods

        /// <summary>
        /// Handles changes to the ColumnDisplayName attached property
        /// </summary>
        private static void OnColumnDisplayNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGridColumn column)
            {
                // Find the SearchDataGrid that owns this column
                var grid = FindSearchDataGridForColumn(column);
                if (grid != null)
                {
                    column.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        //  Update the filter panel to reflect the new display name
                        grid.UpdateFilterPanel();

                        // Update the ColumnSearchBox's SearchTemplateController.ColumnName
                        var columnSearchBox = grid.DataColumns.FirstOrDefault(c => c.CurrentColumn == column);
                        if (columnSearchBox?.SearchTemplateController != null)
                        {
                            columnSearchBox.SearchTemplateController.ColumnName = GetEffectiveColumnDisplayName(column);
                        }

                        // Force refresh of any ItemsControl (like ListBox) that displays the columns collection
                        // by invalidating the binding on the DataGrid's Columns collection
                        if (grid.Columns != null)
                        {
                            var collectionView = CollectionViewSource.GetDefaultView(grid.Columns);
                            collectionView?.Refresh();
                        }
                    }), System.Windows.Threading.DispatcherPriority.DataBind);
                }

                // Raise the static event to notify external subscribers (optional, for advanced scenarios)
                ColumnDisplayNameChanged?.Invoke(null, new ColumnDisplayNameChangedEventArgs(
                    column,
                    e.OldValue as string,
                    e.NewValue as string));
            }
        }

        /// <summary>
        /// Handles changes to the EnableRuleFiltering attached property
        /// </summary>
        private static void OnEnableRuleFilteringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGridColumn column)
            {
                // Find the SearchDataGrid that owns this column
                var grid = FindSearchDataGridForColumn(column);
                if (grid != null)
                {
                    // Find the ColumnSearchBox associated with this column
                    var columnSearchBox = grid.DataColumns.FirstOrDefault(c => c.CurrentColumn == column);
                    if (columnSearchBox != null)
                    {
                        // Update the IsComplexFilteringEnabled property which will trigger binding updates
                        columnSearchBox.UpdateIsComplexFilteringEnabled();
                    }
                }
            }
        }

        /// <summary>
        /// Handles changes to the IsSelectAllColumn attached property
        /// </summary>
        private static void OnIsSelectAllColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGridColumn column)
            {
                // Find the SearchDataGrid that owns this column
                var grid = FindSearchDataGridForColumn(column);
                if (grid != null)
                {
                    // Trigger a refresh of select-all column headers to show/hide the checkbox
                    grid.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        grid.SetupSelectAllColumnHeaders();
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
        }

        /// <summary>
        /// Handles changes to the SelectAllScope attached property
        /// </summary>
        private static void OnSelectAllScopeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGridColumn column)
            {
                // Find the SearchDataGrid that owns this column
                var grid = FindSearchDataGridForColumn(column);
                if (grid != null)
                {
                    // Trigger a refresh of select-all column headers to update row count visibility
                    grid.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        grid.SetupSelectAllColumnHeaders();
                    }), System.Windows.Threading.DispatcherPriority.Loaded);
                }
            }
        }

        /// <summary>
        /// Handles changes to the UseCheckBoxInSearchBox attached property
        /// </summary>
        private static void OnUseCheckBoxInSearchBoxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGridColumn column)
            {
                // Find the SearchDataGrid that owns this column
                var grid = FindSearchDataGridForColumn(column);
                if (grid != null)
                {
                    // Find the ColumnSearchBox associated with this column
                    var columnSearchBox = grid.DataColumns.FirstOrDefault(c => c.CurrentColumn == column);
                    if (columnSearchBox != null)
                    {
                        // Re-determine the checkbox column type based on the new setting
                        columnSearchBox.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            columnSearchBox.DetermineCheckboxColumnTypeFromColumnDefinition();
                        }), System.Windows.Threading.DispatcherPriority.DataBind);
                    }
                }
            }
        }

        /// <summary>
        /// Finds the SearchDataGrid that owns the specified column using reflection.
        /// DataGridColumn is not part of the visual tree, so we must use reflection to access DataGridOwner.
        /// </summary>
        private static SearchDataGrid FindSearchDataGridForColumn(DataGridColumn column)
        {
            try
            {
                if (column == null)
                    return null;

                // The DataGrid that owns the column is accessed via the internal DataGridOwner property
                // We'll use reflection to access it since it's protected
                var dataGridProperty = typeof(DataGridColumn).GetProperty("DataGridOwner",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (dataGridProperty != null)
                {
                    var dataGrid = dataGridProperty.GetValue(column) as DataGrid;
                    return dataGrid as SearchDataGrid;
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

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

        /// <summary>
        /// Determines if a column is a boolean type based on various detection methods.
        /// This method is used internally by the IsSelectAllColumn functionality to validate
        /// that the column can support select-all checkbox behavior.
        ///
        /// Detection priority:
        /// 1. Checks if column is DataGridCheckBoxColumn
        /// 2. Checks GridColumn.UseCheckBoxInSearchBox explicit property
        /// 3. Checks SearchTemplateController.ColumnDataType (if controller is available)
        /// 4. Uses reflection to examine binding path type
        /// </summary>
        /// <param name="column">The column to check</param>
        /// <param name="grid">The SearchDataGrid that owns the column (optional, for accessing SearchTemplateController)</param>
        /// <returns>True if the column is determined to be boolean type; otherwise, false</returns>
        internal static bool IsColumnBooleanType(DataGridColumn column, SearchDataGrid grid = null)
        {
            if (column == null)
                return false;

            try
            {
                // Method 1: Check if it's a DataGridCheckBoxColumn
                if (column is DataGridCheckBoxColumn)
                    return true;

                // Method 2: Check explicit UseCheckBoxInSearchBox property
                if (GetUseCheckBoxInSearchBox(column))
                    return true;

                // Method 3: Check SearchTemplateController if grid is available
                if (grid != null)
                {
                    var columnSearchBox = grid.DataColumns.FirstOrDefault(c => c.CurrentColumn == column);
                    if (columnSearchBox?.SearchTemplateController != null)
                    {
                        return columnSearchBox.SearchTemplateController.ColumnDataType == ColumnDataType.Boolean;
                    }
                }

                // Method 4: Use reflection to check binding type
                if (column is DataGridBoundColumn boundColumn &&
                    boundColumn.Binding is Binding binding &&
                    !string.IsNullOrEmpty(binding.Path?.Path))
                {
                    // Try to get property type from binding path
                    var propertyType = ReflectionHelper.GetPropertyType(grid?.ItemsSource, binding.Path.Path);
                    if (propertyType != null)
                    {
                        // Check if it's bool or bool?
                        if (propertyType == typeof(bool))
                            return true;
                        if (propertyType == typeof(bool?))
                            return true;
                        if (Nullable.GetUnderlyingType(propertyType) == typeof(bool))
                            return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error determining if column is boolean type: {ex.Message}");
                return false;
            }
        }


        #endregion
    }
}
