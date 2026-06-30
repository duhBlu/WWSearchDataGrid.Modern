using System.Windows;
using System.Windows.Controls;

namespace WWControls.SampleApp.Controls
{
    public partial class SampleLoadingOverlay : UserControl
    {
        public SampleLoadingOverlay()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
            nameof(IsBusy), typeof(bool), typeof(SampleLoadingOverlay),
            new PropertyMetadata(false));

        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }

        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
            nameof(Status), typeof(string), typeof(SampleLoadingOverlay),
            new PropertyMetadata(string.Empty));

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }
    }
}
