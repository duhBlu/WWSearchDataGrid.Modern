using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WWControls.SampleApp.SampleData;
using WWControls.Wpf;

namespace WWControls.SampleApp.Controls
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

        public SampleHostControl()
        {
            // The status bar in the chrome shows the live row counts of the sample's grid. Samples
            // don't wire it up — discover the grid from the hosted content's logical tree once it's
            // loaded. A sample may set TrackedGrid explicitly to override (e.g. to pick one of several).
            Loaded += (_, _) =>
            {
                if (TrackedGrid == null)
                    TrackedGrid = FindLogicalDescendant<SearchDataGrid>(Content as DependencyObject);
            };
        }

        /// <summary>
        /// The grid whose row counts the chrome status bar reflects. Auto-discovered on load from the
        /// hosted content; settable in XAML to override when a sample hosts more than one grid.
        /// </summary>
        public static readonly DependencyProperty TrackedGridProperty = DependencyProperty.Register(
            nameof(TrackedGrid), typeof(SearchDataGrid), typeof(SampleHostControl),
            new PropertyMetadata(null, OnTrackedGridChanged));

        public SearchDataGrid TrackedGrid
        {
            get => (SearchDataGrid)GetValue(TrackedGridProperty);
            set => SetValue(TrackedGridProperty, value);
        }

        private static readonly DependencyPropertyKey HasTrackedGridPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(HasTrackedGrid), typeof(bool), typeof(SampleHostControl), new PropertyMetadata(false));

        /// <summary>True once a grid is being tracked — drives the status bar's visibility.</summary>
        public static readonly DependencyProperty HasTrackedGridProperty = HasTrackedGridPropertyKey.DependencyProperty;

        public bool HasTrackedGrid
        {
            get => (bool)GetValue(HasTrackedGridProperty);
            private set => SetValue(HasTrackedGridPropertyKey, value);
        }

        private static void OnTrackedGridChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SampleHostControl host)
                host.HasTrackedGrid = e.NewValue != null;
        }

        /// <summary>Depth-first logical-tree search for the first descendant of type <typeparamref name="T"/>.</summary>
        private static T FindLogicalDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            if (root == null) return null;
            foreach (var child in LogicalTreeHelper.GetChildren(root))
            {
                if (child is T match) return match;
                if (child is DependencyObject d && FindLogicalDescendant<T>(d) is T found)
                    return found;
            }
            return null;
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
