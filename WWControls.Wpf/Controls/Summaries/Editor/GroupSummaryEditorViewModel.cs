using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Working copy behind the summary editor, in one of three modes (see
    /// <see cref="SummaryEditorMode"/>): the grid's group-header set, one column's totals-cell
    /// set ("Totals for 'X'" — entries may target other columns' fields), or the fixed panel's
    /// own set. Loaded on open, mutated freely (the Items tab toggles entries per target
    /// column and bolds configured targets; the Order and Alignment tab reorders, re-sides,
    /// and formats), and written back atomically by <see cref="Apply"/> on OK. Cancel simply
    /// discards the instance.
    /// </summary>
    public sealed class GroupSummaryEditorViewModel : INotifyPropertyChanged
    {
        private readonly SearchDataGrid _grid;
        private SummaryEditorColumnItem _selectedColumn;
        private SummaryEditorEntry _selectedEntry;
        private SummaryEditorEntry _selectedLeftEntry;
        private SummaryEditorEntry _selectedRightEntry;
        private IReadOnlyList<string> _displayFormatOptions = System.Array.Empty<string>();
        private bool _showRowCount;

        internal GroupSummaryEditorViewModel(SearchDataGrid grid, SummaryEditorMode mode, GridColumn ownerColumn)
        {
            _grid = grid;
            Mode = mode;
            OwnerColumn = ownerColumn;
            Columns = new ObservableCollection<SummaryEditorColumnItem>();
            LeftEntries = new ObservableCollection<SummaryEditorEntry>();
            RightEntries = new ObservableCollection<SummaryEditorEntry>();
            Load();
        }

        #region Surface

        public SummaryEditorMode Mode { get; }

        /// <summary>The column whose totals / footer cell is being edited (column-scoped modes only).</summary>
        public GridColumn OwnerColumn { get; }

        /// <summary>
        /// Column-scoped modes edit one column's vertically-stacked cell set (the totals row cell
        /// or the group footer cell) — no row count, no left/right alignment.
        /// </summary>
        private bool IsColumnScoped => Mode == SummaryEditorMode.ColumnTotals || Mode == SummaryEditorMode.GroupFooterTotals;

        /// <summary>
        /// "Show row count" applies to the group headers (grid row count entry) and the fixed
        /// panel (no-FieldName Count entry) — hidden in the column-scoped modes.
        /// </summary>
        public bool IsRowCountVisible => !IsColumnScoped;

        /// <summary>
        /// Left/right alignment only applies to the horizontal runs (group headers, fixed
        /// panel). A column-scoped cell stacks entries vertically, so those modes show a single
        /// "Order:" list with up/down only.
        /// </summary>
        public bool IsAlignmentVisible => !IsColumnScoped;

        /// <summary>Header over the right-hand (or only) entry list.</summary>
        public string OrderListHeader => IsAlignmentVisible ? "Right side:" : "Order:";

        /// <summary>Second tab caption — alignment only exists for the horizontal runs.</summary>
        public string OrderTabHeader => IsAlignmentVisible ? "Order and Alignment" : "Order";

        /// <summary>Aggregation-target columns offered in the Items tab, in grid order.</summary>
        public ObservableCollection<SummaryEditorColumnItem> Columns { get; }

        public SummaryEditorColumnItem SelectedColumn
        {
            get => _selectedColumn;
            set { if (!ReferenceEquals(_selectedColumn, value)) { _selectedColumn = value; OnPropertyChanged(); } }
        }

        /// <summary>Entries rendered in the left run (inline after group header content / fixed panel left).</summary>
        public ObservableCollection<SummaryEditorEntry> LeftEntries { get; }

        /// <summary>Entries rendered in the right run (right-aligned; the default side).</summary>
        public ObservableCollection<SummaryEditorEntry> RightEntries { get; }

        /// <summary>
        /// Per-side selections. Each ListBox binds its OWN property — a single shared
        /// SelectedItem bound to both lists self-destructs in WPF: assigning an item the other
        /// list doesn't contain makes that list coerce its selection to null and push the null
        /// back through the TwoWay binding, wiping the selection. Selecting on one side clears
        /// the other here instead.
        /// </summary>
        public SummaryEditorEntry SelectedLeftEntry
        {
            get => _selectedLeftEntry;
            set
            {
                if (ReferenceEquals(_selectedLeftEntry, value)) return;
                _selectedLeftEntry = value;
                OnPropertyChanged();
                if (value != null) SelectedRightEntry = null;
                UpdateSelectedEntry();
            }
        }

        public SummaryEditorEntry SelectedRightEntry
        {
            get => _selectedRightEntry;
            set
            {
                if (ReferenceEquals(_selectedRightEntry, value)) return;
                _selectedRightEntry = value;
                OnPropertyChanged();
                if (value != null) SelectedLeftEntry = null;
                UpdateSelectedEntry();
            }
        }

        /// <summary>
        /// The entry the Prefix / Display format / Suffix fields edit — whichever side list
        /// holds the selection.
        /// </summary>
        public SummaryEditorEntry SelectedEntry
        {
            get => _selectedEntry;
            private set
            {
                if (ReferenceEquals(_selectedEntry, value)) return;
                _selectedEntry = value;
                // Options before the entry notification, so the format ComboBox has its
                // ItemsSource in place when its Text rebinds to the new entry.
                RefreshDisplayFormatOptions();
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedEntry));
            }
        }

        private void UpdateSelectedEntry()
        {
            SelectedEntry = _selectedLeftEntry ?? _selectedRightEntry;
            // Selection is a CanExecute input for all four move buttons; the SelectedEntry
            // setter early-returns on a same-reference cross-side move, so refresh here.
            RefreshMoveCommandStates();
        }

        public bool HasSelectedEntry => _selectedEntry != null;

        /// <summary>
        /// Format presets for the selected entry's Display-format combo: the entry's current
        /// custom format first (so an existing value like <c>"Latest: {0:MM/dd/yyyy}"</c> is
        /// visible and re-pickable), then the target column's <c>DisplayStringFormat</c>, then
        /// presets matched to the target's type (date vs numeric). The combo stays editable
        /// for anything else.
        /// </summary>
        public IReadOnlyList<string> DisplayFormatOptions
        {
            get => _displayFormatOptions;
            private set { _displayFormatOptions = value; OnPropertyChanged(); }
        }

        private static readonly string[] NumericFormatPresets = { "#.00", "0.##", "N0", "N2", "C2", "P0" };
        private static readonly string[] DateFormatPresets = { "d", "MM/dd/yyyy", "MMM d, yyyy", "yyyy-MM-dd", "MMMM yyyy" };
        private static readonly string[] FallbackFormatPresets = { "#.00", "N0", "C2", "MM/dd/yyyy" };

        private void RefreshDisplayFormatOptions()
        {
            var entry = _selectedEntry;
            if (entry == null)
            {
                DisplayFormatOptions = System.Array.Empty<string>();
                return;
            }

            var options = new List<string>();
            void Add(string format)
            {
                if (!string.IsNullOrEmpty(format) && !options.Contains(format))
                    options.Add(format);
            }

            Add(entry.DisplayFormat);
            Add(entry.Column?.DisplayStringFormat);

            var type = entry.Column?.FieldType;
            var underlying = type == null ? null : (System.Nullable.GetUnderlyingType(type) ?? type);
            string[] presets =
                entry.IsRowCount ? NumericFormatPresets
                : underlying == typeof(System.DateTime) || underlying == typeof(System.DateTimeOffset) ? DateFormatPresets
                : underlying != null && ReflectionHelper.IsNumericType(underlying) ? NumericFormatPresets
                : FallbackFormatPresets;
            foreach (var preset in presets) Add(preset);

            DisplayFormatOptions = options;
        }

        /// <summary>"Show row count" — the row-count summary entry (no owning column).</summary>
        public bool ShowRowCount
        {
            get => _showRowCount;
            set
            {
                if (_showRowCount == value) return;
                _showRowCount = value;
                if (value)
                {
                    if (FindRowCountEntry() == null)
                    {
                        var entry = new SummaryEditorEntry(null, SummaryItemType.Count);
                        RightEntries.Add(entry);
                        SelectedRightEntry = entry;
                    }
                }
                else
                {
                    var entry = FindRowCountEntry();
                    if (entry != null) RemoveEntry(entry);
                }
                OnPropertyChanged();
            }
        }

        #endregion

        #region Entry membership (Items tab)

        internal bool HasEntry(GridColumn column, SummaryItemType type)
            => FindEntry(column, type) != null;

        /// <summary>True when any configured entry targets <paramref name="column"/> — drives the bold column names.</summary>
        internal bool HasAnyEntryFor(GridColumn column)
            => LeftEntries.Concat(RightEntries).Any(e => ReferenceEquals(e.Column, column));

        internal void SetEntry(GridColumn column, SummaryItemType type, bool present)
        {
            var existing = FindEntry(column, type);
            if (present && existing == null)
            {
                var entry = new SummaryEditorEntry(column, type);
                RightEntries.Add(entry);
                RefreshColumnFlags();
                // Select the just-added entry so the Order tab opens on it with live move
                // buttons (mirrors the first-configured-entry pre-select on load).
                SelectedRightEntry = entry;
            }
            else if (!present && existing != null)
            {
                RemoveEntry(existing);
            }
        }

        private SummaryEditorEntry FindEntry(GridColumn column, SummaryItemType type)
            => LeftEntries.Concat(RightEntries)
                .FirstOrDefault(e => ReferenceEquals(e.Column, column) && e.SummaryType == type);

        private SummaryEditorEntry FindRowCountEntry()
            => LeftEntries.Concat(RightEntries).FirstOrDefault(e => e.IsRowCount);

        private void RemoveEntry(SummaryEditorEntry entry)
        {
            LeftEntries.Remove(entry);
            RightEntries.Remove(entry);
            if (ReferenceEquals(_selectedLeftEntry, entry)) SelectedLeftEntry = null;
            if (ReferenceEquals(_selectedRightEntry, entry)) SelectedRightEntry = null;
            RefreshColumnFlags();
            // Even when the removed entry wasn't selected, the survivors' indices shifted.
            RefreshMoveCommandStates();
        }

        private void RefreshColumnFlags()
        {
            foreach (var item in Columns)
                item.NotifyConfigurationChanged();
        }

        #endregion

        #region Move commands (Order and Alignment tab)

        private RelayCommand _moveUpCommand;
        public ICommand MoveUpCommand => _moveUpCommand ??= new RelayCommand(
            _ => MoveWithinList(-1),
            _ => CanMoveWithinList(-1));

        private RelayCommand _moveDownCommand;
        public ICommand MoveDownCommand => _moveDownCommand ??= new RelayCommand(
            _ => MoveWithinList(+1),
            _ => CanMoveWithinList(+1));

        private RelayCommand _moveToLeftCommand;
        public ICommand MoveToLeftCommand => _moveToLeftCommand ??= new RelayCommand(
            _ => MoveAcross(SummaryItemAlignment.Left),
            _ => IsAlignmentVisible && SelectedEntry != null && RightEntries.Contains(SelectedEntry));

        private RelayCommand _moveToRightCommand;
        public ICommand MoveToRightCommand => _moveToRightCommand ??= new RelayCommand(
            _ => MoveAcross(SummaryItemAlignment.Right),
            _ => IsAlignmentVisible && SelectedEntry != null && LeftEntries.Contains(SelectedEntry));

        private RelayCommand _editTextStylingCommand;

        /// <summary>
        /// Opens the per-segment text-styling sub-dialog for the selected entry. The command
        /// parameter is the editor element, used to resolve the owner window for the modal.
        /// </summary>
        public ICommand EditTextStylingCommand => _editTextStylingCommand ??= new RelayCommand(
            param =>
            {
                if (SelectedEntry != null)
                    SummaryTextStyleEditor.Show(SelectedEntry, param as System.Windows.FrameworkElement);
            },
            _ => HasSelectedEntry);

        /// <summary>
        /// Re-evaluates the four move buttons. RelayCommand's CanExecuteChanged rides the
        /// library's explicit requery hub, not WPF's input-driven one, so every mutation that
        /// changes selection, list membership, or order must raise it by hand or the buttons
        /// freeze on their first evaluation.
        /// </summary>
        private void RefreshMoveCommandStates()
        {
            _moveUpCommand?.RaiseCanExecuteChanged();
            _moveDownCommand?.RaiseCanExecuteChanged();
            _moveToLeftCommand?.RaiseCanExecuteChanged();
            _moveToRightCommand?.RaiseCanExecuteChanged();
            _editTextStylingCommand?.RaiseCanExecuteChanged();
        }

        private ObservableCollection<SummaryEditorEntry> ListOf(SummaryEditorEntry entry)
            => entry == null ? null
                : LeftEntries.Contains(entry) ? LeftEntries
                : RightEntries.Contains(entry) ? RightEntries
                : null;

        private bool CanMoveWithinList(int delta)
        {
            var list = ListOf(SelectedEntry);
            if (list == null) return false;
            int index = list.IndexOf(SelectedEntry);
            int target = index + delta;
            return target >= 0 && target < list.Count;
        }

        private void MoveWithinList(int delta)
        {
            var list = ListOf(SelectedEntry);
            if (list == null) return;
            int index = list.IndexOf(SelectedEntry);
            int target = index + delta;
            if (target < 0 || target >= list.Count) return;
            list.Move(index, target);
            // Selection didn't change but the entry's position did — re-arm the buttons
            // (e.g. ▼ disables and ▲ enables when the entry lands at the bottom).
            RefreshMoveCommandStates();
        }

        private void MoveAcross(SummaryItemAlignment side)
        {
            var entry = SelectedEntry;
            if (entry == null) return;
            var from = side == SummaryItemAlignment.Left ? RightEntries : LeftEntries;
            var to = side == SummaryItemAlignment.Left ? LeftEntries : RightEntries;
            if (!from.Remove(entry)) return;
            entry.Alignment = side;
            to.Add(entry);
            // Re-select on the destination side (removal pushed null through the source
            // list's selection binding).
            if (side == SummaryItemAlignment.Left)
                SelectedLeftEntry = entry;
            else
                SelectedRightEntry = entry;
            // The entry kept its SelectedEntry identity but swapped lists — ◀▶ (and the
            // position-dependent ▲▼) must re-evaluate against the destination side.
            RefreshMoveCommandStates();
        }

        #endregion

        #region Load / Apply

        private void Load()
        {
            // Sortable staging set in engine run order.
            var staged = new List<(int order, int itemIndex, SummaryEditorEntry entry)>();

            foreach (var descriptor in _grid.GridColumns)
            {
                if (descriptor is GridColumn column)
                    Columns.Add(new SummaryEditorColumnItem(this, column));
            }

            switch (Mode)
            {
                case SummaryEditorMode.GroupHeaders:
                    if (_grid.ShowGroupRowCount)
                    {
                        _showRowCount = true;
                        staged.Add(CreateRowCountStaged(_grid.GroupRowCountSummary));
                    }
                    LoadTargetedItems(_grid.GroupSummaries, staged);
                    break;

                case SummaryEditorMode.FixedTotals:
                    LoadTargetedItems(_grid.FixedTotalSummaries, staged);
                    break;

                case SummaryEditorMode.ColumnTotals:
                    LoadColumnScopedItems(OwnerColumn?.TotalSummaries, staged);
                    break;

                case SummaryEditorMode.GroupFooterTotals:
                    LoadColumnScopedItems(OwnerColumn?.GroupFooterSummaries, staged);
                    break;
            }

            foreach (var (_, _, entry) in staged.OrderBy(s => s.order).ThenBy(s => s.itemIndex))
            {
                // ColumnTotals mode has no sides — everything lands in the single ordered list
                // (RightEntries backs it; the alignment value is unused by totals cells).
                if (!IsAlignmentVisible)
                    RightEntries.Add(entry);
                else
                    (entry.Alignment == SummaryItemAlignment.Left ? LeftEntries : RightEntries).Add(entry);
            }

            RefreshColumnFlags();
            SelectedColumn = Columns.FirstOrDefault();

            // Pre-select the first configured entry so the Order and Alignment tab opens with
            // live fields instead of a disabled editing strip.
            if (LeftEntries.Count > 0)
                SelectedLeftEntry = LeftEntries[0];
            else if (RightEntries.Count > 0)
                SelectedRightEntry = RightEntries[0];
        }

        /// <summary>
        /// Loads a column-scoped cell collection (one column's totals cell or group-footer cell):
        /// each item resolves to its aggregation target column (own column when no FieldName, the
        /// foreign target otherwise, falling back to the owner). Everything lands in the single
        /// ordered list — these modes have no left/right sides.
        /// </summary>
        private void LoadColumnScopedItems(
            System.Windows.FreezableCollection<SummaryItem> items,
            List<(int order, int itemIndex, SummaryEditorEntry entry)> staged)
        {
            if (items is not { Count: > 0 }) return;

            int itemIndex = -1;
            foreach (var item in items)
            {
                itemIndex++;
                if (item == null) continue;
                var target = string.IsNullOrEmpty(item.FieldName)
                    ? OwnerColumn
                    : _grid.FindDescriptorByFieldPath(item.FieldName) ?? OwnerColumn;
                staged.Add((item.OrderIndex, itemIndex, CreateEntry(target, item)));
            }
        }

        /// <summary>
        /// Loads a grid-level targeted collection (group headers / fixed panel): a no-FieldName
        /// Count item is the row-count entry; targeted items resolve their column (entries with
        /// an unresolvable target are dropped — they could never recompute meaningfully).
        /// </summary>
        private void LoadTargetedItems(
            System.Windows.FreezableCollection<SummaryItem> items,
            List<(int order, int itemIndex, SummaryEditorEntry entry)> staged)
        {
            if (items is not { Count: > 0 }) return;

            int itemIndex = -1;
            foreach (var item in items)
            {
                itemIndex++;
                if (item == null) continue;

                if (string.IsNullOrEmpty(item.FieldName))
                {
                    if (item.SummaryType != SummaryItemType.Count) continue; // value aggregate with no target — dead config
                    _showRowCount = true;
                    staged.Add(CreateRowCountStaged(item));
                    continue;
                }

                var target = _grid.FindDescriptorByFieldPath(item.FieldName);
                if (target == null) continue;
                staged.Add((item.OrderIndex, itemIndex, CreateEntry(target, item)));
            }
        }

        private static (int order, int itemIndex, SummaryEditorEntry entry) CreateRowCountStaged(SummaryItem config)
        {
            var entry = new SummaryEditorEntry(null, SummaryItemType.Count)
            {
                Alignment = config?.Alignment ?? SummaryItemAlignment.Right,
                Prefix = config?.Prefix,
                DisplayFormat = config?.DisplayFormat,
                Suffix = config?.Suffix,
                PrefixStyle = config?.PrefixStyle?.Copy() ?? new SummaryTextStyle(),
                ValueStyle = config?.ValueStyle?.Copy() ?? new SummaryTextStyle(),
                SuffixStyle = config?.SuffixStyle?.Copy() ?? new SummaryTextStyle(),
            };
            return (config?.OrderIndex ?? 0, -1, entry);
        }

        private static SummaryEditorEntry CreateEntry(GridColumn column, SummaryItem item)
            => new SummaryEditorEntry(column, item.SummaryType)
            {
                Alignment = item.Alignment,
                Prefix = item.Prefix,
                DisplayFormat = item.DisplayFormat,
                Suffix = item.Suffix,
                PrefixStyle = item.PrefixStyle?.Copy() ?? new SummaryTextStyle(),
                ValueStyle = item.ValueStyle?.Copy() ?? new SummaryTextStyle(),
                SuffixStyle = item.SuffixStyle?.Copy() ?? new SummaryTextStyle(),
            };

        /// <summary>
        /// Writes the working copy back (list position becomes <see cref="SummaryItem.OrderIndex"/>)
        /// to the mode's backing collection. The definition Changed events drive one coalesced
        /// recompute / projection rebuild.
        /// </summary>
        internal void Apply()
        {
            var orderLookup = new Dictionary<SummaryEditorEntry, int>(LeftEntries.Count + RightEntries.Count);
            for (int i = 0; i < LeftEntries.Count; i++) orderLookup[LeftEntries[i]] = i;
            for (int i = 0; i < RightEntries.Count; i++) orderLookup[RightEntries[i]] = i;

            var ordered = LeftEntries.Concat(RightEntries).ToList();

            switch (Mode)
            {
                case SummaryEditorMode.GroupHeaders:
                {
                    var items = _grid.GroupSummaries;
                    if (items != null)
                    {
                        items.Clear();
                        foreach (var entry in ordered.Where(e => !e.IsRowCount))
                            items.Add(entry.ToSummaryItem(orderLookup[entry], entry.Column.ResolveSummaryPath()));
                    }

                    var countEntry = FindRowCountEntry();
                    if (ShowRowCount && countEntry != null)
                    {
                        _grid.GroupRowCountSummary = countEntry.ToSummaryItem(orderLookup[countEntry]);
                        _grid.ShowGroupRowCount = true;
                    }
                    else
                    {
                        _grid.ShowGroupRowCount = false;
                    }
                    break;
                }

                case SummaryEditorMode.FixedTotals:
                {
                    var items = _grid.FixedTotalSummaries;
                    if (items != null)
                    {
                        items.Clear();
                        foreach (var entry in ordered)
                        {
                            // The row-count entry persists as a no-FieldName Count item — the
                            // same shape the panel's Count menu item toggles.
                            items.Add(entry.IsRowCount
                                ? entry.ToSummaryItem(orderLookup[entry])
                                : entry.ToSummaryItem(orderLookup[entry], entry.Column.ResolveSummaryPath()));
                        }
                    }
                    break;
                }

                case SummaryEditorMode.ColumnTotals:
                {
                    var items = OwnerColumn?.TotalSummaries;
                    if (items != null)
                    {
                        int before = items.Count;
                        items.Clear();
                        foreach (var entry in ordered)
                        {
                            // Own-column entries stay plain (no FieldName) so they render bare
                            // (Min=…) and match what the cell's quick picker produces.
                            string fieldName = ReferenceEquals(entry.Column, OwnerColumn)
                                ? null
                                : entry.Column.ResolveSummaryPath();
                            items.Add(entry.ToSummaryItem(orderLookup[entry], fieldName));
                        }

                        // This editor is the only way to add a column total while the totals row
                        // is collapsed — reveal it so the new total isn't written to a hidden
                        // surface. Net increase only: reordering or removing entries leaves an
                        // explicit hide intact (matching the row's no-auto-collapse contract).
                        if (items.Count > before)
                            _grid.EnsureTotalSummaryRowVisible();
                    }
                    break;
                }

                case SummaryEditorMode.GroupFooterTotals:
                {
                    var items = OwnerColumn?.GroupFooterSummaries;
                    if (items != null)
                    {
                        items.Clear();
                        foreach (var entry in ordered)
                        {
                            // Own-column entries stay plain (no FieldName) so they render bare and
                            // match the footer cell's quick picker. The footer rows appear / vanish
                            // automatically as the projection reacts to this definition change — no
                            // explicit reveal needed (unlike the opt-in totals row).
                            string fieldName = ReferenceEquals(entry.Column, OwnerColumn)
                                ? null
                                : entry.Column.ResolveSummaryPath();
                            items.Add(entry.ToSummaryItem(orderLookup[entry], fieldName));
                        }
                    }
                    break;
                }
            }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
