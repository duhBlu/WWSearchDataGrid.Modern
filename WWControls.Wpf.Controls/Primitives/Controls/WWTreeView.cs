using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WWControls.Core;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// A <see cref="TreeView"/> that adds two-way single-selection binding (<see cref="SelectedObject"/>),
    /// optional drag-and-drop reordering routed through <see cref="OnDropCommand"/>, expand-on-load,
    /// and expand-all / collapse-all commands. Its containers are <see cref="WWTreeViewItem"/>s, which
    /// draw the connector lines and host the per-item expand/collapse affordances.
    /// </summary>
    public class WWTreeView : TreeView, IDisposable
    {
        #region Private Fields

        private bool _disposed = false;

        private readonly ObservableCollection<object> _selectedItems = new ObservableCollection<object>();
        private object _anchorItem;
        private bool _syncingSelection;

        private DispatcherTimer _searchDebounceTimer;
        private List<IWWFilterableTreeNode> _matches = new List<IWWFilterableTreeNode>();
        private Dictionary<object, object> _parentMap = new Dictionary<object, object>();
        private IWWFilterableTreeNode _currentMatch;
        private RelayCommand _nextMatchCommand;
        private RelayCommand _previousMatchCommand;
        private bool _lastQueryWasEmpty = true;

        private readonly HashSet<object> _lazyLoaded = new HashSet<object>();

        #endregion

        #region Constructors and Finalizers

        static WWTreeView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWTreeView), new FrameworkPropertyMetadata(typeof(WWTreeView)));
        }

        public WWTreeView()
        {
            this.Loaded += OnLoaded;
        }

        ~WWTreeView()
        {
            Dispose(false);
        }

        #endregion

        #region Dependency Properties

        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register(
                nameof(SelectedObject),
                typeof(object),
                typeof(WWTreeView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedObjectPropertyChanged));

        public object SelectedObject
        {
            get => GetValue(SelectedObjectProperty);
            set => SetValue(SelectedObjectProperty, value);
        }

        public static readonly DependencyProperty ExpandOnLoadProperty =
            DependencyProperty.Register(
                nameof(ExpandOnLoad),
                typeof(bool),
                typeof(WWTreeView),
                new PropertyMetadata(false));

        public bool ExpandOnLoad
        {
            get => (bool)GetValue(ExpandOnLoadProperty);
            set => SetValue(ExpandOnLoadProperty, value);
        }

        public static readonly DependencyProperty ActivationTriggerProperty =
            DependencyProperty.Register(
                nameof(ActivationTrigger),
                typeof(TreeItemActivationTrigger),
                typeof(WWTreeView),
                new PropertyMetadata(TreeItemActivationTrigger.DoubleClickOrEnter));

        /// <summary>
        /// The user gestures that activate an item — raising <see cref="ItemActivated"/> and executing
        /// <see cref="ItemActivatedCommand"/> with the item's data context.
        /// </summary>
        public TreeItemActivationTrigger ActivationTrigger
        {
            get => (TreeItemActivationTrigger)GetValue(ActivationTriggerProperty);
            set => SetValue(ActivationTriggerProperty, value);
        }

        public static readonly DependencyProperty DoubleClickExpandsProperty =
            DependencyProperty.Register(
                nameof(DoubleClickExpands),
                typeof(bool),
                typeof(WWTreeView),
                new PropertyMetadata(true));

        /// <summary>
        /// Whether double-clicking an item toggles its expansion. Independent of
        /// <see cref="ActivationTrigger"/>: an item can activate on double-click without expanding, or vice versa.
        /// </summary>
        public bool DoubleClickExpands
        {
            get => (bool)GetValue(DoubleClickExpandsProperty);
            set => SetValue(DoubleClickExpandsProperty, value);
        }

        public static readonly DependencyProperty ItemActivatedCommandProperty =
            DependencyProperty.Register(
                nameof(ItemActivatedCommand),
                typeof(ICommand),
                typeof(WWTreeView),
                new PropertyMetadata(null));

        /// <summary>
        /// Command executed when an item is activated (see <see cref="ActivationTrigger"/>), passed the
        /// activated item's data context.
        /// </summary>
        public ICommand ItemActivatedCommand
        {
            get => (ICommand)GetValue(ItemActivatedCommandProperty);
            set => SetValue(ItemActivatedCommandProperty, value);
        }

        public static readonly RoutedEvent ItemActivatedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(ItemActivated),
                RoutingStrategy.Bubble,
                typeof(RoutedPropertyChangedEventHandler<object>),
                typeof(WWTreeView));

        /// <summary>
        /// Raised when an item is activated (see <see cref="ActivationTrigger"/>). The event's
        /// <see cref="RoutedPropertyChangedEventArgs{T}.NewValue"/> is the activated item's data context.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<object> ItemActivated
        {
            add => AddHandler(ItemActivatedEvent, value);
            remove => RemoveHandler(ItemActivatedEvent, value);
        }

        /// <summary>
        /// Raises <see cref="ItemActivated"/> and executes <see cref="ItemActivatedCommand"/> for the
        /// given item data. Called by <see cref="WWTreeViewItem"/> when an activation gesture occurs.
        /// </summary>
        internal void ActivateItem(object itemData)
        {
            RaiseEvent(new RoutedPropertyChangedEventArgs<object>(null, itemData, ItemActivatedEvent));

            var command = ItemActivatedCommand;
            if (command != null && command.CanExecute(itemData))
            {
                command.Execute(itemData);
            }
        }

        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register(
                nameof(SelectionMode),
                typeof(TreeSelectionMode),
                typeof(WWTreeView),
                new PropertyMetadata(TreeSelectionMode.Single, OnSelectionModePropertyChanged));

        /// <summary>
        /// How the tree handles selecting multiple items. <see cref="TreeSelectionMode.Single"/> (default)
        /// preserves native single-selection behavior; <see cref="TreeSelectionMode.Extended"/> and
        /// <see cref="TreeSelectionMode.Multiple"/> populate <see cref="SelectedItems"/> from mouse gestures.
        /// </summary>
        public TreeSelectionMode SelectionMode
        {
            get => (TreeSelectionMode)GetValue(SelectionModeProperty);
            set => SetValue(SelectionModeProperty, value);
        }

        public static readonly DependencyProperty SelectionChangedCommandProperty =
            DependencyProperty.Register(
                nameof(SelectionChangedCommand),
                typeof(ICommand),
                typeof(WWTreeView),
                new PropertyMetadata(null));

        /// <summary>
        /// Command executed whenever <see cref="SelectedItems"/> changes, passed the live
        /// <see cref="SelectedItems"/> collection.
        /// </summary>
        public ICommand SelectionChangedCommand
        {
            get => (ICommand)GetValue(SelectionChangedCommandProperty);
            set => SetValue(SelectionChangedCommandProperty, value);
        }

        /// <summary>
        /// The selected item data. In <see cref="TreeSelectionMode.Single"/> this mirrors
        /// <see cref="SelectedObject"/> (0 or 1 items); in the multi modes it holds the full selection.
        /// The instance is stable — bind to it or read it; mutate the selection via mouse gestures or
        /// <see cref="SelectItems"/>.
        /// </summary>
        public IList SelectedItems => _selectedItems;

        /// <summary>Raised whenever <see cref="SelectedItems"/> changes.</summary>
        public event EventHandler SelectionChanged;

        private static void OnSelectionModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is WWTreeView tree))
                return;

            // Leaving a multi mode: drop the multi-selection visuals and fall back to the single item.
            if ((TreeSelectionMode)e.NewValue == TreeSelectionMode.Single)
            {
                tree.ClearSelectionSet();
                tree.ApplyMultiSelectionVisuals();
                if (tree.SelectedItem != null)
                    tree._selectedItems.Add(tree.SelectedItem);
                tree.RaiseSelectionChanged();
            }
        }

        /// <summary>
        /// Replaces the current selection with <paramref name="items"/> (multi modes). The last item
        /// becomes the anchor and current item.
        /// </summary>
        public void SelectItems(IEnumerable items)
        {
            if (SelectionMode == TreeSelectionMode.Single)
                return;

            _syncingSelection = true;
            try
            {
                ClearSelectionSet();
                if (items != null)
                {
                    foreach (var item in items)
                        AddToSelection(item);
                }

                _anchorItem = _selectedItems.LastOrDefault();
                ApplyMultiSelectionVisuals();
                CommitNativeSelection(_anchorItem);
            }
            finally
            {
                _syncingSelection = false;
            }

            RaiseSelectionChanged();
        }

        /// <summary>
        /// Applies a mouse click to the multi-selection, honoring Ctrl (toggle) and Shift (range) and
        /// the current <see cref="SelectionMode"/>. Called by <see cref="WWTreeViewItem"/>.
        /// </summary>
        internal void HandleItemClick(WWTreeViewItem item, MouseButtonEventArgs e)
        {
            object data = item.DataContext;
            if (data == null)
                return;

            ModifierKeys mods = Keyboard.Modifiers;
            bool ctrl = (mods & ModifierKeys.Control) == ModifierKeys.Control;
            bool shift = (mods & ModifierKeys.Shift) == ModifierKeys.Shift;

            // The "current" item carries WPF's single native selection (used only as the anchor
            // marker). It is always a member of the selection, or null when nothing is selected.
            object current;

            _syncingSelection = true;
            try
            {
                if (shift && _anchorItem != null)
                {
                    SelectRange(_anchorItem, data);
                    current = data;                 // keep the range origin as the anchor
                }
                else if (ctrl || SelectionMode == TreeSelectionMode.Multiple)
                {
                    bool nowSelected = ToggleItem(data);
                    _anchorItem = nowSelected ? data : _selectedItems.LastOrDefault();
                    current = _anchorItem;
                }
                else
                {
                    ReplaceSelection(data);
                    _anchorItem = data;
                    current = data;
                }

                ApplyMultiSelectionVisuals();
                CommitNativeSelection(current);
            }
            finally
            {
                _syncingSelection = false;
            }

            RaiseSelectionChanged();
            e.Handled = true;
        }

        /// <summary>
        /// Points WPF's single native selection at <paramref name="current"/> (the anchor) and clears it
        /// from every other realized container, then focuses the anchor.
        /// <para>
        /// In the multi modes native <c>IsSelected</c> is only an anchor marker, but the theme lights the
        /// same brush for both <c>IsSelected</c> and <c>IsMultiSelected</c>. Without forcing native
        /// selection onto the anchor, a just-deselected node would keep its native selection (and stay
        /// highlighted) until a later click moved it — the "one click behind" visual. Focusing the anchor
        /// rather than the clicked node also stops WPF from re-selecting a node that was just removed from
        /// the selection when it takes focus.
        /// </para>
        /// </summary>
        private void CommitNativeSelection(object current)
        {
            WWTreeViewItem currentContainer = null;
            foreach (var container in EnumerateRealizedContainers())
            {
                bool isCurrent = current != null && Equals(container.DataContext, current);
                if (isCurrent)
                    currentContainer = container;
                if (container.IsSelected != isCurrent)
                    container.IsSelected = isCurrent;
            }

            SelectedObject = current;
            currentContainer?.Focus();
        }

        internal bool ContainsSelected(object data) => _selectedItems.Contains(data);

        internal bool IsMultiSelectEnabled => SelectionMode != TreeSelectionMode.Single;

        private bool ToggleItem(object data)
        {
            if (_selectedItems.Contains(data))
            {
                RemoveFromSelection(data);
                return false;
            }

            AddToSelection(data);
            return true;
        }

        private void ReplaceSelection(object data)
        {
            ClearSelectionSet();
            AddToSelection(data);
        }

        private void SelectRange(object anchor, object target)
        {
            var order = GetVisibleDataItems();
            int a = order.IndexOf(anchor);
            int b = order.IndexOf(target);
            if (a < 0 || b < 0)
            {
                ReplaceSelection(target);
                return;
            }

            if (a > b)
            {
                int t = a;
                a = b;
                b = t;
            }

            ClearSelectionSet();
            for (int i = a; i <= b; i++)
                AddToSelection(order[i]);
        }

        private void AddToSelection(object data)
        {
            if (!_selectedItems.Contains(data))
            {
                _selectedItems.Add(data);
                SetNodeSelected(data, true);
            }
        }

        private void RemoveFromSelection(object data)
        {
            if (_selectedItems.Remove(data))
                SetNodeSelected(data, false);
        }

        private void ClearSelectionSet()
        {
            foreach (var data in _selectedItems)
                SetNodeSelected(data, false);
            _selectedItems.Clear();
        }

        private static void SetNodeSelected(object data, bool selected)
        {
            if (data is IWWTreeNode node)
                node.IsSelected = selected;
        }

        private void RaiseSelectionChanged()
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);

            var command = SelectionChangedCommand;
            if (command != null && command.CanExecute(SelectedItems))
                command.Execute(SelectedItems);
        }

        private void ApplyMultiSelectionVisuals()
        {
            foreach (var container in EnumerateRealizedContainers())
                container.IsMultiSelected = _selectedItems.Contains(container.DataContext);
        }

        private void SelectContainerOfData(object data)
        {
            var container = FindContainer(data);
            if (container != null)
                container.IsSelected = true;
            else
                SelectedObject = data;
        }

        private WWTreeViewItem FindContainer(object data)
        {
            return EnumerateRealizedContainers().FirstOrDefault(c => Equals(c.DataContext, data));
        }

        private IEnumerable<WWTreeViewItem> EnumerateRealizedContainers()
        {
            foreach (var item in Items)
            {
                if (ItemContainerGenerator.ContainerFromItem(item) is WWTreeViewItem container)
                {
                    yield return container;
                    foreach (var descendant in EnumerateDescendantContainers(container))
                        yield return descendant;
                }
            }
        }

        private static IEnumerable<WWTreeViewItem> EnumerateDescendantContainers(WWTreeViewItem parent)
        {
            foreach (var item in parent.Items)
            {
                if (parent.ItemContainerGenerator.ContainerFromItem(item) is WWTreeViewItem container)
                {
                    yield return container;
                    foreach (var descendant in EnumerateDescendantContainers(container))
                        yield return descendant;
                }
            }
        }

        /// <summary>
        /// The visible (expanded) data items in top-to-bottom order — the sequence Shift-range selection
        /// walks. Uses <see cref="IWWTreeNode"/> when present (virtualization-safe) and realized
        /// containers otherwise.
        /// </summary>
        private List<object> GetVisibleDataItems()
        {
            var result = new List<object>();
            foreach (var item in Items)
                CollectVisibleData(item, ItemContainerGenerator.ContainerFromItem(item) as WWTreeViewItem, result);
            return result;
        }

        private static void CollectVisibleData(object data, WWTreeViewItem container, List<object> result)
        {
            result.Add(data);

            // The container is authoritative for expansion (user clicks set it, not the node flag); the
            // node flag is only a fallback for an unrealized item.
            bool expanded = container != null
                ? container.IsExpanded
                : (data is IWWTreeNode n && n.IsExpanded);
            if (!expanded)
                return;

            IEnumerable children = (data as IWWTreeNode)?.Children;
            if (children != null)
            {
                foreach (var child in children)
                {
                    var childContainer = container?.ItemContainerGenerator.ContainerFromItem(child) as WWTreeViewItem;
                    CollectVisibleData(child, childContainer, result);
                }
            }
            else if (container != null)
            {
                foreach (var child in container.Items)
                {
                    var childContainer = container.ItemContainerGenerator.ContainerFromItem(child) as WWTreeViewItem;
                    CollectVisibleData(child, childContainer, result);
                }
            }
        }

        #region Search / Filter

        public static readonly DependencyProperty FilterTextProperty =
            DependencyProperty.Register(
                nameof(FilterText),
                typeof(string),
                typeof(WWTreeView),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSearchInputChanged));

        /// <summary>The search term. Applied per <see cref="SearchMode"/> after a short debounce.</summary>
        public string FilterText
        {
            get => (string)GetValue(FilterTextProperty);
            set => SetValue(FilterTextProperty, value);
        }

        public static readonly DependencyProperty SearchModeProperty =
            DependencyProperty.Register(
                nameof(SearchMode),
                typeof(TreeSearchMode),
                typeof(WWTreeView),
                new PropertyMetadata(TreeSearchMode.Off, OnSearchInputChanged));

        /// <summary>How <see cref="FilterText"/> is applied: not at all, highlight-and-cycle, or filter-down.</summary>
        public TreeSearchMode SearchMode
        {
            get => (TreeSearchMode)GetValue(SearchModeProperty);
            set => SetValue(SearchModeProperty, value);
        }

        public static readonly DependencyProperty FilterKeepsAncestorsProperty =
            DependencyProperty.Register(
                nameof(FilterKeepsAncestors),
                typeof(bool),
                typeof(WWTreeView),
                new PropertyMetadata(true, OnSearchInputChanged));

        /// <summary>
        /// In <see cref="TreeSearchMode.Filter"/>, whether a matched node keeps its full descendant subtree
        /// visible (context). When false, only matching nodes and the ancestor path to them are shown.
        /// </summary>
        public bool FilterKeepsAncestors
        {
            get => (bool)GetValue(FilterKeepsAncestorsProperty);
            set => SetValue(FilterKeepsAncestorsProperty, value);
        }

        public static readonly DependencyProperty SelectMatchOnNavigateProperty =
            DependencyProperty.Register(
                nameof(SelectMatchOnNavigate),
                typeof(bool),
                typeof(WWTreeView),
                new PropertyMetadata(false));

        /// <summary>
        /// Whether cycling matches also selects the current match. Default false — navigation focuses and
        /// scrolls to the match without changing <see cref="SelectedObject"/> (so selection-driven side
        /// effects don't fire on every step).
        /// </summary>
        public bool SelectMatchOnNavigate
        {
            get => (bool)GetValue(SelectMatchOnNavigateProperty);
            set => SetValue(SelectMatchOnNavigateProperty, value);
        }

        public static readonly DependencyProperty ShowSearchBarProperty =
            DependencyProperty.Register(
                nameof(ShowSearchBar),
                typeof(bool),
                typeof(WWTreeView),
                new PropertyMetadata(false));

        /// <summary>Whether the built-in search bar (search box + match navigation + "n of m") is shown.</summary>
        public bool ShowSearchBar
        {
            get => (bool)GetValue(ShowSearchBarProperty);
            set => SetValue(ShowSearchBarProperty, value);
        }

        public static readonly DependencyProperty SearchDebounceProperty =
            DependencyProperty.Register(
                nameof(SearchDebounce),
                typeof(TimeSpan),
                typeof(WWTreeView),
                new PropertyMetadata(TimeSpan.FromMilliseconds(200)));

        /// <summary>How long typing pauses before the filter pass runs.</summary>
        public TimeSpan SearchDebounce
        {
            get => (TimeSpan)GetValue(SearchDebounceProperty);
            set => SetValue(SearchDebounceProperty, value);
        }

        private static readonly DependencyPropertyKey MatchCountPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(MatchCount), typeof(int), typeof(WWTreeView), new PropertyMetadata(0));
        public static readonly DependencyProperty MatchCountProperty = MatchCountPropertyKey.DependencyProperty;

        /// <summary>The number of matches for the current <see cref="FilterText"/> ("m" in "n of m").</summary>
        public int MatchCount
        {
            get => (int)GetValue(MatchCountProperty);
            private set => SetValue(MatchCountPropertyKey, value);
        }

        private static readonly DependencyPropertyKey CurrentMatchIndexPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(CurrentMatchIndex), typeof(int), typeof(WWTreeView), new PropertyMetadata(-1));
        public static readonly DependencyProperty CurrentMatchIndexProperty = CurrentMatchIndexPropertyKey.DependencyProperty;

        /// <summary>Zero-based index of the current match, or -1 when there are none.</summary>
        public int CurrentMatchIndex
        {
            get => (int)GetValue(CurrentMatchIndexProperty);
            private set => SetValue(CurrentMatchIndexPropertyKey, value);
        }

        private static readonly DependencyPropertyKey HasMatchesPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(HasMatches), typeof(bool), typeof(WWTreeView), new PropertyMetadata(false));
        public static readonly DependencyProperty HasMatchesProperty = HasMatchesPropertyKey.DependencyProperty;

        /// <summary>Whether there is at least one match. Bind match-navigation button enablement to this.</summary>
        public bool HasMatches
        {
            get => (bool)GetValue(HasMatchesProperty);
            private set => SetValue(HasMatchesPropertyKey, value);
        }

        private static readonly DependencyPropertyKey MatchDisplayPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(MatchDisplay), typeof(string), typeof(WWTreeView), new PropertyMetadata("0 of 0"));
        public static readonly DependencyProperty MatchDisplayProperty = MatchDisplayPropertyKey.DependencyProperty;

        /// <summary>The "n of m" match counter text.</summary>
        public string MatchDisplay
        {
            get => (string)GetValue(MatchDisplayProperty);
            private set => SetValue(MatchDisplayPropertyKey, value);
        }

        /// <summary>Focuses the next match, wrapping around. Does not select it unless <see cref="SelectMatchOnNavigate"/>.</summary>
        public ICommand NextMatchCommand => _nextMatchCommand ??= new RelayCommand(_ => MoveMatch(1), _ => HasMatches);

        /// <summary>Focuses the previous match, wrapping around.</summary>
        public ICommand PreviousMatchCommand => _previousMatchCommand ??= new RelayCommand(_ => MoveMatch(-1), _ => HasMatches);

        private static void OnSearchInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as WWTreeView)?.ScheduleFilter();
        }

        private void ScheduleFilter()
        {
            if (_searchDebounceTimer == null)
            {
                _searchDebounceTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher);
                _searchDebounceTimer.Tick += OnSearchDebounceTick;
            }

            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Interval = SearchDebounce;
            _searchDebounceTimer.Start();
        }

        private void OnSearchDebounceTick(object sender, EventArgs e)
        {
            _searchDebounceTimer.Stop();
            RunFilter();
        }

        private void RunFilter()
        {
            var query = SearchQuery.Parse(FilterText);
            var mode = SearchMode;

            bool clearing = query.IsEmpty && !_lastQueryWasEmpty;
            _lastQueryWasEmpty = query.IsEmpty;

            if (mode == TreeSearchMode.Off)
            {
                ResetFilterState();
                return;
            }

            var result = TreeFilterEngine.Run(Items, query, mode, FilterKeepsAncestors);
            _matches = result.Matches;
            _parentMap = result.ParentMap;

            ApplyRootFilter(mode == TreeSearchMode.Filter);

            MatchCount = _matches.Count;
            HasMatches = _matches.Count > 0;
            CurrentMatchIndex = HasMatches ? 0 : -1;
            UpdateMatchDisplay();
            _nextMatchCommand?.RaiseCanExecuteChanged();
            _previousMatchCommand?.RaiseCanExecuteChanged();

            ClearCurrentMatch();
            if (CurrentMatchIndex >= 0)
                FocusCurrentMatch();

            if (clearing)
                CollapseRoots();
        }

        private void ResetFilterState()
        {
            // Reuse the engine with an empty query to reset every node's visibility to true and refresh views.
            TreeFilterEngine.Run(Items, SearchQuery.Empty, TreeSearchMode.Highlight, FilterKeepsAncestors);
            ApplyRootFilter(false);

            _matches = new List<IWWFilterableTreeNode>();
            _parentMap = new Dictionary<object, object>();
            MatchCount = 0;
            HasMatches = false;
            CurrentMatchIndex = -1;
            UpdateMatchDisplay();
            _nextMatchCommand?.RaiseCanExecuteChanged();
            _previousMatchCommand?.RaiseCanExecuteChanged();
            ClearCurrentMatch();
        }

        private void ApplyRootFilter(bool on)
        {
            try
            {
                Items.Filter = on
                    ? (Predicate<object>)(o => !(o is IWWFilterableTreeNode n) || n.IsVisibleInFilter)
                    : null;
            }
            catch (NotSupportedException)
            {
                // The current items view can't filter (e.g. an unusual ItemsSource); filter-down is a no-op.
            }
        }

        private void UpdateMatchDisplay()
        {
            MatchDisplay = $"{(CurrentMatchIndex < 0 ? 0 : CurrentMatchIndex + 1)} of {MatchCount}";
        }

        private void MoveMatch(int direction)
        {
            if (_matches.Count == 0)
                return;

            CurrentMatchIndex = (CurrentMatchIndex + direction + _matches.Count) % _matches.Count;
            UpdateMatchDisplay();
            FocusCurrentMatch();
        }

        internal bool IsCurrentMatchData(object data) => data != null && Equals(_currentMatch, data);

        private void ClearCurrentMatch()
        {
            if (_currentMatch == null)
                return;

            _currentMatch.IsCurrentSearchMatch = false;
            if (FindContainer(_currentMatch) is WWTreeViewItem container)
                container.IsCurrentSearchMatch = false;
            _currentMatch = null;
        }

        /// <summary>
        /// Marks the current match, expands its ancestors, and scrolls it into view — without changing
        /// selection unless <see cref="SelectMatchOnNavigate"/> is set.
        /// </summary>
        private void FocusCurrentMatch()
        {
            ClearCurrentMatch();
            if (CurrentMatchIndex < 0 || CurrentMatchIndex >= _matches.Count)
                return;

            var node = _matches[CurrentMatchIndex];
            _currentMatch = node;
            node.IsCurrentSearchMatch = true;
            ExpandAncestors(node, _parentMap);

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                UpdateLayout();
                if (FindContainer(node) is WWTreeViewItem container)
                {
                    container.IsCurrentSearchMatch = true;
                    CenterContainerInView(container);
                }

                if (SelectMatchOnNavigate)
                    SelectContainerOfData(node);
            }));
        }

        /// <summary>
        /// Scrolls a data item into view, expanding its ancestors first. Does not select it.
        /// </summary>
        public void BringItemIntoView(object item)
        {
            if (item == null)
                return;

            var map = (_parentMap != null && _parentMap.ContainsKey(item)) ? _parentMap : BuildParentMap();
            ExpandAncestors(item, map);

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
            {
                UpdateLayout();
                if (FindContainer(item) is WWTreeViewItem container)
                    CenterContainerInView(container);
            }));
        }

        private static void ExpandAncestors(object item, Dictionary<object, object> parentMap)
        {
            object current = item;
            while (parentMap != null && parentMap.TryGetValue(current, out var parent))
            {
                if (parent is IWWTreeNode node)
                    node.IsExpanded = true;
                current = parent;
            }
        }

        private Dictionary<object, object> BuildParentMap()
        {
            var map = new Dictionary<object, object>();
            foreach (var item in Items)
                BuildParentMap(item, map);
            return map;
        }

        private static void BuildParentMap(object data, Dictionary<object, object> map)
        {
            if (!(data is IWWTreeNode node) || node.Children == null)
                return;

            foreach (var child in node.Children)
            {
                map[child] = data;
                BuildParentMap(child, map);
            }
        }

        private void CollapseRoots()
        {
            foreach (var container in Items.OfType<object>()
                .Select(item => ItemContainerGenerator.ContainerFromItem(item) as WWTreeViewItem)
                .Where(c => c != null))
            {
                container.IsExpanded = false;
            }
        }

        private void CenterContainerInView(FrameworkElement element)
        {
            double elementHeight = default;

            ContentPresenter header = VisualTreeHelperMethods.FindVisualDescendant<ContentPresenter>(element);
            if (header != null && element.DataContext != null && ReferenceEquals(header.DataContext, element.DataContext))
            {
                element = header;
                elementHeight = header.ActualHeight;
            }

            if (double.IsNaN(elementHeight) || elementHeight <= 0)
            {
                element.UpdateLayout();
                elementHeight = element.ActualHeight;
            }

            var scrollViewer = VisualTreeHelperMethods.FindVisualAncestor<ScrollViewer>(element);
            if (scrollViewer == null || double.IsNaN(elementHeight) || elementHeight <= 0)
            {
                element.BringIntoView();
                return;
            }

            GeneralTransform transform = element.TransformToAncestor(scrollViewer);
            Rect bounds = transform.TransformBounds(new Rect(new Point(0, 0), new Size(element.RenderSize.Width, elementHeight)));

            double elementCenterY = bounds.Top + (bounds.Height / 2);
            double viewportCenterY = scrollViewer.ViewportHeight / 2;
            double desiredOffset = scrollViewer.VerticalOffset + (elementCenterY - viewportCenterY);
            desiredOffset = Math.Max(0, Math.Min(desiredOffset, scrollViewer.ExtentHeight - scrollViewer.ViewportHeight));
            scrollViewer.ScrollToVerticalOffset(desiredOffset);
        }

        #endregion

        #region Lazy Loading

        /// <summary>
        /// Loads a lazy node's children on its first expand. Idempotent per node; a failed load is allowed
        /// to retry on the next expand. Called by <see cref="WWTreeViewItem.OnExpanded"/>.
        /// </summary>
        internal async void RequestLazyLoad(WWTreeViewItem item)
        {
            if (!(item.DataContext is IWWLazyTreeNode node) || _lazyLoaded.Contains(node))
                return;

            _lazyLoaded.Add(node);
            node.IsLoading = true;
            item.IsLoading = true;
            try
            {
                await node.LoadChildrenAsync();
            }
            catch
            {
                _lazyLoaded.Remove(node); // allow a retry on the next expand
            }
            finally
            {
                node.IsLoading = false;
                item.IsLoading = false;
                item.UpdateExpanderState();
            }
        }

        #endregion

        #region Layout / Visual

        // Layout/visual knobs set on the tree. Each item's template reads them off the owning tree via
        // a RelativeSource AncestorType={WWTreeView} binding (property-value inheritance does not reliably
        // reach generated TreeViewItem containers), so there is no matching per-item DP.

        private static Brush CreateFrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        private static readonly Brush DefaultConnectorLineBrush = CreateFrozenBrush(0xE0, 0xE0, 0xE0);
        private static readonly Brush DefaultLineHoverBrush = CreateFrozenBrush(0x00, 0x78, 0xD4);
        private static readonly Brush DefaultSelectionBrush = CreateFrozenBrush(0xE5, 0xF1, 0xFB);
        private static readonly Brush DefaultItemHoverBrush = CreateFrozenBrush(0xF0, 0xF0, 0xF0);

        public static readonly DependencyProperty IndentationProperty =
            DependencyProperty.Register(
                nameof(Indentation),
                typeof(double),
                typeof(WWTreeView),
                new PropertyMetadata(16.0));

        /// <summary>
        /// The per-level indent width, in device-independent pixels. Sets each item's expander-column
        /// width; the connector-line offsets derive from it.
        /// </summary>
        public double Indentation
        {
            get => (double)GetValue(IndentationProperty);
            set => SetValue(IndentationProperty, value);
        }

        public static readonly DependencyProperty ShowConnectorLinesProperty =
            DependencyProperty.Register(
                nameof(ShowConnectorLines),
                typeof(bool),
                typeof(WWTreeView),
                new PropertyMetadata(true));

        /// <summary>Whether the parent/child connector lines are drawn.</summary>
        public bool ShowConnectorLines
        {
            get => (bool)GetValue(ShowConnectorLinesProperty);
            set => SetValue(ShowConnectorLinesProperty, value);
        }

        public static readonly DependencyProperty ConnectorLineBrushProperty =
            DependencyProperty.Register(
                nameof(ConnectorLineBrush),
                typeof(Brush),
                typeof(WWTreeView),
                new PropertyMetadata(DefaultConnectorLineBrush));

        /// <summary>The brush for the connector lines.</summary>
        public Brush ConnectorLineBrush
        {
            get => (Brush)GetValue(ConnectorLineBrushProperty);
            set => SetValue(ConnectorLineBrushProperty, value);
        }

        public static readonly DependencyProperty ConnectorLineThicknessProperty =
            DependencyProperty.Register(
                nameof(ConnectorLineThickness),
                typeof(double),
                typeof(WWTreeView),
                new PropertyMetadata(1.0));

        /// <summary>The stroke thickness of the connector lines.</summary>
        public double ConnectorLineThickness
        {
            get => (double)GetValue(ConnectorLineThicknessProperty);
            set => SetValue(ConnectorLineThicknessProperty, value);
        }

        public static readonly DependencyProperty LineHoverBrushProperty =
            DependencyProperty.Register(
                nameof(LineHoverBrush),
                typeof(Brush),
                typeof(WWTreeView),
                new PropertyMetadata(DefaultLineHoverBrush));

        /// <summary>The accent brush a child's vertical line takes while its parent's line is hovered.</summary>
        public Brush LineHoverBrush
        {
            get => (Brush)GetValue(LineHoverBrushProperty);
            set => SetValue(LineHoverBrushProperty, value);
        }

        public static readonly DependencyProperty SelectionBrushProperty =
            DependencyProperty.Register(
                nameof(SelectionBrush),
                typeof(Brush),
                typeof(WWTreeView),
                new PropertyMetadata(DefaultSelectionBrush));

        /// <summary>The background brush of selected (and multi-selected) items.</summary>
        public Brush SelectionBrush
        {
            get => (Brush)GetValue(SelectionBrushProperty);
            set => SetValue(SelectionBrushProperty, value);
        }

        public static readonly DependencyProperty ItemHoverBrushProperty =
            DependencyProperty.Register(
                nameof(ItemHoverBrush),
                typeof(Brush),
                typeof(WWTreeView),
                new PropertyMetadata(DefaultItemHoverBrush));

        /// <summary>The background brush an item takes on hover.</summary>
        public Brush ItemHoverBrush
        {
            get => (Brush)GetValue(ItemHoverBrushProperty);
            set => SetValue(ItemHoverBrushProperty, value);
        }

        public static readonly DependencyProperty RowFullWidthHoverProperty =
            DependencyProperty.Register(
                nameof(RowFullWidthHover),
                typeof(bool),
                typeof(WWTreeView),
                new PropertyMetadata(false));

        /// <summary>
        /// When true, the hover/selection highlight spans the item's full width (including the indent
        /// column); when false (default), it covers only the header content column.
        /// </summary>
        public bool RowFullWidthHover
        {
            get => (bool)GetValue(RowFullWidthHoverProperty);
            set => SetValue(RowFullWidthHoverProperty, value);
        }

        public static readonly DependencyProperty EmptyContentProperty =
            DependencyProperty.Register(
                nameof(EmptyContent),
                typeof(object),
                typeof(WWTreeView),
                new PropertyMetadata(null));

        /// <summary>Content shown centered in the tree when it has no items. Pair with <see cref="EmptyTemplate"/>.</summary>
        public object EmptyContent
        {
            get => GetValue(EmptyContentProperty);
            set => SetValue(EmptyContentProperty, value);
        }

        public static readonly DependencyProperty EmptyTemplateProperty =
            DependencyProperty.Register(
                nameof(EmptyTemplate),
                typeof(DataTemplate),
                typeof(WWTreeView),
                new PropertyMetadata(null));

        /// <summary>Template for <see cref="EmptyContent"/> when the tree is empty.</summary>
        public DataTemplate EmptyTemplate
        {
            get => (DataTemplate)GetValue(EmptyTemplateProperty);
            set => SetValue(EmptyTemplateProperty, value);
        }

        #endregion

        public static readonly DependencyProperty AllowDragDropProperty =
        DependencyProperty.Register(
            nameof(AllowDragDrop),
            typeof(bool),
            typeof(WWTreeView),
            new PropertyMetadata(false));

        public bool AllowDragDrop
        {
            get => (bool)GetValue(AllowDragDropProperty);
            set => SetValue(AllowDragDropProperty, value);
        }

        public static readonly DependencyProperty OnDropCommandProperty =
            DependencyProperty.Register(
                nameof(OnDropCommand),
                typeof(ICommand),
                typeof(WWTreeView),
                new PropertyMetadata(null));

        public ICommand OnDropCommand
        {
            get => (ICommand)GetValue(OnDropCommandProperty);
            set => SetValue(OnDropCommandProperty, value);
        }

        public static readonly DependencyProperty ShowExpandCollapseButtonsProperty =
            DependencyProperty.Register(
                nameof(ShowExpandCollapseButtons),
                typeof(bool),
                typeof(WWTreeView),
                new PropertyMetadata(false));

        /// <summary>
        /// When true, shows expand all / collapse all buttons on tree view items on hover.
        /// </summary>
        public bool ShowExpandCollapseButtons
        {
            get => (bool)GetValue(ShowExpandCollapseButtonsProperty);
            set => SetValue(ShowExpandCollapseButtonsProperty, value);
        }

        public static readonly DependencyProperty ExpandCollapseButtonModeProperty =
            DependencyProperty.Register(
                nameof(ExpandCollapseButtonMode),
                typeof(ExpandCollapseButtonVisibility),
                typeof(WWTreeView),
                new PropertyMetadata(ExpandCollapseButtonVisibility.HasGrandchildren, OnExpandCollapseButtonModePropertyChanged));

        /// <summary>
        /// Gets or sets when expand/collapse all buttons should be shown on tree view items.
        /// </summary>
        public ExpandCollapseButtonVisibility ExpandCollapseButtonMode
        {
            get => (ExpandCollapseButtonVisibility)GetValue(ExpandCollapseButtonModeProperty);
            set => SetValue(ExpandCollapseButtonModeProperty, value);
        }

        // Newly realized items pick the mode up in WWTreeViewItem's Loaded handler; this pushes a
        // runtime change out to the items that are already realized (otherwise the mode would only
        // ever take effect at first load, so it couldn't be changed live).
        private static void OnExpandCollapseButtonModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is WWTreeView tree))
                return;

            var mode = (ExpandCollapseButtonVisibility)e.NewValue;
            foreach (var container in tree.EnumerateRealizedContainers())
            {
                container.ExpandCollapseButtonMode = mode;
                container.UpdateShowExpandCollapseButtons();
            }
        }

        public static readonly DependencyProperty ExpandAllCommandProperty =
            DependencyProperty.Register(
                nameof(ExpandAllCommand),
                typeof(ICommand),
                typeof(WWTreeView),
                new PropertyMetadata(null));

        /// <summary>
        /// Command that expands all nodes in the tree. Bind this to a button's Command property.
        /// </summary>
        public ICommand ExpandAllCommand
        {
            get => (ICommand)GetValue(ExpandAllCommandProperty);
            set => SetValue(ExpandAllCommandProperty, value);
        }

        public static readonly DependencyProperty CollapseAllCommandProperty =
            DependencyProperty.Register(
                nameof(CollapseAllCommand),
                typeof(ICommand),
                typeof(WWTreeView),
                new PropertyMetadata(null));

        /// <summary>
        /// Command that collapses all nodes in the tree. Bind this to a button's Command property.
        /// </summary>
        public ICommand CollapseAllCommand
        {
            get => (ICommand)GetValue(CollapseAllCommandProperty);
            set => SetValue(CollapseAllCommandProperty, value);
        }

        /// <summary>
        /// Expands or collapses all root-level items and their descendants. Data-driven for
        /// <see cref="IWWTreeNode"/> items (so it works even when containers aren't realized yet —
        /// the two-way <c>IsExpanded</c> binding reflects each node flag onto its container as it
        /// materializes); plain items fall back to their realized containers.
        /// </summary>
        public void SetAllExpanded(bool expanded)
        {
            foreach (var item in Items)
            {
                var container = ItemContainerGenerator.ContainerFromItem(item) as WWTreeViewItem;
                WWTreeViewItem.SetExpandedDeep(item, container, expanded);
            }
        }


        #endregion

        #region Event Handlers

        private static void OnSelectedObjectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is WWTreeView treeView) || treeView._syncingSelection)
                return;

            if (e.NewValue == null)
            {
                // WPF TreeView doesn't natively support deselection. When SelectedObject is set to null,
                // deselect the current container so the tree doesn't show a stale highlight.
                if (treeView.SelectedItem != null && treeView.FindContainer(treeView.SelectedItem) is WWTreeViewItem container)
                {
                    container.IsSelected = false;
                }
            }
            else if (!Equals(treeView.SelectedItem, e.NewValue))
            {
                // Programmatic selection: select the realized container for the bound item.
                treeView.SelectContainerOfData(e.NewValue);
            }
        }

        /// <summary>
        /// Mirrors the native <see cref="TreeView.SelectedItem"/> into the two-way
        /// <see cref="SelectedObject"/>. Using the override rather than a Loaded-time event subscription
        /// keeps the tracking free of subscription lifecycle. In <see cref="TreeSelectionMode.Single"/>
        /// it also keeps <see cref="SelectedItems"/> in sync.
        /// </summary>
        protected override void OnSelectedItemChanged(RoutedPropertyChangedEventArgs<object> e)
        {
            base.OnSelectedItemChanged(e);
            SelectedObject = e.NewValue;

            if (SelectionMode == TreeSelectionMode.Single)
            {
                _selectedItems.Clear();
                if (e.NewValue != null)
                    _selectedItems.Add(e.NewValue);
                RaiseSelectionChanged();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (ExpandAllCommand == null)
                ExpandAllCommand = new RelayCommand(_ => SetAllExpanded(true));

            if (CollapseAllCommand == null)
                CollapseAllCommand = new RelayCommand(_ => SetAllExpanded(false));
        }

        #endregion

        #region Overrides

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new WWTreeViewItem();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is WWTreeViewItem;
        }

        /// <summary>
        /// Reflects the current multi-selection onto a container as it is (re)realized, so virtualized
        /// items scrolled back into view show the correct selection state.
        /// </summary>
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            if (element is WWTreeViewItem tvi)
            {
                tvi.Level = 0;
                tvi.IsMultiSelected = IsMultiSelectEnabled && _selectedItems.Contains(item);
                tvi.IsCurrentSearchMatch = IsCurrentMatchData(item);
            }
        }

        /// <summary>
        /// Called by WPF for EVERY container being removed — including virtualized (off-screen) ones.
        /// This is the reliable cleanup hook that ContainerFromIndex misses.
        /// </summary>
        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            if (element is WWTreeViewItem tvi)
                tvi.Dispose();

            base.ClearContainerForItemOverride(element, item);
        }

        #endregion

        #region IDisposable Implementation

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    this.Loaded -= OnLoaded;

                    // Disable container recycling to ensure ClearContainerForItemOverride
                    // is called for all containers (recycled containers are otherwise pooled).
                    VirtualizingPanel.SetVirtualizationMode(this, VirtualizationMode.Standard);

                    // Sever ItemsSource completely — ClearValue handles both bound and
                    // programmatic cases and automatically empties the Items collection.
                    ClearValue(ItemsSourceProperty);
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
