using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Enhanced filter value group with hierarchical selection support
    /// </summary>
    public partial class FilterValueGroup : FilterValueItem
    {
        private bool? isChecked;
        internal bool isUpdating;

        public object GroupKey { get; set; }

        private ObservableCollection<FilterValueItem> _children;
        private bool _childrenLoaded = false;

        public ObservableCollection<FilterValueItem> Children
        {
            get
            {
                EnsureChildrenLoaded();
                return _children;
            }
        }

        public Action LazyLoadChildren { get; set; }

        public FilterValueGroup()
        {
            _children = new ObservableCollection<FilterValueItem>();
        }

        /// <summary>
        /// Gets or sets the three-state selection for groups
        /// </summary>
        public new bool? IsSelected
        {
            get => isChecked;
            set
            {
                // Store the previous value for comparison
                var previousValue = isChecked;
                
                // Always set the property, even if it's the same value (for UI responsiveness)
                isChecked = value;
                
                if (!isUpdating)
                {
                    // Handle user interaction with checkboxes
                    if (value.HasValue)
                    {
                        // User clicked to set to true or false - update children accordingly
                        UpdateChildrenSelection(value.Value);
                    }
                    else
                    {
                        // Indeterminate state clicked - select all children
                        UpdateChildrenSelection(true);
                        isChecked = true;
                    }
                }

                // Always notify of property changes for UI binding
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(SelectedChildCount));
                
                // Notify parent of selection change
                Parent?.OnChildSelectionChanged();
            }
        }

        public void EnsureChildrenLoaded()
        {
            if (!_childrenLoaded && LazyLoadChildren != null)
            {
                LazyLoadChildren();
                _childrenLoaded = true;
                LazyLoadChildren = null; // Clear to free memory
            }
        }

        /// <summary>
        /// Updates all children to match the specified selection state
        /// </summary>
        private void UpdateChildrenSelection(bool isSelected)
        {
            isUpdating = true;
            try
            {
                foreach (var child in Children)
                {
                    if (child is FilterValueGroup group)
                    {
                        // For child groups, use silent update to prevent immediate parent notifications
                        group.isUpdating = true;
                        group.isChecked = isSelected;
                        
                        // Recursively update their children
                        group.UpdateChildrenSelection(isSelected);
                        
                        group.isUpdating = false;
                        group.OnPropertyChanged(nameof(IsSelected));
                        group.OnPropertyChanged(nameof(SelectedChildCount));
                    }
                    else
                    {
                        // For leaf items, use SetIsSelected to trigger UI updates
                        child.SetIsSelected(isSelected);
                    }
                }
            }
            finally
            {
                isUpdating = false;
            }
        }

        /// <summary>
        /// Called when a child's selection state changes
        /// </summary>
        internal void OnChildSelectionChanged()
        {
            // Don't recalculate state if we're in the middle of a bulk update
            // or if our parent is in the middle of a bulk update
            if (!isUpdating && (Parent == null || !Parent.isUpdating))
            {
                isUpdating = true;
                try
                {
                    UpdateGroupSelectionState();
                }
                finally
                {
                    isUpdating = false;
                }
            }
        }

        /// <summary>
        /// Updates the group's selection state based on children
        /// </summary>
        public void UpdateGroupSelectionState()
        {
            isUpdating = true;
            try
            {
                if (Children.Count == 0)
                {
                    isChecked = false;
                    OnPropertyChanged(nameof(IsSelected));
                    OnPropertyChanged(nameof(SelectedChildCount));
                    return;
                }

                int selectedCount = 0;
                int totalCount = 0;

                foreach (var child in Children)
                {
                    if (child is FilterValueGroup group)
                    {
                        if (group.IsSelected == true)
                            selectedCount += group.GetTotalChildCount();
                        else if (group.IsSelected == null)
                            selectedCount += group.GetSelectedChildCount();

                        totalCount += group.GetTotalChildCount();
                    }
                    else
                    {
                        if (child.IsSelected)
                            selectedCount++;
                        totalCount++;
                    }
                }

                bool? previousState = isChecked;
                if (selectedCount == 0)
                    isChecked = false;
                else if (selectedCount == totalCount)
                    isChecked = true;
                else
                    isChecked = null; // Indeterminate state

                if (previousState != isChecked)
                {
                    OnPropertyChanged(nameof(IsSelected));
                    OnPropertyChanged(nameof(SelectedChildCount));
                }
            }
            finally
            {
                isUpdating = false;
            }
        }

        /// <summary>
        /// Gets the total count of all leaf items in this group
        /// </summary>
        public int GetTotalChildCount()
        {
            EnsureChildrenLoaded();
            return _children.Sum(c => c is FilterValueGroup g ? g.GetTotalChildCount() : 1);
        }

        /// <summary>
        /// Gets the count of selected leaf items in this group (property for binding)
        /// </summary>
        public int SelectedChildCount => GetSelectedChildCount();

        /// <summary>
        /// Gets the count of selected leaf items in this group
        /// </summary>
        public int GetSelectedChildCount()
        {
            if (IsSelected == true)
                return GetTotalChildCount();

            if (IsSelected == false)
                return 0;

            EnsureChildrenLoaded();
            return _children.Sum(c => c is FilterValueGroup g ? g.GetSelectedChildCount() : (c.IsSelected ? 1 : 0));
        }

        /// <summary>
        /// Sets the selection state without triggering events
        /// </summary>
        public void SetIsSelectedSilent(bool value, bool notifyPropertyChanged = true)
        {
            isUpdating = true;
            try
            {
                isChecked = value;
                
                // Update all children silently
                foreach (var child in Children)
                {
                    if (child is FilterValueGroup group)
                    {
                        group.SetIsSelectedSilent(value, notifyPropertyChanged);
                    }
                    else
                    {
                        child.SetIsSelectedSilent(value, false);
                    }
                }

                if (notifyPropertyChanged)
                {
                    OnPropertyChanged(nameof(IsSelected));
                    OnPropertyChanged(nameof(SelectedChildCount));
                }
            }
            finally
            {
                isUpdating = false;
            }
        }
    }
}
