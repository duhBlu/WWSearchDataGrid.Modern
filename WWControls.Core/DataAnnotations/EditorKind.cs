namespace WWControls.Core.DataAnnotations
{
    /// <summary>
    /// Identifies which editor a column should use, named independently of the WPF editor types
    /// so model assemblies can annotate without referencing the WPF library. The grid maps each
    /// value onto a concrete <c>BaseEditorSettings</c> subclass when generating a smart column.
    /// </summary>
    public enum EditorKind
    {
        /// <summary>
        /// Let the grid choose the editor from the field's CLR type (the non-smart default):
        /// text for strings/numbers, checkbox for <c>bool</c>, date picker for <c>DateTime</c>.
        /// </summary>
        Default = 0,

        /// <summary>Plain text editor (<c>TextBoxSettings</c>).</summary>
        Text,

        /// <summary>Checkbox editor (<c>CheckBoxSettings</c>).</summary>
        CheckBox,

        /// <summary>Drop-down list editor (<c>ComboBoxSettings</c>).</summary>
        ComboBox,

        /// <summary>Date picker editor (<c>DatePickerSettings</c>).</summary>
        Date,

        /// <summary>Numeric up/down editor (<c>NumericUpDownSettings</c>).</summary>
        Spin,
    }
}
