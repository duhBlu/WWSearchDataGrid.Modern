using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using WWControls.Core.Validation;

namespace WWControls.SampleApp.Editors.Views.Samples.PropertyGrid
{
    /// <summary>
    /// Backs the "Validation" property-grid sample. The model carries data-annotation attributes and
    /// self-reports through <c>INotifyDataErrorInfo</c> (via <see cref="ObservableValidator"/>); one
    /// property is downgraded to a warning through <see cref="IValidationSeverityProvider"/>. The
    /// grid's <c>ShowValidationErrors</c> and <c>AllowCommitOnValidationError</c> are bound to toggles.
    /// </summary>
    public partial class PropertyGridValidationSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private ValidationOrder _order = new ValidationOrder();

        [ObservableProperty]
        private bool _showValidationErrors = true;

        [ObservableProperty]
        private bool _allowCommitOnValidationError;
    }

    /// <summary>
    /// A model that both carries data-annotation attributes and self-reports validity through
    /// <c>INotifyDataErrorInfo</c> (<see cref="ObservableValidator"/> validates on set). It also
    /// implements <see cref="IValidationSeverityProvider"/> to render the stock-level rule as an
    /// advisory warning rather than a blocking error.
    /// </summary>
    public class ValidationOrder : ObservableValidator, IValidationSeverityProvider
    {
        // Starting values are deliberately invalid so the grid opens with its badges already
        // showing (ValidateAllProperties in the ctor fills the error set on load): a length error,
        // an email-format error, a range error, one advisory warning (StockLevel below its soft
        // floor), and one valid field for contrast.
        private string _customerName = "A";
        private string _email = "acme.com";
        private int _quantity = 750;
        private int _stockLevel = 4;
        private string _discountCode = "SAVE10";

        public ValidationOrder()
        {
            // Populate the INotifyDataErrorInfo error set from the initial values so a badge shows
            // immediately if a starting value is invalid.
            ValidateAllProperties();
        }

        [Category("Customer")]
        [DisplayName("Customer Name")]
        [Description("Required, 3–20 characters.")]
        [Required(ErrorMessage = "Customer name is required.")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Name must be 3–20 characters.")]
        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value, validate: true);
        }

        [Category("Customer")]
        [DisplayName("Email")]
        [Description("Required, must be a valid email address.")]
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value, validate: true);
        }

        [Category("Order")]
        [DisplayName("Quantity")]
        [Description("Must be between 1 and 500.")]
        [Range(1, 500, ErrorMessage = "Quantity must be 1–500.")]
        public int Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value, validate: true);
        }

        [Category("Order")]
        [DisplayName("Stock Level")]
        [Description("Advisory floor of 10 — surfaces as a warning, not a blocking error.")]
        [Range(10, 1000, ErrorMessage = "Stock is below the advisory floor of 10.")]
        public int StockLevel
        {
            get => _stockLevel;
            set => SetProperty(ref _stockLevel, value, validate: true);
        }

        [Category("Order")]
        [DisplayName("Discount Code")]
        [Description("Optional, up to 8 characters.")]
        [StringLength(8, ErrorMessage = "Discount code is at most 8 characters.")]
        public string DiscountCode
        {
            get => _discountCode;
            set => SetProperty(ref _discountCode, value, validate: true);
        }

        /// <summary>Stock level is a soft floor — render its error as a warning; everything else blocks.</summary>
        public ValidationSeverity GetSeverity(string propertyName)
            => propertyName == nameof(StockLevel) ? ValidationSeverity.Warning : ValidationSeverity.Error;
    }
}
