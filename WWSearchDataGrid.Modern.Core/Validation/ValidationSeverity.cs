namespace WWSearchDataGrid.Modern.Core.Validation
{
    /// <summary>
    /// Severity a row model assigns to a self-reported validation error. The WPF layer maps each
    /// value onto the cell badge's status (Info → informational, Warning → advisory, Error → blocking).
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>Informational — the value is accepted but the user should be aware of something.</summary>
        Info,

        /// <summary>Advisory — the value is questionable but not rejected.</summary>
        Warning,

        /// <summary>Blocking — the value is invalid. This is the default when no severity is supplied.</summary>
        Error,
    }
}
