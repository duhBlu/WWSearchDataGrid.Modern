using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Editor-time view model for a logical group inside the Filter Editor. Contains a
    /// heterogeneous mix of nested groups and condition rows. Mutations on
    /// <see cref="Children"/> reparent nodes automatically so <see cref="FilterEditorNode.Depth"/>
    /// stays accurate.
    /// </summary>
    public class FilterGroupNode : FilterEditorNode
    {
        private LogicalOperator op = LogicalOperator.And;
        private ObservableCollection<GridColumn> availableColumns;
        private ICommand addConditionCommand;
        private ICommand addGroupCommand;
        private ICommand removeCommand;

        public FilterGroupNode()
        {
            Children = new ObservableCollection<FilterEditorNode>();
            Children.CollectionChanged += OnChildrenChanged;
        }

        /// <summary>
        /// The logical operator joining this group's children. Negated variants
        /// (NotAnd / NotOr) wrap the combined body in <c>Expression.Not</c> at build time.
        /// </summary>
        public LogicalOperator Operator
        {
            get => op;
            set => SetProperty(value, ref op);
        }

        /// <summary>
        /// Heterogeneous collection of child nodes (groups and condition rows).
        /// </summary>
        public ObservableCollection<FilterEditorNode> Children { get; }

        /// <summary>
        /// Columns available to any condition row in this subtree. Set by the editor's open-time
        /// builder; condition rows reach this collection via their parent chain.
        /// </summary>
        public ObservableCollection<GridColumn> AvailableColumns
        {
            get => availableColumns ?? Parent?.AvailableColumns;
            internal set
            {
                if (SetProperty(value, ref availableColumns))
                {
                    OnPropertyChanged(nameof(AvailableColumns));
                }
            }
        }

        public ICommand AddConditionCommand =>
            addConditionCommand ?? (addConditionCommand = new RelayCommand(_ =>
            {
                var condition = new FilterConditionNode();
                Children.Insert(0, condition);
                var columns = AvailableColumns;
                if (columns != null && columns.Count > 0 && condition.Column == null)
                {
                    condition.Column = columns[0];
                }
            }));

        public ICommand AddGroupCommand =>
            addGroupCommand ?? (addGroupCommand = new RelayCommand(_ =>
            {
                Children.Add(new FilterGroupNode { Operator = LogicalOperator.And });
            }));

        public ICommand RemoveCommand =>
            removeCommand ?? (removeCommand = new RelayCommand(_ =>
            {
                var parent = Parent;
                if (parent == null) return;
                parent.Children.Remove(this);
                FilterEditorNormalizer.NormalizeAfterRemoval(parent);
            }));

        private void OnChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (FilterEditorNode child in e.NewItems)
                {
                    child.Parent = this;
                }
            }
            if (e.OldItems != null)
            {
                foreach (FilterEditorNode child in e.OldItems)
                {
                    if (child.Parent == this)
                        child.Parent = null;
                }
            }
        }
    }
}
