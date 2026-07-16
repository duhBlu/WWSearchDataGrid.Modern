using System;
using System.Collections.ObjectModel;
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

        public ObservableCollection<SampleTreeNode> Roots { get; }

        /// <summary>Bound to <c>WWTreeView.OnDropCommand</c>; the payload is <c>(dropTarget, draggedItem)</c>.</summary>
        public IRelayCommand<Tuple<object, object>> DropCommand { get; }

        public TreeViewSampleViewModel()
        {
            Roots = new ObservableCollection<SampleTreeNode>(BuildTree());
            DropCommand = new RelayCommand<Tuple<object, object>>(Drop, CanDrop);
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

            return new[]
            {
                N("WWControls.Wpf.Controls.Primitives", false,
                    N("Controls", true, N("WWButton.cs", true), N("WWTreeView.cs", true), N("Icon.cs", true)),
                    N("Themes", true, N("PrimitiveThemeKeys.cs", true), N("Generic.xaml", true))),
                N("WWControls.Wpf.Controls.Editors", false,
                    N("Controls", true, N("WWTextBox.cs", true), N("WWComboBox.cs", true), N("WWDatePicker.cs", true))),
                N("Documentation", false,
                    N("Primitives", true, N("WWButton.md", true), N("WWTreeView.md", true))),
            };
        }
    }

    /// <summary>
    /// A project/file node for the WWTreeView sample. Implements <see cref="IWWTreeViewDragItem"/> so
    /// the structural top-level nodes can opt out of being dragged (<see cref="CanDrag"/> = false).
    /// </summary>
    public sealed class SampleTreeNode : IWWTreeViewDragItem
    {
        public SampleTreeNode(string name) => Name = name;

        public string Name { get; }

        public ObservableCollection<SampleTreeNode> Children { get; } = new();

        public SampleTreeNode? Parent { get; set; }

        public bool CanDrag { get; set; } = true;
    }
}
