using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WWControls.Wpf.Controls.Primitives;

namespace WWControls.SampleApp.Editors.Views.Samples.Trees
{
    /// <summary>
    /// Backs the WWTreeView playground: a small project/file tree bound to the control. Toggling
    /// AllowDragDrop enables reparenting a node onto another; the DropCommand's CanExecute rejects
    /// no-op and cycle-forming moves (which is also what drives the drag cursor while hovering). The
    /// structural top-level nodes opt out of being dragged via <see cref="IWWTreeViewDragItem"/>.
    /// </summary>
    public partial class TreeViewSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private SampleTreeNode? _selected;

        [ObservableProperty]
        private bool _allowDragDrop = true;

        [ObservableProperty]
        private TreeSelectionMode _selectionMode = TreeSelectionMode.Extended;

        [ObservableProperty]
        private string _selectionSummary = "(none)";

        [ObservableProperty]
        private TreeSearchMode _searchMode = TreeSearchMode.Highlight;

        [ObservableProperty]
        private bool _filterKeepsAncestors = true;

        [ObservableProperty]
        private ExpandCollapseButtonVisibility _expandCollapseButtonMode = ExpandCollapseButtonVisibility.HasGrandchildren;

        [ObservableProperty]
        private double _indentation = 16;

        [ObservableProperty]
        private bool _showConnectorLines = true;

        [ObservableProperty]
        private bool _rowFullWidthHover;

        [ObservableProperty]
        private bool _showEmptyState;

        // Holds the roots while the "show empty state" toggle empties the tree, so toggling back restores them.
        private readonly List<SampleTreeNode> _stashedRoots = new();

        public ObservableCollection<SampleTreeNode> Roots { get; }

        /// <summary>The selection modes offered by the mode picker.</summary>
        public Array SelectionModes { get; } = Enum.GetValues(typeof(TreeSelectionMode));

        /// <summary>The search modes offered by the mode picker.</summary>
        public Array SearchModes { get; } = Enum.GetValues(typeof(TreeSearchMode));

        /// <summary>When per-item expand-all / collapse-all buttons appear, offered by the mode picker.</summary>
        public Array ExpandCollapseButtonModes { get; } = Enum.GetValues(typeof(ExpandCollapseButtonVisibility));

        /// <summary>Bound to <c>WWTreeView.OnDropCommand</c>; the payload is <c>(dropTarget, draggedItem)</c>.</summary>
        public IRelayCommand<Tuple<object, object>> DropCommand { get; }

        /// <summary>Bound to <c>WWTreeView.SelectionChangedCommand</c>; receives the live selection list.</summary>
        public IRelayCommand<IList> SelectionChangedCommand { get; }

        public TreeViewSampleViewModel()
        {
            Roots = new ObservableCollection<SampleTreeNode>(BuildTree());
            DropCommand = new RelayCommand<Tuple<object, object>>(Drop, CanDrop);
            SelectionChangedCommand = new RelayCommand<IList>(OnSelectionChanged);
        }

        // Empties the tree (stashing the roots) to show the EmptyContent placeholder, and restores on toggle-off.
        partial void OnShowEmptyStateChanged(bool value)
        {
            if (value)
            {
                _stashedRoots.Clear();
                _stashedRoots.AddRange(Roots);
                Roots.Clear();
            }
            else
            {
                foreach (var node in _stashedRoots)
                    Roots.Add(node);
                _stashedRoots.Clear();
            }
        }

        private void OnSelectionChanged(IList? items)
        {
            SelectionSummary = items == null || items.Count == 0
                ? "(none)"
                : string.Join(", ", items.Cast<SampleTreeNode>().Select(n => n.Name));
        }

        private bool CanDrop(Tuple<object, object>? move)
        {
            if (move?.Item1 is not SampleTreeNode target || move.Item2 is not SampleTreeNode dragged)
                return false;

            // Reject dropping a node onto itself, onto its current parent (a no-op), or into its own subtree.
            return !ReferenceEquals(target, dragged)
                   && !ReferenceEquals(dragged.Parent, target)
                   && !IsSelfOrDescendant(dragged, target);
        }

