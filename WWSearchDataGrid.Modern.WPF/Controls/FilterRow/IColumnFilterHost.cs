using System.Windows;
using System.Windows.Controls;
using WWSearchDataGrid.Modern.Core;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Contract that <see cref="SearchDataGridFiltering"/> (and the context-menu / chip
    /// pipeline) reads from each entry in <see cref="SearchDataGrid.DataColumns"/>. Lets the
    /// filter pipeline operate against either the modern <see cref="ColumnFilterControl"/> or
    /// any future filter-host implementation without coupling to a specific control type.
    /// </summary>
    /// <remarks>
    /// Phase 5 introduced this interface to replace the legacy <c>ColumnSearchBox</c>
    /// direct-type coupling. Members reflect the exact surface area
    /// <c>SearchDataGridFiltering</c> reads — adding members is a breaking change to every
    /// implementer, so keep the surface minimal: anything purely UI (focus, popup wiring,
    /// editor templates) stays on the concrete control.
    /// </remarks>
    public interface IColumnFilterHost
    {
        /// <summary>The <see cref="DataGridColumn"/> this filter host represents.</summary>
        DataGridColumn CurrentColumn { get; }

        /// <summary>The persistent <see cref="WPF.GridColumn"/> descriptor for the column.</summary>
        GridColumn GridColumn { get; }

        /// <summary>The shared filter-state controller for this column.</summary>
        SearchTemplateController SearchTemplateController { get; }

        /// <summary>
        /// Resolved property path used to read each row's value for filtering on this column.
        /// Surfaced so the grid's incremental cache-update pipeline can extract values without
        /// re-resolving the descriptor.
        /// </summary>
        string BindingPath { get; }

        /// <summary>Currently-typed search text (string fast path).</summary>
        string SearchText { get; }

        /// <summary>Whether a transient (uncommitted) filter template exists.</summary>
        bool HasTemporaryTemplate { get; }

        /// <summary>Whether any filter (live or committed) is currently active on this column.</summary>
        bool HasActiveFilter { get; }

        /// <summary>Whether an advanced rule-filter is committed for this column.</summary>
        bool HasAdvancedFilter { get; }

        /// <summary>True when the column is rendered as a tri-state checkbox filter.</summary>
        bool IsCheckboxColumn { get; }

        /// <summary>Current tri-state checkbox value (null/true/false). Only meaningful when <see cref="IsCheckboxColumn"/>.</summary>
        bool? FilterCheckboxState { get; set; }

        /// <summary>
        /// Whether the filter editor is visible for this column. Mirrors
        /// <see cref="WPF.GridColumn.AllowFiltering"/>. GridColumn writes through this when
        /// the column-level AllowFiltering DP changes.
        /// </summary>
        bool IsFilterVisible { get; set; }

        /// <summary>
        /// Whether the filter editor is enabled (interactive) for this column. Mirrors
        /// <see cref="WPF.GridColumn.AllowAutoFilter"/>. GridColumn writes through this when
        /// the column-level <c>AllowAutoFilter</c> DP changes. <c>false</c> greys the cell
        /// without collapsing it — distinct from <see cref="IsFilterVisible"/>, which hides
        /// the cell entirely.
        /// </summary>
        bool IsFilterEnabled { get; set; }

        /// <summary>
        /// WPF visibility of the host control itself. Surfaced on the interface so GridColumn
        /// can hide the editor wholesale when AllowFiltering is set to false. Implementations
        /// that derive from <see cref="UIElement"/> get this for free via the inherited member.
        /// </summary>
        Visibility Visibility { get; set; }

        /// <summary>Clears this column's filter (text + temporary template + checkbox state).</summary>
        void ClearFilter();

        /// <summary>
        /// Re-evaluates whether this column should render as a tri-state checkbox filter
        /// based on the descriptor. Called when <see cref="WPF.GridColumn.UseCheckBoxInSearchBox"/>
        /// changes.
        /// </summary>
        void DetermineCheckboxColumnTypeFromColumnDefinition();

        /// <summary>
        /// Opens the column's rule-filter editor popup. Invoked from the filter-popup button
        /// in the column header chrome (and any consumer-defined invocation path). Implementers
        /// commit any pending grid edit before showing the popup so the click doesn't leave a
        /// half-edited cell behind.
        /// </summary>
        void ShowFilterEditor();

        /// <summary>
        /// Recomputes the cached <see cref="IsComplexFilteringEnabled"/>-equivalent state from the
        /// owning grid and column descriptor — called when the grid's <c>EnableRuleFiltering</c> changes.
        /// </summary>
        void UpdateIsComplexFilteringEnabled();

        /// <summary>
        /// Recomputes <see cref="HasActiveFilter"/> from the underlying
        /// <see cref="SearchTemplateController"/> + transient template state.
        /// </summary>
        void UpdateHasActiveFilterState();

        /// <summary>
        /// Resolves the user-facing column display name from the descriptor — used by the
        /// filter-chip pipeline.
        /// </summary>
        string ResolveColumnDisplayName();
    }
}
