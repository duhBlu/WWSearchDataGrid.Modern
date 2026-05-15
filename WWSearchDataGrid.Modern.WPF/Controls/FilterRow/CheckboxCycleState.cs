namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Tri-state cycle a checkbox column rotates through when the user clicks /
    /// keyboard-activates the filter checkbox. Used by <see cref="ColumnFilterControl"/>'s
    /// checkbox path to drive the three filter shapes a boolean column can produce:
    /// no filter, equals-true, equals-false (plus an indeterminate-with-IsNull fallback
    /// on nullable columns once the cycle has been started).
    /// </summary>
    public enum CheckboxCycleState
    {
        /// <summary>Shows all data (no filter) — initial state and manual-clear state.</summary>
        Intermediate,

        /// <summary>Shows only true values.</summary>
        Checked,

        /// <summary>Shows only false values.</summary>
        Unchecked,
    }
}
