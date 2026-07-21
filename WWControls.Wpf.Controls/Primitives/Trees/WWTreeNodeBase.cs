using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using WWControls.Core;

namespace WWControls.Wpf.Controls.Primitives
{
    /// <summary>
    /// Ready-made base for tree-node view models bound to a <see cref="WWTreeView"/>. Supplies the child
    /// collection, a filter-gated <see cref="ChildrenView"/>, parent link, and all the state flags the
    /// filter pass writes. Derive and implement <see cref="MatchesSelf"/>.
    /// </summary>
    /// <typeparam name="T">The concrete node type (CRTP), so <see cref="Children"/> and <see cref="Parent"/> are strongly typed.</typeparam>
    public abstract class WWTreeNodeBase<T> : IWWFilterableTreeNode, INotifyPropertyChanged
        where T : WWTreeNodeBase<T>
    {
        private bool _isVisibleInFilter = true;
        private bool _hasMatchingDescendant;
        private bool _isCurrentSearchMatch;
        private bool _isExpanded;
        private bool _isSelected;
        private T _parent;
        private ObservableCollection<T> _children = new ObservableCollection<T>();
        private ListCollectionView _childrenView;

        public T Parent
        {
            get => _parent;
            set => SetField(ref _parent, value);
        }

        public ObservableCollection<T> Children => _children;

        /// <summary>
        /// Filtered view over <see cref="Children"/>. Created lazily on first access (so unexpanded
        /// branches stay cheap); the filter reads each child's <see cref="IsVisibleInFilter"/>.
        /// </summary>
        public ICollectionView ChildrenView
        {
            get
            {
                if (_childrenView == null)
                {
                    _childrenView = new ListCollectionView(_children)
                    {
                        Filter = o => o is IWWFilterableTreeNode node && node.IsVisibleInFilter
                    };
                }
                return _childrenView;
            }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetField(ref _isExpanded, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetField(ref _isSelected, value);
        }

        public bool IsVisibleInFilter
        {
            get => _isVisibleInFilter;
            set => SetField(ref _isVisibleInFilter, value);
        }

        public bool HasMatchingDescendant
        {
            get => _hasMatchingDescendant;
            set => SetField(ref _hasMatchingDescendant, value);
        }

        public bool IsCurrentSearchMatch
        {
            get => _isCurrentSearchMatch;
            set => SetField(ref _isCurrentSearchMatch, value);
        }

        IEnumerable IWWTreeNode.Children => _children;

        public abstract bool MatchesSelf(SearchQuery query);

        public void RefreshChildrenView() => _childrenView?.Refresh();

        /// <summary>
        /// Replaces the children, re-parenting the new nodes and detaching the old filtered view.
        /// </summary>
        public void SetChildren(IEnumerable<T> newChildren)
        {
            if (_childrenView != null)
            {
                _childrenView.Filter = null;
                _childrenView = null;
            }

            _children = newChildren as ObservableCollection<T>
                ?? new ObservableCollection<T>(newChildren ?? Enumerable.Empty<T>());

            foreach (var child in _children)
                child.Parent = (T)this;

            OnPropertyChanged(nameof(Children));
            OnPropertyChanged(nameof(ChildrenView));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetField<TField>(ref TField field, TField value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<TField>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
