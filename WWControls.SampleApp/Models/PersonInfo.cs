using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using WWControls.Core.Validation;

namespace WWControls.SampleApp.Models
{
    /// <summary>
    /// Editable model for the Data Error Indication sample. Unlike <see cref="ValidationData"/> —
    /// which leans on reflection over data-annotation attributes — this row <b>self-reports</b> its
    /// errors: deriving from <see cref="ObservableValidator"/> makes it an
    /// <see cref="System.ComponentModel.INotifyDataErrorInfo"/> source, and the grid reads errors
    /// straight off that interface. Each validated property carries <c>[NotifyDataErrorInfo]</c> so
    /// the generated setter re-validates on edit and raises <c>ErrorsChanged</c>.
    /// </summary>
    /// <remarks>
    /// Implementing <see cref="IValidationSeverityProvider"/> is what turns the single red "invalid"
    /// badge into three distinct indicators. <see cref="GetSeverity"/> is consulted only once an
    /// error already exists for a property, so it answers "how loudly" rather than "whether":
    /// missing names are blocking <see cref="ValidationSeverity.Error"/>, a missing address or e-mail
    /// is an advisory <see cref="ValidationSeverity.Info"/>, and a badly formatted phone or e-mail is
    /// a <see cref="ValidationSeverity.Warning"/>. The e-mail case shows the provider inspecting its
    /// own state to vary severity by condition (empty → Info, malformed → Warning).
    /// </remarks>
    public sealed partial class PersonInfo : ObservableValidator, IValidationSeverityProvider
    {
        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "First Name can't be empty.")]
        [Display(Name = "First Name", Order = 0)]
        private string _firstName;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Last Name can't be empty.")]
        [Display(Name = "Last Name", Order = 1)]
        private string _lastName;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Address hasn't been entered.")]
        [Display(Name = "Address", Order = 2)]
        private string _address;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [RegularExpression(@"^\(\d{3}\) \d{3}-\d{4}$",
            ErrorMessage = "Phone should look like (206) 555-0100.")]
        [Display(Name = "Phone", Order = 3)]
        private string _phone;

        [ObservableProperty]
        [NotifyDataErrorInfo]
        [Required(ErrorMessage = "Email hasn't been entered.")]
        [EmailAddress(ErrorMessage = "This doesn't look like a valid e-mail address.")]
        [Display(Name = "Email", Order = 4)]
        private string _email;

        public PersonInfo(string firstName, string lastName, string address, string phone, string email)
        {
            // Assign the backing fields directly so construction doesn't validate field-by-field,
            // then validate the whole object once — that seeds GetErrors so badges are visible on
            // load (the setters re-validate per-property from here on).
            _firstName = firstName;
            _lastName = lastName;
            _address = address;
            _phone = phone;
            _email = email;
            ValidateAllProperties();
        }

        /// <inheritdoc />
        public ValidationSeverity GetSeverity(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(FirstName):
                case nameof(LastName):
                    return ValidationSeverity.Error;

                case nameof(Address):
                    return ValidationSeverity.Info;

                case nameof(Phone):
                    return ValidationSeverity.Warning;

                case nameof(Email):
                    // A missing e-mail is only advisory; a malformed one is worth a warning.
                    return string.IsNullOrWhiteSpace(Email)
                        ? ValidationSeverity.Info
                        : ValidationSeverity.Warning;

                default:
                    return ValidationSeverity.Error;
            }
        }
    }
}
