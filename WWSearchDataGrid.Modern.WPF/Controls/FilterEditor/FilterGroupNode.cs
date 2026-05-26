using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
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
        private bool isAddPopupOpen;
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
            set
            {
                if (SetProperty(value, ref op))
                {
                    OnPropertyChanged(nameof(HasMixedColumnsWithOrOperator));
                }
            }
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

        /// <summary>
        /// Inline warning trigger: true when this group's operator is OR-flavored
        /// (<see cref="LogicalOperator.Or"/> or <see cref="LogicalOperator.NotOr"/>) and its direct
        /// condition children target more than one column. The editor writes per-column slices
        /// back to each column's controller, so cross-column OR / NotOr cannot round-trip — the
        /// predicate degrades to the lossy AND form.
        /// </summary>
        public bool HasMixedColumnsWithOrOperator
        {
            get
            {
                if (Operator != LogicalOperator.Or && Operator != LogicalOperator.NotOr) return false;

                var distinctColumns = Children
                    .OfType<FilterConditionNode>()
                    .Where(c => c.Column != null)
                    .Select(c => c.Column)
                    .Distinct()
                    .Count();

                return distinctColumns > 1;
            }
        }

        /// <summary>
        /// Bound two-way by the Add popup so the AddCondition/AddGroup commands can close it
        /// after they fire. The popup itself opens via the toggle button in the template.
        /// </summary>
        public bool IsAddPopupOpen
        {
            get => isAddPopupOpen;
            set => SetProperty(value, ref isAddPopupOpen);
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
                IsAddPopupOpen = false;
            }));

        public ICommand AddGroupCommand =>
            addGroupCommand ?? (addGroupCommand = new RelayCommand(_ =>
            {
                Children.Insert(0, new FilterGroupNode { Operator = LogicalOperator.And });
                IsAddPopupOpen = false;
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
            OnPropertyChanged(nameof(HasMixedColumnsWithOrOperator));
        }
    }
}
