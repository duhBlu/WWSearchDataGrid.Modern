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
        private bool isUpdating;

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
                if (SetProperty(value, ref isChecked))
                {
                    if (!isUpdating && value.HasValue)
                    {
                        // Update all children when group selection changes
                        UpdateChildrenSelection(value.Value);
                    }

                    // Notify parent of selection change
                    Parent?.OnChildSelectionChanged();
                }
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
                        group.IsSelected = isSelected;
                    }
                    else
                    {
                        child.IsSelected = isSelected;
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
            if (!isUpdating)
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
            if (Children.Count == 0)
            {
                IsSelected = false;
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

            if (selectedCount == 0)
                IsSelected = false;
            else if (selectedCount == totalCount)
                IsSelected = true;
            else
                IsSelected = null; // Indeterminate state
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
    }
}
