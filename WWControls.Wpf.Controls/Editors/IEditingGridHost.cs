using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf.Controls.Editors
{
    /// <summary>
    /// The grid-side cell-editing services an editor's <c>EditSettings</c> needs when its editor is
    /// hosted in a data grid — commit/navigation gating, mouse-edit caret hand-off, and the grid-wide
    /// decoration-button policy. Implemented by <c>SearchDataGrid</c>; the editor settings and the
    /// editor host reach it via <c>FindVisualAncestor&lt;IEditingGridHost&gt;</c>, so they never name
    /// the grid type and can live in the Editors assembly.
    /// </summary>
    public interface IEditingGridHost
    {
        /// <summary>True while an editing cell holds an unresolved validation error and commit-on-error is off — an arrow at a cell boundary must not carry focus away.</summary>
        bool IsEditLockActive();

        /// <summary>Flags the grid to BeginEdit on the next cell that receives focus (used when an arrow key carries the edit to an adjacent cell).</summary>
        void SetCarryEditStateOnNextFocus();

        /// <summary>Wraps a Left/Right arrow at the row edge to the opposite end of the same row. Returns false when not at a row edge.</summary>
        bool TryWrapArrowWithinRow(DataGridCell cell, bool forward);

        /// <summary>If edit mode was started by a mouse click, returns the click point so the editor can place its caret there. Returns false for keyboard-initiated edits.</summary>
        bool TryConsumeMouseEditPoint(DataGridCell forCell, out Point cellPoint);

        /// <summary>The grid-wide default for editor decoration-button visibility (an editor's own <c>EditorButtonShowMode</c> can override).</summary>
        EditorButtonShowMode EditorButtonShowMode { get; }

        /// <summary>Whether the grid allows committing a cell whose value fails a data-annotation attribute (drives the validation rule).</summary>
        bool AllowCommitOnValidationAttributeError { get; }
    }
}
