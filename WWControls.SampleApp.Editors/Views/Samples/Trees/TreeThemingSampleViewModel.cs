using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WWControls.Wpf.Controls.Primitives;

namespace WWControls.SampleApp.Editors.Views.Samples.Trees
{
    /// <summary>
    /// Backs the "Theming" sample: the tree's chrome is restyled through plain brush / metric
    /// properties — no retemplating. Four <c>WWColorPicker</c>s bind two-way to the role
    /// <see cref="Color"/>s below; each projects to a frozen <see cref="Brush"/> the tree binds
    /// (<c>SelectionBrush</c> / <c>ItemHoverBrush</c> / <c>ConnectorLineBrush</c> / <c>LineHoverBrush</c>).
    /// A slider sizes <c>ConnectorLineThickness</c>, and a preset picker re-seeds every role at once.
    /// The empty-state toggle swaps the tree to a custom <c>EmptyTemplate</c>.
    /// </summary>
    public partial class TreeThemingSampleViewModel : ObservableObject
    {
        // ── Role colors (each picker binds SelectedColor two-way here) ───────────
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectionBrush))]
        private Color _selectionColor = Hex("#E5F1FB");

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ItemHoverBrush))]
        private Color _itemHoverColor = Hex("#F0F0F0");

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ConnectorLineBrush))]
        private Color _connectorLineColor = Hex("#E0E0E0");

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LineHoverBrush))]
        private Color _lineHoverColor = Hex("#0078D4");

        // ── Brush projections the tree binds to (SelectedColor is a Color, not a Brush) ──
        public Brush SelectionBrush => Frozen(SelectionColor);
        public Brush ItemHoverBrush => Frozen(ItemHoverColor);
        public Brush ConnectorLineBrush => Frozen(ConnectorLineColor);
        public Brush LineHoverBrush => Frozen(LineHoverColor);

        // ── Metrics / layout ─────────────────────────────────────────────────────
        [ObservableProperty]
        private double _lineThickness = 1.0;

        [ObservableProperty]
        private bool _showConnectorLines = true;

        [ObservableProperty]
        private bool _rowFullWidthHover = true;

        [ObservableProperty]
        private TreeThemePreset _preset = TreeThemePreset.Default;

        [ObservableProperty]
        private bool _showEmptyState;

        // Holds the roots while the empty-state toggle empties the tree, so toggling back restores them.
        private readonly List<ThemeNode> _stashedRoots = new();

        public ObservableCollection<ThemeNode> Roots { get; }

        /// <summary>Coordinated presets offered by the picker.</summary>
        public IReadOnlyList<TreeThemePreset> Presets { get; } =
            new[] { TreeThemePreset.Default, TreeThemePreset.Slate, TreeThemePreset.HighContrast, TreeThemePreset.Playful };

        /// <summary>Swatches offered inside every color picker's popup.</summary>
        public IReadOnlyList<Color> Swatches { get; } = new[]
        {
            Colors.White, Hex("#F0F0F0"), Hex("#E5F1FB"), Hex("#E0E0E0"), Hex("#CFD8DC"),
            Hex("#0078D4"), Hex("#2196F3"), Hex("#009688"), Hex("#4CAF50"), Hex("#FF9800"),
            Hex("#E91E63"), Hex("#9C27B0"), Hex("#616161"), Hex("#212121"), Colors.Black,
        };

        public TreeThemingSampleViewModel()
        {
            Roots = new ObservableCollection<ThemeNode>(BuildTree());
        }

        /// <summary>Restores the sample nodes — bound from the custom empty template's call-to-action button.</summary>
        [RelayCommand]
        private void RestoreNodes() => ShowEmptyState = false;

        // Picking a preset re-seeds all four role colors and the line thickness in one move.
        partial void OnPresetChanged(TreeThemePreset value)
        {
            var (selection, itemHover, connector, lineHover, thickness) = ThemeFor(value);
            SelectionColor = selection;
            ItemHoverColor = itemHover;
            ConnectorLineColor = connector;
            LineHoverColor = lineHover;
            LineThickness = thickness;
        }

        // Empties the tree (stashing the roots) to show the EmptyTemplate, and restores on toggle-off.
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

        private static (Color selection, Color itemHover, Color connector, Color lineHover, double thickness)
            ThemeFor(TreeThemePreset preset) => preset switch
        {
            TreeThemePreset.Slate =>
                (Hex("#DCE3EA"), Hex("#EDF1F5"), Hex("#B7C2CD"), Hex("#37506B"), 1.0),
            TreeThemePreset.HighContrast =>
                (Hex("#FFE08A"), Hex("#F2F2F2"), Hex("#000000"), Hex("#D40000"), 2.0),
            TreeThemePreset.Playful =>
                (Hex("#FCE4EC"), Hex("#F3E5F5"), Hex("#CE93D8"), Hex("#E91E63"), 1.5),
            _ =>
                (Hex("#E5F1FB"), Hex("#F0F0F0"), Hex("#E0E0E0"), Hex("#0078D4"), 1.0),
        };

        private static ThemeNode[] BuildTree()
        {
            ThemeNode N(string name, params ThemeNode[] children)
            {
                var node = new ThemeNode(name);
                foreach (var child in children)
                {
                    child.Parent = node;
                    node.Children.Add(child);
                }
                return node;
            }

            return new[]
            {
                N("MyApp",
                    N("src",
                        N("App.xaml"),
                        N("MainWindow.xaml"),
                        N("Program.cs"),
                        N("Views", N("HomeView.xaml"), N("SettingsView.xaml")),
                        N("ViewModels", N("HomeViewModel.cs"), N("SettingsViewModel.cs"))),
                    N("tests",
                        N("HomeTests.cs"), N("SettingsTests.cs")),
                    N("docs",
                        N("README.md"), N("CHANGELOG.md"))),
            };
        }

        private static Color Hex(string hex) => (Color)ColorConverter.ConvertFromString(hex);

        private static Brush Frozen(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }

    /// <summary>Coordinated look presets the theming sample can apply to all role colors at once.</summary>
    public enum TreeThemePreset
    {
        Default,
        Slate,
        HighContrast,
        Playful,
    }

    /// <summary>A file-tree node for the theming sample — just a name and the child plumbing from the base.</summary>
    public sealed class ThemeNode : WWTreeNodeBase<ThemeNode>
    {
        public ThemeNode(string name) => Name = name;

        public string Name { get; }

        public override bool MatchesSelf(WWControls.Core.SearchQuery query) => query.Matches(Name);
    }
}
