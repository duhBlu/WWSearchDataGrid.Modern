using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    public partial class SearchDataGrid
    {
        #region View-state persistence configuration

        /// <summary>
        /// Gets or sets a stable key that identifies this grid for view-state persistence. Used to
        /// scope the auto-remembered "last view" (and any per-grid defaults) so two grids never
        /// collide. When unset, <see cref="FrameworkElement.Name"/> is used as a fallback; if
        /// neither is set, auto-remember is disabled.
        /// </summary>
        public static readonly DependencyProperty PersistenceIdProperty =
            DependencyProperty.Register(
                nameof(PersistenceId),
                typeof(string),
                typeof(SearchDataGrid),
                new PropertyMetadata(null));

        public string PersistenceId
        {
            get => (string)GetValue(PersistenceIdProperty);
            set => SetValue(PersistenceIdProperty, value);
        }

        /// <summary>
        /// Gets or sets the default folder that the Save/Load-to-file dialogs open to. When
        /// <see cref="AllowUserPresetLocation"/> is <c>false</c>, users are confined to this folder.
        /// When unset, a computed per-user default under <c>%APPDATA%</c> is used (see
        /// <see cref="ResolveEffectivePresetDirectory"/>).
        /// </summary>
        public static readonly DependencyProperty PresetDirectoryProperty =
            DependencyProperty.Register(
                nameof(PresetDirectory),
                typeof(string),
                typeof(SearchDataGrid),
                new PropertyMetadata(null));

        public string PresetDirectory
        {
            get => (string)GetValue(PresetDirectoryProperty);
            set => SetValue(PresetDirectoryProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the user may save/load presets to an arbitrary file location of
        /// their choosing. <c>true</c> (default) presents a free file dialog; <c>false</c> confines
        /// them to <see cref="PresetDirectory"/> (the "restricted" mode).
        /// </summary>
        public static readonly DependencyProperty AllowUserPresetLocationProperty =
            DependencyProperty.Register(
                nameof(AllowUserPresetLocation),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(true));

        public bool AllowUserPresetLocation
        {
            get => (bool)GetValue(AllowUserPresetLocationProperty);
            set => SetValue(AllowUserPresetLocationProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the grid automatically remembers the user's last view (layout +
        /// filters) between sessions and re-applies it on open. Defaults to <c>true</c>. Requires
        /// <see cref="PersistenceId"/> (or <see cref="FrameworkElement.Name"/>) to be set.
        /// </summary>
        public static readonly DependencyProperty RememberViewStateProperty =
            DependencyProperty.Register(
                nameof(RememberViewState),
                typeof(bool),
                typeof(SearchDataGrid),
                new PropertyMetadata(true));

        public bool RememberViewState
        {
            get => (bool)GetValue(RememberViewStateProperty);
            set => SetValue(RememberViewStateProperty, value);
        }

        #endregion

        #region Path resolution

        /// <summary>
        /// The stable key used to scope this grid's persisted state: <see cref="PersistenceId"/> if
        /// set, otherwise <see cref="FrameworkElement.Name"/>, otherwise <c>null</c> (persistence off).
        /// </summary>
        internal string ResolvePersistenceKey()
        {
            if (!string.IsNullOrWhiteSpace(PersistenceId)) return PersistenceId;
            if (!string.IsNullOrWhiteSpace(Name)) return Name;
            return null;
        }

        /// <summary>
        /// Resolves the folder used for preset files and the auto-remembered last view: the
        /// dev-supplied <see cref="PresetDirectory"/> when set, otherwise a per-user default of
        /// <c>%APPDATA%\{Company}\{Product}\GridViewState</c> derived from the entry assembly.
        /// Does not create the directory.
        /// </summary>
        internal string ResolveEffectivePresetDirectory()
        {
            if (!string.IsNullOrWhiteSpace(PresetDirectory)) return PresetDirectory;

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var entry = Assembly.GetEntryAssembly();
            var company = entry?.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
            var product = entry?.GetName().Name;

            if (string.IsNullOrWhiteSpace(company)) company = "WWControls";
            if (string.IsNullOrWhiteSpace(product)) product = "App";

            return Path.Combine(appData, company, product, "GridViewState");
        }

        #endregion

        #region Capture

        /// <summary>
        /// Captures the grid's current layout — column order, widths, visibility, pinning, sorting,
        /// and grouping — as a serializable <see cref="GridViewState"/>. Filters are captured in a
        /// later phase; today this returns a layout-only view.
        /// </summary>
        public GridViewState CaptureViewState()
        {
            return new GridViewState { Layout = CaptureLayout() };
        }

        private GridLayoutState CaptureLayout()
        {
            var descriptors = GridColumns;
            if (descriptors == null) return null;

            var layout = new GridLayoutState { IsGroupPanelVisible = IsGroupPanelVisible };

            foreach (var d in descriptors)
            {
                if (d == null) continue;
                var fieldName = ResolveFieldKey(d);
                if (string.IsNullOrEmpty(fieldName)) continue;

                var cl = new GridColumnLayout
                {
                    FieldName = fieldName,
                    Visible = d.Visible,
                    Fixed = d.Fixed.ToString(),
                };

                var col = d.InternalColumn;
                if (col != null)
                {
                    cl.DisplayIndex = col.DisplayIndex;
                    if (col.ActualWidth > 0) cl.Width = col.ActualWidth;
                }

                if (d.SortOrder != ColumnSortOrder.None)
                {
                    cl.SortOrder = d.SortOrder.ToString();
                    cl.SortIndex = d.SortIndex;
                }

                layout.Columns.Add(cl);
            }

            // GroupedColumns is already ordered by GroupLevel (outermost first).
            foreach (var g in GroupedColumns ?? System.Linq.Enumerable.Empty<GridColumn>())
            {
                if (g == null) continue;
                var fieldName = ResolveFieldKey(g);
                if (string.IsNullOrEmpty(fieldName)) continue;
                layout.Grouping.Add(new GridGroupLayout
                {
                    FieldName = fieldName,
                    GroupInterval = g.GroupInterval.ToString(),
                    SortDirection = g.DefaultGroupBySortDirection.ToString(),
                });
            }

            return layout;
        }

        /// <summary>The persistence key for a column: <c>FieldName</c>, falling back to <c>FilterMemberPath</c>.</summary>
        private static string ResolveFieldKey(GridColumn d)
            => !string.IsNullOrEmpty(d.FieldName) ? d.FieldName : d.FilterMemberPath;

        #endregion

        #region Apply

        /// <summary>
        /// Applies a previously captured view state. Only the populated sections are applied, so a
        /// layout-only or filters-only view leaves the other aspect of the grid untouched. Columns
        /// that no longer exist are skipped (logged).
        /// </summary>
        public void ApplyViewState(GridViewState state)
        {
            if (state?.Layout != null) ApplyLayout(state.Layout);
            // Filters are applied in a later phase.
        }

        private void ApplyLayout(GridLayoutState layout)
        {
            if (layout == null) return;
            var descriptors = GridColumns;
            if (descriptors == null || descriptors.Count == 0) return;
            if (!_gridColumnsGenerated)
            {
                Debug.WriteLine("ViewState: columns not generated yet; skipping layout apply.");
                return;
            }

            // 1. Visibility / pinning / width, per matched column.
            foreach (var cl in layout.Columns)
            {
                var d = FindDescriptorByFieldName(descriptors, cl.FieldName);
                if (d == null)
                {
                    Debug.WriteLine($"ViewState: no column for field '{cl.FieldName}'; skipping.");
                    continue;
                }
                if (cl.Visible.HasValue) d.Visible = cl.Visible.Value;
                if (TryParseEnum(cl.Fixed, out FixedColumnPosition fixedPos)) d.Fixed = fixedPos;
                if (cl.Width.HasValue && cl.Width.Value > 0) d.Width = new DataGridLength(cl.Width.Value);
            }

            // 2. Column order via the generated DataGridColumn.DisplayIndex.
            ApplyColumnOrder(layout, descriptors);

            // 3. Reconcile pinning bands (also re-sorts DisplayIndex within each band).
            ApplyFixedColumnLayout();

            // 4. Grouping, in saved order (GroupBy appends at the end each time).
            ClearGrouping();
            foreach (var g in layout.Grouping)
            {
                var d = FindDescriptorByFieldName(descriptors, g.FieldName);
                if (d == null) continue;
                if (TryParseEnum(g.GroupInterval, out ColumnGroupInterval interval)) d.GroupInterval = interval;
                if (TryParseEnum(g.SortDirection, out ColumnSortOrder groupSort)) d.DefaultGroupBySortDirection = groupSort;
                GroupBy(d);
            }

            if (layout.IsGroupPanelVisible.HasValue) IsGroupPanelVisible = layout.IsGroupPanelVisible.Value;

            // 5. Sorting last, so ApplyColumnSort routes through the correct grouped/ungrouped path.
            ClearAllSorts();
            var sorted = layout.Columns
                .Where(c => TryParseEnum(c.SortOrder, out ColumnSortOrder so) && so != ColumnSortOrder.None)
                .OrderBy(c => c.SortIndex ?? int.MaxValue)
                .ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                var d = FindDescriptorByFieldName(descriptors, sorted[i].FieldName);
                if (d?.InternalColumn == null) continue;
                if (!TryParseEnum(sorted[i].SortOrder, out ColumnSortOrder so)) continue;
                var dir = so == ColumnSortOrder.Descending ? ListSortDirection.Descending : ListSortDirection.Ascending;
                ApplyColumnSort(d.InternalColumn, dir, multiColumn: i > 0);
            }
        }

        private void ApplyColumnOrder(GridLayoutState layout, FreezableCollection<GridColumn> descriptors)
        {
            int maxIndex = Columns.Count - 1;
            if (maxIndex < 0) return;

            // Assign in ascending target order so WPF's shifting of the other columns converges.
            var targets = layout.Columns
                .Where(c => c.DisplayIndex.HasValue)
                .OrderBy(c => c.DisplayIndex.Value);

            foreach (var cl in targets)
            {
                var col = FindDescriptorByFieldName(descriptors, cl.FieldName)?.InternalColumn;
                if (col == null) continue;
                int idx = cl.DisplayIndex.Value;
                if (idx < 0) idx = 0;
                if (idx > maxIndex) idx = maxIndex;
                try { if (col.DisplayIndex != idx) col.DisplayIndex = idx; }
                catch (Exception ex) { Debug.WriteLine($"ViewState: could not set DisplayIndex for '{cl.FieldName}': {ex.Message}"); }
            }
        }

        private void ClearAllSorts()
        {
            Items?.SortDescriptions?.Clear();
            foreach (var col in Columns)
                col.SortDirection = null;
        }

        private static bool TryParseEnum<TEnum>(string value, out TEnum result) where TEnum : struct
        {
            if (!string.IsNullOrEmpty(value) && Enum.TryParse(value, ignoreCase: true, out result))
                return true;
            result = default;
            return false;
        }

        #endregion

        #region Reset to defaults

        private sealed class DefaultColumnLayout
        {
            public int DisplayIndex;
            public bool Visible;
            public FixedColumnPosition Fixed;
            public DataGridLength Width;
            public int GroupIndex;
        }

        // The layout as first generated from the descriptors ("XAML defaults"). Captured once.
        private Dictionary<string, DefaultColumnLayout> _defaultLayout;
        private bool _defaultGroupPanelVisible;

        /// <summary>
        /// Snapshots the layout as first generated from the descriptors so
        /// <see cref="ResetLayoutToDefaults"/> can restore it. Captured once, on first generation;
        /// later calls are no-ops.
        /// </summary>
        internal void CaptureDefaultLayoutSnapshot()
        {
            if (_defaultLayout != null) return;
            var descriptors = GridColumns;
            if (descriptors == null) return;

            var map = new Dictionary<string, DefaultColumnLayout>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in descriptors)
            {
                if (d == null) continue;
                var key = ResolveFieldKey(d);
                if (string.IsNullOrEmpty(key)) continue;
                map[key] = new DefaultColumnLayout
                {
                    DisplayIndex = d.InternalColumn?.DisplayIndex ?? -1,
                    Visible = d.Visible,
                    Fixed = d.Fixed,
                    Width = d.Width,
                    GroupIndex = d.GroupIndex,
                };
            }
            _defaultLayout = map;
            _defaultGroupPanelVisible = IsGroupPanelVisible;
        }

        /// <summary>
        /// Restores column visibility, width, pinning, order, sorting, and grouping to the values
        /// captured when the grid first generated its columns.
        /// </summary>
        public void ResetLayoutToDefaults()
        {
            var descriptors = GridColumns;
            if (descriptors == null || descriptors.Count == 0) return;
            if (_defaultLayout == null) CaptureDefaultLayoutSnapshot();
            if (_defaultLayout == null) return;

            ClearGrouping();
            ClearAllSorts();

            foreach (var d in descriptors)
            {
                if (d == null) continue;
                var key = ResolveFieldKey(d);
                if (key == null || !_defaultLayout.TryGetValue(key, out var def)) continue;
                d.Visible = def.Visible;
                d.Fixed = def.Fixed;
                d.Width = def.Width;
            }

            ApplyFixedColumnLayout();

            // Restore declared display order.
            int maxIndex = Columns.Count - 1;
            if (maxIndex >= 0)
            {
                var ordered = descriptors
                    .Where(d => d != null && DefaultFor(d) != null)
                    .OrderBy(d => DefaultFor(d).DisplayIndex);
                foreach (var d in ordered)
                {
                    var col = d.InternalColumn;
                    if (col == null) continue;
                    int idx = DefaultFor(d).DisplayIndex;
                    if (idx < 0) continue;
                    if (idx > maxIndex) idx = maxIndex;
                    try { if (col.DisplayIndex != idx) col.DisplayIndex = idx; }
                    catch (Exception ex) { Debug.WriteLine($"ViewState reset: DisplayIndex failed: {ex.Message}"); }
                }
            }

            ApplyFixedColumnLayout();

            // Restore declared grouping order.
            var grouped = descriptors
                .Where(d => d != null && (DefaultFor(d)?.GroupIndex ?? -1) >= 0)
                .OrderBy(d => DefaultFor(d).GroupIndex);
            foreach (var d in grouped)
                GroupBy(d);

            IsGroupPanelVisible = _defaultGroupPanelVisible;
        }

        private DefaultColumnLayout DefaultFor(GridColumn d)
        {
            var key = ResolveFieldKey(d);
            if (key != null && _defaultLayout != null && _defaultLayout.TryGetValue(key, out var def)) return def;
            return null;
        }

        #endregion
    }
}
