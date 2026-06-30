using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Editor-time view model for a single filter condition row inside the Filter Editor.
    /// Wraps a <see cref="SearchTemplate"/> so the existing input templates and
    /// search-type registry continue to drive the value editor unchanged.
    /// </summary>
    public class FilterConditionNode : FilterEditorNode
    {
        private GridColumn column;
        private SearchTemplate searchTemplate;
        private ICommand removeCommand;

        public FilterConditionNode()
        {
        }

        /// <summary>
        /// The column this condition targets. Changing the column rebuilds the underlying
        /// <see cref="SearchTemplate"/> against the new column's controller so that
        /// <see cref="SearchTemplate.AvailableValues"/>, <see cref="SearchTemplate.ValidSearchTypes"/>,
        /// the display value provider, and the mask pattern all reflect the new column.
        /// The previous template is left untouched in its original column's controller — Cancel
        /// rolls back cleanly because nothing outside the editor tree was mutated.
        /// </summary>
        public GridColumn Column
        {
            get => column;
            set
            {
                if (!SetProperty(value, ref column)) return;
                RebuildTemplateForCurrentColumn();
            }
        }

        /// <summary>
        /// The wrapped <see cref="SearchTemplate"/>. Created on first access (or when
        /// <see cref="Column"/> is set), and re-used for the lifetime of this node.
        /// </summary>
        public SearchTemplate SearchTemplate
        {
            get
            {
                EnsureTemplate();
                return searchTemplate;
            }
        }

        /// <summary>
        /// Columns available for this condition's column-chip ComboBox. Resolved through the
        /// parent group chain so the same list propagates without per-node duplication.
        /// </summary>
        public ObservableCollection<GridColumn> AvailableColumns => Parent?.AvailableColumns;

        public ICommand RemoveCommand =>
            removeCommand ?? (removeCommand = new RelayCommand(_ =>
            {
                var parent = Parent;
                if (parent == null) return;
                parent.Children.Remove(this);
                FilterEditorNormalizer.NormalizeAfterRemoval(parent);
            }));

        // Pass-through properties used by chip controls so XAML bindings can stay flat.
        public SearchType SearchType
        {
            get => SearchTemplate.SearchType;
            set { SearchTemplate.SearchType = value; OnPropertyChanged(nameof(SearchType)); }
        }

        public object SelectedValue
        {
            get => SearchTemplate.SelectedValue;
            set { SearchTemplate.SelectedValue = value; OnPropertyChanged(nameof(SelectedValue)); }
        }

        public object SelectedSecondaryValue
        {
            get => SearchTemplate.SelectedSecondaryValue;
            set { SearchTemplate.SelectedSecondaryValue = value; OnPropertyChanged(nameof(SelectedSecondaryValue)); }
        }

        public FilterInputTemplate InputTemplate => SearchTemplate.InputTemplate;

        private void EnsureTemplate()
        {
            if (searchTemplate != null) return;
            searchTemplate = CreateTemplateForColumn(column, preserveFrom: null);
            OnPropertyChanged(nameof(SearchTemplate));
            OnPropertyChanged(nameof(InputTemplate));
        }

        /// <summary>
        /// Replace the current <see cref="SearchTemplate"/> with one bound to the current
        /// column's controller, preserving the user's <c>SearchType</c> (if still valid for the
        /// new column's data type) and value selections.
        /// </summary>
        private void RebuildTemplateForCurrentColumn()
        {
            var previous = searchTemplate;
            searchTemplate = CreateTemplateForColumn(column, preserveFrom: previous);
            OnPropertyChanged(nameof(SearchTemplate));
            OnPropertyChanged(nameof(SearchType));
            OnPropertyChanged(nameof(SelectedValue));
            OnPropertyChanged(nameof(SelectedSecondaryValue));
            OnPropertyChanged(nameof(InputTemplate));
        }

        private static SearchTemplate CreateTemplateForColumn(GridColumn target, SearchTemplate preserveFrom)
        {
            var controller = target?.SearchTemplateController;
            var columnDataType = controller?.ColumnDataType ?? ColumnDataType.String;

            var fresh = new SearchTemplate(columnDataType)
            {
                SearchTemplateController = controller
            };

            if (preserveFrom == null) return fresh;

            // Keep the user's previous SearchType if it remains valid for the new column.
            if (fresh.ValidSearchTypes != null && fresh.ValidSearchTypes.Contains(preserveFrom.SearchType))
            {
                fresh.SearchType = preserveFrom.SearchType;
            }

            fresh.SelectedValue = preserveFrom.SelectedValue;
            fresh.SelectedSecondaryValue = preserveFrom.SelectedSecondaryValue;
            return fresh;
        }

        /// <summary>
        /// Adopts an existing <see cref="SearchTemplate"/> instance (e.g. when rebuilding the
        /// editor tree from a column's existing groups). The template's controller is left
        /// untouched so its <see cref="SearchTemplate.ValidSearchTypes"/> remain in sync.
        /// </summary>
        internal void AdoptTemplate(SearchTemplate template, GridColumn owningColumn)
        {
            searchTemplate = template;
            column = owningColumn;
            OnPropertyChanged(nameof(Column));
            OnPropertyChanged(nameof(SearchTemplate));
            OnPropertyChanged(nameof(SearchType));
            OnPropertyChanged(nameof(SelectedValue));
            OnPropertyChanged(nameof(SelectedSecondaryValue));
            OnPropertyChanged(nameof(InputTemplate));
        }
    }
}
