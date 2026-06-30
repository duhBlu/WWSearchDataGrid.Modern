namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Controls whether moving focus out of an open edit form that holds unsaved changes prompts
    /// the user before the edit is abandoned. The prompt asks whether to cancel editing; the
    /// available buttons depend on the mode.
    /// </summary>
    public enum EditFormPostConfirmationMode
    {
        /// <summary>
        /// No prompt. Focus leaves the form freely; the open row transaction stays open until the
        /// user commits or cancels. This is the default.
        /// </summary>
        None = 0,

        /// <summary>
        /// Prompts with Yes / No when focus leaves a dirty form. Yes cancels the edit (reverting
        /// the row); No keeps the form open with focus returned to it.
        /// </summary>
        YesNo,

        /// <summary>
        /// Prompts with Yes / No / Cancel when focus leaves a dirty form. Yes cancels the edit; No
        /// keeps editing; Cancel aborts the focus change and leaves the form open and focused.
        /// </summary>
        YesNoCancel,
    }
}
