using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace WWSearchDataGrid.Modern.SampleApp.Models
{
    /// <summary>
    /// Each property holds the raw, unformatted value. The mask on the column applies the literal
    /// characters at display time and parses them back out on commit, so the model stays free of
    /// format-specific punctuation.
    /// </summary>
    public partial class Contact : ObservableObject
    {
        [ObservableProperty] private int _id;
        [ObservableProperty] private string _fullName = string.Empty;
        [ObservableProperty] private string _phone = string.Empty;
        [ObservableProperty] private string _ssn = string.Empty;
        [ObservableProperty] private string _zipPlus4 = string.Empty;
        [ObservableProperty] private string _licensePlate = string.Empty;
        [ObservableProperty] private string _accountNumber = string.Empty;
        [ObservableProperty] private DateTime _birthday;
        [ObservableProperty] private DateTime _lastSeen;
        [ObservableProperty] private DateTime _logged;
        [ObservableProperty] private DateTime _day;
        [ObservableProperty] private DateTime _iso;
        [ObservableProperty] private decimal _balance;
        [ObservableProperty] private decimal _discount;
        [ObservableProperty] private decimal _margin;
        [ObservableProperty] private TimeSpan _callDuration;
    }
}
