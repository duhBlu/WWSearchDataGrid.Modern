using System;
using System.Collections.Generic;
using System.Text;

namespace WWSearchDataGrid.Modern.Core
{
    /// <summary>
    /// Enhanced filter value item with hierarchical selection support
    /// </summary>
    public partial class FilterValueItem : ObservableObject
    {
        private bool _isSelected;
        private object value;
        private string displayValue;
        private int itemCount;
        private FilterValueGroup parent;

        public object Value
        {
            get => value;
            set => SetProperty(value, ref this.value);
        }

        public string DisplayValue
        {
            get => displayValue;
            set => SetProperty(value, ref displayValue);
        }

        public int ItemCount
        {
            get => itemCount;
            set => SetProperty(value, ref itemCount);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(value, ref _isSelected))
                {
                    // Only notify parent if parent is not in the middle of a bulk update
                    if (parent != null && !parent.isUpdating)
                    {
                        parent.OnChildSelectionChanged();
                    }
                }
            }
        }

        public FilterValueGroup Parent
        {
            get => parent;
            set => parent = value;
        }

        public bool ShowItemCount => ItemCount > 1;

        internal void SetIsSelectedSilent(bool value, bool silent = true)
        {
            _isSelected = value;

            if (!silent)
            {
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public void SetIsSelected(bool isSelected)
        {
            if (_isSelected != isSelected)
            {
                _isSelected = isSelected;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

    }
}
