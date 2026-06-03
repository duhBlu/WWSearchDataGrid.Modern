using System;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using WWSearchDataGrid.Modern.Core.DataAnnotations;

namespace WWSearchDataGrid.Modern.SampleApp.Models
{
    /// <summary>Employment status — drives the smart ComboBox editor on the Status column.</summary>
    public enum EmploymentStatus
    {
        Active,
        OnLeave,
        Terminated,
    }

    /// <summary>
    /// Editable model for the Data Validation sample. Carries two kinds of annotations:
    /// <list type="bullet">
    ///   <item><b>Validation</b> — standard <see cref="System.ComponentModel.DataAnnotations"/>
    ///   attributes (<c>Required</c>, <c>StringLength</c>, <c>Range</c>, <c>RegularExpression</c>,
    ///   <c>EmailAddress</c>, <c>CustomValidation</c>). Smart columns evaluate these both at rest
    ///   (the cell shows an animated error badge) and while editing (the edit is blocked unless
    ///   commit-on-error is allowed).</item>
    ///   <item><b>Smart layout</b> — <see cref="DisplayAttribute"/> headers/order plus the
    ///   library's mask / editor attributes (<see cref="NumericMaskAttribute"/>,
    ///   <see cref="DateTimeMaskAttribute"/>, <see cref="SimpleMaskAttribute"/>,
    ///   <see cref="GridEditorAttribute"/>). Columns flagged <c>IsSmart</c> read these to pick
    ///   their editor, mask, and header automatically.</item>
    /// </list>
    /// Implemented as an <see cref="ObservableObject"/> so the error badge re-evaluates the moment
    /// an in-grid edit changes a value. Attributes are forwarded onto the generated properties via
    /// <c>[property: …]</c>.
    /// </summary>
    public sealed partial class ValidationData : ObservableObject
    {
        [ObservableProperty]
        [property: Display(Name = "First Name", Order = 0)]
        [property: Required]
        private string _firstName = string.Empty;

        [ObservableProperty]
        [property: Display(Name = "Last Name", Order = 1)]
        [property: Required]
        private string _lastName = string.Empty;

        [ObservableProperty]
        [property: Display(Name = "Title", Order = 2)]
        [property: StringLength(30, ErrorMessage = "The {0} field cannot exceed {1} characters.")]
        private string _title = string.Empty;

        [ObservableProperty]
        [property: Display(Name = "Status", Order = 3)]
        [property: GridEditor(EditorKind.ComboBox)]
        private EmploymentStatus _status;

        [ObservableProperty]
        [property: Display(Name = "Hire Date", Order = 4)]
        [property: DateTimeMask("MM/dd/yyyy")]
        [property: CustomValidation(typeof(ValidationData), nameof(ValidateHireDate))]
        private DateTime _hireDate;

        [ObservableProperty]
        [property: Display(Name = "Salary", Order = 5)]
        [property: DataType(DataType.Currency)]
        [property: NumericMask("C2")]
        [property: Range(typeof(decimal), "0", "1000000",
            ErrorMessage = "The {0} field must be between {1:C0} and {2:C0}.")]
        private decimal _salary;

        [ObservableProperty]
        [property: Display(Name = "Zip Code", Order = 6)]
        [property: RegularExpression(@"^\d{5}$", ErrorMessage = "The {0} field must be a 5-digit zip code.")]
        private string _zipCode = string.Empty;

        [ObservableProperty]
        [property: Display(Name = "Phone", Order = 7)]
        [property: SimpleMask("(000) 000-0000")]
        private string _phone = string.Empty;

        [ObservableProperty]
        [property: Display(Name = "Email", Order = 8)]
        [property: EmailAddress(ErrorMessage = "The {0} field is not a valid e-mail address.")]
        private string _email = string.Empty;

        /// <summary>
        /// Custom validation for <see cref="HireDate"/> — a hire date may not be in the future.
        /// Referenced by the <see cref="CustomValidationAttribute"/> on the property.
        /// </summary>
        public static ValidationResult ValidateHireDate(DateTime date, ValidationContext context)
        {
            return date <= DateTime.Today
                ? ValidationResult.Success
                : new ValidationResult("The Hire Date field cannot be in the future.",
                    new[] { context?.MemberName ?? nameof(HireDate) });
        }
    }
}
