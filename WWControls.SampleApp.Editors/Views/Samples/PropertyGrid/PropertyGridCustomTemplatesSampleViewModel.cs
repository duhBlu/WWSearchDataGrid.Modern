using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WWControls.SampleApp.Editors.Views.Samples.PropertyGrid
{
    /// <summary>
    /// Backs the "Custom templates" property-grid sample: a <c>WWPropertyDefinition.EditTemplate</c>
    /// supplies a fully custom editor (a slider, a multiline box) for chosen properties, winning over
    /// the auto editor. The template's DataContext is the property item, so it binds
    /// <c>{Binding Value}</c>.
    /// </summary>
    public partial class PropertyGridCustomTemplatesSampleViewModel : ObservableObject
    {
        [ObservableProperty]
        private TemplatesProduct _product = new TemplatesProduct();
    }

    /// <summary>
    /// A model whose numeric / text properties are edited by custom templates in the view (a rating
    /// slider, a discount slider, a multiline notes box); the remaining properties fall back to the
    /// auto editors, so the sample shows custom and auto editors side by side.
    /// </summary>
    public class TemplatesProduct : INotifyPropertyChanged
    {
        private string _name = "Shaker Base Cabinet";
        private bool _isFeatured = true;
        private int _rating = 4;
        private double _discount = 0.15;
        private string _notes = "Ships flat-packed. Assembly required.";

        [Category("General")]
        [DisplayName("Product Name")]
        [Description("Auto text editor — no definition.")]
        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        [Category("General")]
        [DisplayName("Featured")]
        [Description("Auto checkbox editor — no definition.")]
        public bool IsFeatured
        {
            get => _isFeatured;
            set => Set(ref _isFeatured, value);
        }

        [Category("Presentation")]
        [DisplayName("Rating")]
        [Description("Custom EditTemplate — a 0–5 snap slider bound to Value.")]
        public int Rating
        {
            get => _rating;
            set => Set(ref _rating, value);
        }

        [Category("Pricing")]
        [DisplayName("Discount")]
        [Description("Custom EditTemplate — a 0–100% slider bound to Value.")]
        public double Discount
        {
            get => _discount;
            set => Set(ref _discount, value);
        }

        [Category("Presentation")]
        [DisplayName("Notes")]
        [Description("Custom EditTemplate — a multiline, wrapping text box.")]
        public string Notes
        {
            get => _notes;
            set => Set(ref _notes, value);
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