        private void Drop(Tuple<object, object>? move)
        {
            if (!CanDrop(move))
                return;

            var target = (SampleTreeNode)move!.Item1;
            var dragged = (SampleTreeNode)move.Item2;

            if (dragged.Parent is { } parent)
                parent.Children.Remove(dragged);
            else
                Roots.Remove(dragged);

            dragged.Parent = target;
            target.Children.Add(dragged);
        }

        private static bool IsSelfOrDescendant(SampleTreeNode node, SampleTreeNode candidate)
        {
            for (SampleTreeNode? current = candidate; current != null; current = current.Parent)
            {
                if (ReferenceEquals(current, node))
                    return true;
            }
            return false;
        }

        private static SampleTreeNode[] BuildTree()
        {
            // Local builder: wires each child's Parent as it's nested so drag-drop can re-home nodes.
            SampleTreeNode N(string name, bool canDrag, params SampleTreeNode[] children)
            {
                var node = new SampleTreeNode(name) { CanDrag = canDrag };
                foreach (var child in children)
                {
                    child.Parent = node;
                    node.Children.Add(child);
                }
                return node;
            }

            // A lazy branch: children are fetched (with a simulated delay) on first expand.
            var lazyRoot = new SampleTreeNode("Lazy load demo (expand me)") { CanDrag = false };
            lazyRoot.SetLoader(async () =>
            {
                await Task.Delay(700);
                return (IEnumerable<SampleTreeNode>)new[]
                {
                    new SampleTreeNode("Fetched item A"),
                    new SampleTreeNode("Fetched item B"),
                    new SampleTreeNode("Fetched item C"),
                };
            });

            return new[]
            {
                N("WWControls.Wpf.Controls.Primitives", false,
                    N("Controls", true, N("WWButton.cs", true), N("WWTreeView.cs", true), N("Icon.cs", true)),
                    N("Themes", true, N("PrimitiveThemeKeys.cs", true), N("Generic.xaml", true))),
                N("WWControls.Wpf.Controls.Editors", false,
                    N("Controls", true, N("WWTextBox.cs", true), N("WWComboBox.cs", true), N("WWDatePicker.cs", true))),
                N("Documentation", false,
                    N("Primitives", true, N("WWButton.md", true), N("WWTreeView.md", true))),
                lazyRoot,
            };
        }
    }

    /// <summary>
    /// A project/file node for the WWTreeView sample. Derives from <see cref="WWTreeNodeBase{T}"/> for the
    /// children/expansion/selection/filter plumbing, and implements <see cref="IWWTreeViewDragItem"/> so the
    /// structural top-level nodes can opt out of being dragged (<see cref="CanDrag"/> = false).
    /// </summary>
    public sealed class SampleTreeNode : WWTreeNodeBase<SampleTreeNode>, IWWTreeViewDragItem, IWWLazyTreeNode
    {
        private Func<Task<IEnumerable<SampleTreeNode>>> _loader;

        public SampleTreeNode(string name) => Name = name;

        public string Name { get; }

        public bool CanDrag { get; set; } = true;

        public bool IsLoading { get; set; }

        /// <summary>Has children when there's a pending loader or children are already present.</summary>
        public bool HasChildren => _loader != null || Children.Count > 0;

        /// <summary>Assigns an on-demand children loader, marking this a lazy node.</summary>
        public void SetLoader(Func<Task<IEnumerable<SampleTreeNode>>> loader) => _loader = loader;

        public async Task LoadChildrenAsync()
        {
            if (_loader == null)
                return;

            var loader = _loader;
            _loader = null;
            foreach (var child in await loader())
            {
                child.Parent = this;
                Children.Add(child);
            }
        }

        public override bool MatchesSelf(WWControls.Core.SearchQuery query) => query.Matches(Name);
    }
}
