using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WWControls.SampleApp.Grid.Controls
{
    public partial class SampleFooterControl : UserControl
    {
        public SampleFooterControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty RowCountProperty = DependencyProperty.Register(
            nameof(RowCount), typeof(int), typeof(SampleFooterControl),
            new FrameworkPropertyMetadata(1000, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public int RowCount
        {
            get => (int)GetValue(RowCountProperty);
            set => SetValue(RowCountProperty, value);
        }

        public static readonly DependencyProperty GenerateCommandProperty = DependencyProperty.Register(
            nameof(GenerateCommand), typeof(ICommand), typeof(SampleFooterControl),
            new PropertyMetadata(null));

        public ICommand? GenerateCommand
        {
            get => (ICommand?)GetValue(GenerateCommandProperty);
            set => SetValue(GenerateCommandProperty, value);
        }

        public static readonly DependencyProperty ClearCommandProperty = DependencyProperty.Register(
            nameof(ClearCommand), typeof(ICommand), typeof(SampleFooterControl),
            new PropertyMetadata(null));

        public ICommand? ClearCommand
        {
            get => (ICommand?)GetValue(ClearCommandProperty);
            set => SetValue(ClearCommandProperty, value);
        }
    }
}
