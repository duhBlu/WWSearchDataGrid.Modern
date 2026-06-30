namespace WWControls.Wpf
{
    /// <summary>
    /// Tri-state cycle the checkbox filter rotates through. Maps to no-filter / equals-true /
    /// equals-false, with Intermediate switching to IsNull on nullable columns mid-cycle.
    /// </summary>
    public enum CheckboxCycleState
    {
        /// <summary>No filter (initial / manual-clear state).</summary>
        Intermediate,

        /// <summary>Shows only true values.</summary>
        Checked,

        /// <summary>Shows only false values.</summary>
        Unchecked,
    }
}
