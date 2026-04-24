using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Hierarchical node for DateTime column values in the Filter Values tab.
    /// Represents a Year, Month, or Day level with tri-state checkbox support.
    /// </summary>
    public class DateTreeGroupItem : ObservableObject
    {
        private bool? _isChecked = false;
        private bool _isExpanded;
        private bool _isSyncing;

        /// <summary>
        /// Reference to the owning FilterValueManager for batched sync on user checkbox changes.
        /// </summary>
        public FilterValueManager Manager { get; set; }

        /// <summary>
        /// Display text for this node ("2025", "March", "15").
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Tri-state checked: true (all children checked), false (none), null (mixed).
        /// </summary>
        public bool? IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;
                SetProperty(value, ref _isChecked);

                if (!_isSyncing && value.HasValue)
                {
                    // Route through manager for batched sync (suppresses per-leaf SyncToRules)
                    if (Manager != null)
                    {
                        Manager.OnTreeNodeCheckedByUser(this, value.Value);
                    }
                    else
                    {
                        SetCheckStateRecursive(value.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Whether this tree node is expanded in the TreeView.
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(value, ref _isExpanded);
        }

        /// <summary>
        /// Sum of occurrence counts across all descendant leaf values.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Child nodes (Year has Month children, Month has Day children).
        /// </summary>
        public ObservableCollection<DateTreeGroupItem> Children { get; set; } = new ObservableCollection<DateTreeGroupItem>();

        /// <summary>
        /// Parent node for upward propagation. Null for root (year) nodes.
        /// </summary>
        public DateTreeGroupItem Parent { get; set; }

        /// <summary>
        /// At the leaf (day) level, references to the flat CheckableValueItems.
        /// Null for non-leaf nodes.
        /// </summary>
        public List<CheckableValueItem> LeafValues { get; set; }

        /// <summary>
        /// Cascades a check state to all descendants and linked leaf values.
        /// </summary>
        public void SetCheckStateRecursive(bool isChecked)
        {
            _isSyncing = true;
            try
            {
                if (LeafValues != null)
                {
                    foreach (var leaf in LeafValues)
                        leaf.IsChecked = isChecked;
                }

                foreach (var child in Children)
                {
                    child._isSyncing = true;
                    child._isChecked = isChecked;
                    child.OnPropertyChanged(nameof(IsChecked));
                    child.SetCheckStateRecursive(isChecked);
                    child._isSyncing = false;
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        /// <summary>
        /// Recalculates this node's IsChecked from its immediate children/leaf states,
        /// then propagates upward to the parent. Use when a single child changed (user click).
        /// </summary>
        public void UpdateCheckStateFromChildren()
        {
            _isSyncing = true;
            try
            {
                RecalculateFromDirectChildren();
            }
            finally
            {
                _isSyncing = false;
            }

            // Propagate upward
            Parent?.UpdateCheckStateFromChildren();
        }

        /// <summary>
        /// Depth-first refresh: recursively updates all descendants first, then this node.
        /// Use after bulk changes (SyncFromRules, Initialize) where the entire tree needs
        /// to sync from leaf CheckableValueItem states upward.
        /// </summary>
        public void RefreshCheckStateFromLeaves()
        {
            _isSyncing = true;
            try
            {
                // Recurse into children first so their states are current
                foreach (var child in Children)
                    child.RefreshCheckStateFromLeaves();

                RecalculateFromDirectChildren();
            }
            finally
            {
                _isSyncing = false;
            }
        }

        private void RecalculateFromDirectChildren()
        {
            if (Children.Count == 0 && LeafValues != null)
            {
                // Leaf parent: check leaf values
                bool allChecked = LeafValues.All(l => l.IsChecked);
                bool noneChecked = LeafValues.All(l => !l.IsChecked);
                _isChecked = allChecked ? true : noneChecked ? false : (bool?)null;
            }
            else if (Children.Count > 0)
            {
                bool allChecked = Children.All(c => c.IsChecked == true);
                bool noneChecked = Children.All(c => c.IsChecked == false);
                _isChecked = allChecked ? true : noneChecked ? false : (bool?)null;
            }

            OnPropertyChanged(nameof(IsChecked));
        }
    }
}
