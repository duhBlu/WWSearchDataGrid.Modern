namespace WWSearchDataGrid.Modern.Core.Validation
{
    /// <summary>
    /// Optional companion to <see cref="System.ComponentModel.INotifyDataErrorInfo"/>: a row model
    /// implements this only when it wants a property's error rendered as something other than the
    /// default <see cref="ValidationSeverity.Error"/> — for instance an advisory
    /// <see cref="ValidationSeverity.Warning"/> or an informational <see cref="ValidationSeverity.Info"/>
    /// badge. Models that don't implement it still get error badges from
    /// <see cref="System.ComponentModel.INotifyDataErrorInfo"/> or data-annotation attributes.
    /// </summary>
    public interface IValidationSeverityProvider
    {
        /// <summary>
        /// Returns the severity for the current error on <paramref name="propertyName"/>. Only
        /// consulted once an error message already exists for that property, so a model never has to
        /// re-decide whether the property is in error — only how loudly to surface it.
        /// </summary>
        ValidationSeverity GetSeverity(string propertyName);
    }
}
