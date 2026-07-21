using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WWControls.Wpf.Controls.Primitives;

namespace WWControls.SampleApp.Editors.Views.Samples.Trees
{
    /// <summary>
    /// Backs the "Search &amp; Navigation" sample: the tree's built-in search bar is turned off
    /// (<c>ShowSearchBar=False</c>) and the same engine is driven from a hand-built find bar. The view
    /// binds a text box to <c>FilterText</c>, prev/next buttons to <c>PreviousMatchCommand</c> /
    /// <c>NextMatchCommand</c>, and a counter to <c>MatchDisplay</c> — every member the built-in bar
    /// would consume. This view model only owns the data, the search options, and the selection
    /// read-out; the imperative <c>BringItemIntoView</c> / <c>SelectItems</c> calls live in the
    /// code-behind (they are methods on the control, not commands) and read <see cref="AllNodes"/>
    /// and <see cref="MatchesFor"/> from here.
    /// </summary>
    public partial class TreeSearchSampleViewModel : ObservableObject
    {
        /// <summary>Highlight keeps every node and cycles matches; Filter hides non-matches. Off is omitted — the find bar assumes searching.</summary>
        [ObservableProperty]
        private TreeSearchMode _searchMode = TreeSearchMode.Highlight;

        /// <summary>Cycling matches also selects the current one when true; otherwise it only scrolls / accents it.</summary>
        [ObservableProperty]
        private bool _selectMatchOnNavigate;

        /// <summary>The pause after typing before the filter pass runs, in milliseconds; projected to <see cref="SearchDebounce"/>.</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SearchDebounce))]
        private int _debounceMs = 200;

        /// <summary>The node the "Go to node" picker points at; the Reveal button scrolls it into view.</summary>
        [ObservableProperty]
        private CatalogNode? _jumpTarget;

        [ObservableProperty]
        private string _selectionSummary = "(none)";

        public ObservableCollection<CatalogNode> Roots { get; }

        /// <summary>Every node, depth-first — feeds the "Go to node" picker and the match computation.</summary>
        public IReadOnlyList<CatalogNode> AllNodes { get; }

        /// <summary>The two search modes the find bar offers (Off is excluded).</summary>
        public IReadOnlyList<TreeSearchMode> SearchModes { get; } =
            new[] { TreeSearchMode.Highlight, TreeSearchMode.Filter };

        /// <summary>Bound to <c>WWTreeView.SearchDebounce</c> (a <see cref="TimeSpan"/>); driven by the ms slider.</summary>
        public TimeSpan SearchDebounce => TimeSpan.FromMilliseconds(DebounceMs);

        /// <summary>Bound to <c>WWTreeView.SelectionChangedCommand</c>; receives the live selection list.</summary>
        public IRelayCommand<IList> SelectionChangedCommand { get; }

        public TreeSearchSampleViewModel()
        {
            Roots = new ObservableCollection<CatalogNode>(BuildTree());
            AllNodes = Flatten(Roots).ToList();
            SelectionChangedCommand = new RelayCommand<IList>(OnSelectionChanged);
        }

        /// <summary>
        /// The nodes whose name contains <paramref name="text"/> — the same rule the tree's own matcher
        /// applies. The code-behind hands this to <c>WWTreeView.SelectItems</c> so "Select all matches"
        /// selects exactly the set the find bar is cycling.
        /// </summary>
        public IReadOnlyList<CatalogNode> MatchesFor(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<CatalogNode>();

            return AllNodes
                .Where(n => n.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private void OnSelectionChanged(IList? items)
        {
            SelectionSummary = items == null || items.Count == 0
                ? "(none)"
                : string.Join(", ", items.Cast<CatalogNode>().Select(n => n.Name));
        }

        private static IEnumerable<CatalogNode> Flatten(IEnumerable<CatalogNode> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;
                foreach (var descendant in Flatten(node.Children))
                    yield return descendant;
            }
        }

        private static CatalogNode[] BuildTree()
        {
            // Local builder: nests children and wires each child's Parent so CatalogNode.Path can walk up.
            CatalogNode N(string name, params CatalogNode[] children)
            {
                var node = new CatalogNode(name);
                foreach (var child in children)
                {
                    child.Parent = node;
                    node.Children.Add(child);
                }
                return node;
            }

            // A library-shaped catalog: names repeat tokens ("WW", "Grid", "Column", "Property") so a
            // search turns up several matches to cycle through.
            return new[]
            {
                N("WWControls.Wpf.Controls",
                    N("Primitives",
                        N("WWButton"), N("WWTreeView"), N("WWTreeViewItem"), N("Icon"),
                        N("SimpleStackPanel"), N("HighlightTextBlock"), N("WWMessageBox")),
                    N("Editors",
                        N("WWTextBox"), N("WWComboBox"), N("WWDatePicker"), N("WWNumericUpDown"),
                        N("WWCheckBox"), N("WWColorPicker"), N("WWListBox")),
                    N("PropertyGrid",
                        N("WWPropertyGrid"), N("WWPropertyDefinition"), N("PropertyItem"))),
                N("WWControls.Wpf.Grid",
                    N("SearchDataGrid"), N("GridColumn"), N("ColumnHeader"),
                    N("FilterPanel"), N("AutoFilterRow")),
                N("WWControls.Core",
                    N("RelayCommand"), N("AsyncCommand"), N("SearchQuery")),
            };
        }
    }

    /// <summary>
    /// A catalog node for the search sample. Derives from <see cref="WWTreeNodeBase{T}"/> for the
    /// children / expansion / filter plumbing and implements <see cref="MatchesSelf"/> so the tree's
    /// search engine can test it. Exposes <see cref="Path"/> (the ancestor chain) for the picker label.
    /// </summary>
    public sealed class CatalogNode : WWTreeNodeBase<CatalogNode>
    {
        public CatalogNode(string name) => Name = name;

        public string Name { get; }

        /// <summary>The full path from the root, e.g. "WWControls.Wpf.Controls › Editors › WWColorPicker".</summary>
        public string Path
        {
            get
            {
                var parts = new List<string>();
                for (CatalogNode? node = this; node != null; node = node.Parent)
                    parts.Insert(0, node.Name);
                return string.Join(" › ", parts);
            }
        }

        public override bool MatchesSelf(WWControls.Core.SearchQuery query) => query.Matches(Name);
    }
}
