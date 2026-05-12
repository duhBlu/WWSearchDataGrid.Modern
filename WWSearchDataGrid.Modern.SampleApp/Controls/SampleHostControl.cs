using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WWSearchDataGrid.Modern.SampleApp.SampleData;

namespace WWSearchDataGrid.Modern.SampleApp.Controls
{
    /// <summary>
    /// Two-tab shell wrapping a sample's live UI alongside a combobox-driven source viewer. The
    /// implicit content goes into <see cref="ContentControl.Content"/> (the Live tab); the Source
    /// tab reads <see cref="Sources"/> and switches the editor's contents and syntax highlighting
    /// based on which file the user picks. Subclassed from <see cref="ContentControl"/> rather
    /// than <c>UserControl</c> so the control's template lives in a Style and doesn't introduce a
    /// separate NameScope — consumer's <c>x:Name</c>'d elements inside the Content register with
    /// the consumer's NameScope as expected.
    /// </summary>
    public class SampleHostControl : ContentControl
    {
        static SampleHostControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SampleHostControl),
                new FrameworkPropertyMetadata(typeof(SampleHostControl)));
        }

        /// <summary>
        /// The list of source files the user can switch between (XAML, code-behind, view model,
        /// related models / generators). Set once per sample via
        /// <c>{x:Static controls:SampleSources.SomeSample}</c>.
        /// </summary>
        public static readonly DependencyProperty SourcesProperty = DependencyProperty.Register(
            nameof(Sources), typeof(IReadOnlyList<SampleSourceFile>), typeof(SampleHostControl),
            new PropertyMetadata(null, OnSourcesChanged));

        public IReadOnlyList<SampleSourceFile> Sources
        {
            get => (IReadOnlyList<SampleSourceFile>)GetValue(SourcesProperty);
            set => SetValue(SourcesProperty, value);
        }

        /// <summary>
        /// The file currently displayed in the Source tab. Two-way bound from the combobox; default
        /// is the first entry in <see cref="Sources"/>.
        /// </summary>
        public static readonly DependencyProperty SelectedSourceProperty = DependencyProperty.Register(
            nameof(SelectedSource), typeof(SampleSourceFile), typeof(SampleHostControl),
            new PropertyMetadata(null));

        public SampleSourceFile SelectedSource
        {
            get => (SampleSourceFile)GetValue(SelectedSourceProperty);
            set => SetValue(SelectedSourceProperty, value);
        }

        private static void OnSourcesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SampleHostControl host) return;
            // Default-select the first file so the source editor shows something on first paint.
            host.SelectedSource = (e.NewValue as IReadOnlyList<SampleSourceFile>)?.FirstOrDefault();
        }
    }
}
