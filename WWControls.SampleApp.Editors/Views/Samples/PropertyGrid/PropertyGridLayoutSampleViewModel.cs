using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using WWControls.Wpf.Controls.Editors;

namespace WWControls.SampleApp.Editors.Views.Samples.PropertyGrid
{
    /// <summary>
    /// Backs the "Layout" property-grid sample: one model shown twice — once by a grid whose
    /// grid-wide <see cref="WWPropertyGrid.HeaderShowMode"/> flips live from a picker, and once by a
    /// grid that leaves the default (Left) but overrides HeaderShowMode per property through
    /// <see cref="WWPropertyDefinition.HeaderShowMode"/>.
    /// </summary>
    public partial class PropertyGridLayoutSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private LayoutProfile _profile = new LayoutProfile();

        /// <summary>Drives the grid-wide default on the left-hand grid.</summary>
        [ObservableProperty]
        private PropertyHeaderShowMode _selectedHeaderMode = PropertyHeaderShowMode.Left;

        /// <summary>The four header modes, offered in the picker.</summary>
        public PropertyHeaderShowMode[] AvailableHeaderModes { get; } =
        {
            PropertyHeaderShowMode.Left,
            PropertyHeaderShowMode.Top,
            PropertyHeaderShowMode.Hidden,
            PropertyHeaderShowMode.OnlyHeader,
        };
    }

    public enum LayoutDepartment { Engineering, Sales, Support, Operations }

    /// <summary>
    /// A small profile model. Its <c>Biography</c> is long enough that a top-placed header reads
    /// better than a left one; <c>SectionCaption</c> is a read-only label that suits OnlyHeader; and
    /// <c>InternalToken</c> is the kind of field a form hides outright.
    /// </summary>
    public class LayoutProfile : INotifyPropertyChanged
    {
        private string _fullName = "Dana Whitfield";
        private string _title = "Product Designer";
        private LayoutDepartment _department = LayoutDepartment.Engineering;
        private bool _isPublic = true;
        private string _biography =
            "Fifteen years shipping desktop tooling. Leads the layout-consistency guild and keeps the " +
            "component library honest across screen resolutions.";
        private string _internalToken = "tok_9f83b1";

        [Display(Name = "About", GroupName = "Profile", Order = 0,
            Description = "A short section label for the profile block.")]
        public string SectionCaption { get; } = string.Empty;

        [Display(Name = "Full Name", GroupName = "Profile", Order = 1,
            Description = "The name shown on the public profile card.")]
        public string FullName
        {
            get => _fullName;
            set => Set(ref _fullName, value);
        }

        [Display(Name = "Title", GroupName = "Profile", Order = 2,
            Description = "Job title displayed under the name.")]
        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }

        [Display(Name = "Department", GroupName = "Profile", Order = 3,
            Description = "Owning department.")]
        public LayoutDepartment Department
        {
            get => _department;
            set => Set(ref _department, value);
        }

        [Display(Name = "Public Profile", GroupName = "Profile", Order = 4,
            Description = "Whether this profile is visible to customers.")]
        public bool IsPublic
        {
            get => _isPublic;
            set => Set(ref _isPublic, value);
        }

        [Display(Name = "Biography", GroupName = "Profile", Order = 5,
            Description = "A longer free-text blurb shown on the profile card.")]
        public string Biography
        {
            get => _biography;
            set => Set(ref _biography, value);
        }

        [Display(Name = "Internal Token", GroupName = "Profile", Order = 6,
            Description = "An internal-only identifier used by integrations.")]
        public string InternalToken
        {
            get => _internalToken;
            set => Set(ref _internalToken, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
