using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace WWControls.SampleApp.Grid.Models
{
    /// <summary>
    /// POCO with annotations driving auto-generation. Auto-gen should:
    ///   - Skip <c>InternalId</c> ([Browsable(false)])
    ///   - Skip <c>InternalNotes</c> ([Display(AutoGenerateField = false)])
    ///   - Use the <c>Display.Name</c> values as headers
    ///   - Order columns by <c>Display.Order</c>
    /// </summary>
    public sealed class Customer
    {
        [Browsable(false)]
        public int InternalId { get; set; }

        [Display(Name = "ID", Order = 0)]
        public int Id { get; set; }

        [Display(Name = "First Name", Order = 1)]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Last Name", Order = 2)]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Account #", Order = 3)]
        public string AccountNumber { get; set; } = string.Empty;

        [Display(Name = "Email", Order = 4)]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Joined", Order = 5)]
        public DateTime JoinedOn { get; set; }

        [Display(Name = "Active", Order = 6)]
        public bool IsActive { get; set; }

        [Display(Name = "Credit Limit", Order = 7)]
        public decimal CreditLimit { get; set; }

        [Display(AutoGenerateField = false)]
        public string? InternalNotes { get; set; }
    }
}
