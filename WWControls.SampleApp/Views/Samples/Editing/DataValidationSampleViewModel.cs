using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WWControls.SampleApp.Models;
using WWControls.SampleApp.SampleData;

namespace WWControls.SampleApp.Views.Samples.Editing
{
    /// <summary>
    /// Backs the Data Validation sample. Seeds an editable collection of <see cref="ValidationData"/>
    /// rows — deliberately mixing valid rows with ones that break each rule (missing Required name,
    /// Title over 30 chars, Salary above 1,000,000, malformed zip / e-mail, future hire date) so
    /// the animated error badges are visible on load. Edit a flagged cell to a good value and its
    /// badge clears immediately; break a good cell and a badge appears.
    /// </summary>
    public sealed partial class DataValidationSampleViewModel : SampleViewModelBase
    {
        [ObservableProperty]
        private ObservableCollection<ValidationData> _employees = new();

        public DataValidationSampleViewModel()
        {
            StartLoad(() =>
            {
                Employees = new ObservableCollection<ValidationData>
                {
                    // Valid row — no badges.
                    new() { FirstName = "Nancy", LastName = "Davolio", Title = "Sales Representative", Status = EmploymentStatus.Active, HireDate = new DateTime(2019, 5, 1), Salary = 52000m, ZipCode = "98122", Phone = "(206) 555-9857", Email = "nancy@example.com" },

                    // Missing first name (Required) + 4-digit zip (RegularExpression).
                    new() { FirstName = "", LastName = "Fuller", Title = "VP, Sales", Status = EmploymentStatus.Active, HireDate = new DateTime(2015, 8, 14), Salary = 138000m, ZipCode = "9840", Phone = "(206) 555-9482", Email = "andrew@example.com" },

                    // Title over 30 chars (StringLength) + salary over 1,000,000 (Range).
                    new() { FirstName = "Janet", LastName = "Leverling", Title = "Senior Regional Sales Representative, West", Status = EmploymentStatus.OnLeave, HireDate = new DateTime(2021, 4, 1), Salary = 1500000m, ZipCode = "98033", Phone = "(206) 555-3412", Email = "janet@example.com" },

                    // Malformed e-mail (EmailAddress) + future hire date (CustomValidation).
                    new() { FirstName = "Margaret", LastName = "Peacock", Title = "Sales Representative", Status = EmploymentStatus.Active, HireDate = new DateTime(2027, 1, 1), Salary = 56000m, ZipCode = "98052", Phone = "(206) 555-8122", Email = "margaret(at)example.com" },

                    // Valid row — no badges.
                    new() { FirstName = "Steven", LastName = "Buchanan", Title = "Sales Manager", Status = EmploymentStatus.Terminated, HireDate = new DateTime(2017, 10, 17), Salary = 95000m, ZipCode = "98004", Phone = "(206) 555-4848", Email = "steven@example.com" },
                };
                return Task.CompletedTask;
            });
        }
    }
}
