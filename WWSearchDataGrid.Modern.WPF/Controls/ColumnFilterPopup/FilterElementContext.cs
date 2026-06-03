using System;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Bundles everything a <see cref="FilterElementBase"/> needs to drive filtering for one
    /// column. The owning <see cref="ColumnFilterPopup"/> constructs the context when the popup
    /// opens and hands it to the element; the element calls
    /// <see cref="RequestApply"/> when its internal state changes.
    /// </summary>
    public sealed class FilterElementContext
    {
        public FilterElementContext(
            GridColumn column,
            SearchTemplateController controller,
            FilterValueManager filterValueManager,
            Action applyFilter)
        {
            Column = column ?? throw new ArgumentNullException(nameof(column));
            Controller = controller ?? throw new ArgumentNullException(nameof(controller));
            FilterValueManager = filterValueManager;
            _applyFilter = applyFilter;
        }

        /// <summary>
        /// The column descriptor whose popup is open. Filter elements read
        /// <see cref="ColumnDataBase.FieldName"/>, <see cref="ColumnDataBase.FieldType"/>, etc. to shape
        /// their UI / converters.
        /// </summary>
        public GridColumn Column { get; }

        /// <summary>
        /// The column's persistent <see cref="SearchTemplateController"/>. Filter elements that
        /// edit filter rules directly (rather than going through
        /// <see cref="FilterValueManager"/>) push templates into
        /// <see cref="SearchTemplateController.SearchGroups"/>.
        /// </summary>
        public SearchTemplateController Controller { get; }

        /// <summary>
        /// Helper that translates checkbox / value selections into IsAnyOf / IsNoneOf rules and
        /// back. Provided when the popup wants to expose a checkbox-list interaction;
        /// <c>null</c> when the element drives rules directly.
        /// </summary>
        public FilterValueManager FilterValueManager { get; }

        private readonly Action _applyFilter;

        /// <summary>
        /// Signals that the element's state has changed and the grid should re-apply its filter.
        /// The owning editor controls debounce / batching policy; elements just call this
        /// whenever their selection changes.
        /// </summary>
        public void RequestApply() => _applyFilter?.Invoke();
    }
}
