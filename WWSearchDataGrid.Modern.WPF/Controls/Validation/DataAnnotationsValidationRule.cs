using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
// ValidationRule.Validate returns the WPF type; the DataAnnotations type of the same name is
// used only for the results list below (fully qualified to avoid the collision).
using ValidationResult = System.Windows.Controls.ValidationResult;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// A <see cref="ValidationRule"/> that validates a proposed cell value against the bound
    /// property's <see cref="System.ComponentModel.DataAnnotations"/> validation attributes
    /// (<c>Required</c>, <c>Range</c>, <c>StringLength</c>, <c>RegularExpression</c>,
    /// <c>CustomValidation</c>, …). Backs Phase 2.2's data-annotation-aware error display: the
    /// grid attaches one to each editable cell binding via
    /// <see cref="BaseEditSettings.CreateValueBinding"/>.
    /// </summary>
    /// <remarks>
    /// Runs at <see cref="ValidationStep.ConvertedProposedValue"/> so the attributes see the value
    /// already coerced to the property's CLR type (a <c>[Range]</c> on a <c>decimal</c> needs a
    /// decimal, not the raw editor string). When the column resolves
    /// <see cref="ColumnDataBase.ActualShowValidationAttributeErrors"/> to <c>false</c>, the rule
    /// reports success so no error chrome appears. Validation against the full object instance —
    /// which lets cross-property <c>[CustomValidation]</c> work — needs the bound row item, so the
    /// rule pulls it from the <see cref="BindingExpression"/> owner overload.
    /// </remarks>
    internal sealed class DataAnnotationsValidationRule : ValidationRule
    {
        private readonly ColumnDataBase _column;
        private readonly string _propertyName;

        public DataAnnotationsValidationRule(ColumnDataBase column, string propertyName)
            : base(ValidationStep.ConvertedProposedValue, validatesOnTargetUpdated: false)
        {
            _column = column;
            _propertyName = propertyName;
        }

        /// <summary>
        /// The bound item isn't available from this overload — WPF calls the
        /// <see cref="BindingExpressionBase"/> overload below whenever it can, which is the path
        /// the cell binding takes. Returns valid here as a safe fallback.
        /// </summary>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
            => ValidationResult.ValidResult;

        public override ValidationResult Validate(object value, CultureInfo cultureInfo, BindingExpressionBase owner)
        {
            if (_column == null || string.IsNullOrEmpty(_propertyName))
                return ValidationResult.ValidResult;

            // Dotted paths don't map onto a single property's attribute set — skip them.
            if (_propertyName.IndexOf('.') >= 0)
                return ValidationResult.ValidResult;

            if (!_column.ActualShowValidationAttributeErrors)
                return ValidationResult.ValidResult;

            // Commit-on-error mode: the rule must NOT register a WPF binding error here. A failed
            // ValidationRule makes DataGrid.CommitEdit refuse to leave edit mode — that block sits
            // below the grid's own AllowCommit gate, so reporting failure would trap the cell in
            // edit even though the consumer asked to allow the commit. Report success and let the
            // value flow to the source; the cell's ValidationErrorIcon re-validates the committed
            // value by reflection and surfaces the error as an advisory badge.
            if (_column.View?.AllowCommitOnValidationAttributeError == true)
                return ValidationResult.ValidResult;

            object item = (owner as BindingExpression)?.DataItem;
            if (item == null)
                return ValidationResult.ValidResult;

            try
            {
                var context = new ValidationContext(item) { MemberName = _propertyName };
                var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                if (!Validator.TryValidateProperty(value, context, results))
                {
                    string message = results.FirstOrDefault(r => !string.IsNullOrEmpty(r.ErrorMessage))?.ErrorMessage
                                     ?? "The value is not valid.";
                    return new ValidationResult(false, message);
                }
            }
            catch (System.ArgumentException)
            {
                // TryValidateProperty throws when the value isn't assignable to the property type
                // — a conversion the binding hasn't completed. Let the binding's own conversion
                // error surface instead of masking it with a generic message.
                return ValidationResult.ValidResult;
            }

            return ValidationResult.ValidResult;
        }
    }
}
