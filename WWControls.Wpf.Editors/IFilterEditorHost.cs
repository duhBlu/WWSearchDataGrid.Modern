using System.Windows.Input;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// Narrow, grid-agnostic contract an editor's <c>EditSettings</c> binds its filter-row editor to.
    /// Implemented by the grid's <c>ColumnFilterControl</c> (whose full <c>IColumnFilterHost</c>
    /// derives from this). Kept minimal so the <c>EditSettings</c> filter editors can live in the
    /// Editors assembly without referencing grid-only types like <c>GridColumn</c>.
    /// </summary>
    public interface IFilterEditorHost
    {
        /// <summary>Currently-typed filter text (string fast path).</summary>
        string SearchText { get; }

        /// <summary>Tri-state checkbox filter value (null / true / false).</summary>
        bool? FilterCheckboxState { get; set; }

        /// <summary>The column context (binding path, formatting) for building the filter editor.</summary>
        IEditorColumn EditorColumn { get; }

        /// <summary>Routes a boundary arrow key to the filter-row's own cell-to-cell navigation. Returns whether navigation was handled.</summary>
        bool TryNavigateOnArrow(KeyEventArgs e);
    }
}
