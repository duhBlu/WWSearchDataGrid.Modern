using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Pins an element that lives inside horizontally-scrolling content to the viewport: the
    /// element is counter-translated by the ancestor <see cref="ScrollViewer"/>'s
    /// <c>HorizontalOffset</c> so it stays put while the content pans (the group-header rows
    /// clamp their inner grid to <c>ViewportWidth</c> and pin it with this). Owned in code
    /// because the equivalent XAML — a <c>RelativeSource</c> binding on a
    /// <see cref="TranslateTransform"/> — targets a <see cref="Freezable"/> with no governing
    /// FrameworkElement, which fails to resolve inside row templates.
    /// </summary>
    public static class ViewportPinBehavior
    {
        /// <summary>Set <c>true</c> on the element to pin. The element's RenderTransform is owned by the behavior.</summary>
        public static readonly DependencyProperty IsPinnedProperty =
            DependencyProperty.RegisterAttached(
                "IsPinned",
                typeof(bool),
                typeof(ViewportPinBehavior),
                new PropertyMetadata(false, OnIsPinnedChanged));

        public static bool GetIsPinned(DependencyObject obj) => (bool)obj.GetValue(IsPinnedProperty);

        public static void SetIsPinned(DependencyObject obj, bool value) => obj.SetValue(IsPinnedProperty, value);

        /// <summary>The ScrollViewer the pinned element is currently tracking.</summary>
        private static readonly DependencyProperty TrackedScrollViewerProperty =
            DependencyProperty.RegisterAttached(
                "TrackedScrollViewer",
                typeof(ScrollViewer),
                typeof(ViewportPinBehavior),
                new PropertyMetadata(null));

        private static void OnIsPinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element) return;

            if ((bool)e.NewValue)
            {
                element.Loaded += OnElementLoaded;
                element.Unloaded += OnElementUnloaded;
                if (element.IsLoaded) Attach(element);
            }
            else
            {
                element.Loaded -= OnElementLoaded;
                element.Unloaded -= OnElementUnloaded;
                Detach(element);
            }
        }

        private static void OnElementLoaded(object sender, RoutedEventArgs e)
            => Attach((FrameworkElement)sender);

        private static void OnElementUnloaded(object sender, RoutedEventArgs e)
            => Detach((FrameworkElement)sender);

        private static void Attach(FrameworkElement element)
        {
            // Recycled containers reload under the same ScrollViewer — re-attach is a no-op
            // apart from re-syncing the offset.
            var current = (ScrollViewer)element.GetValue(TrackedScrollViewerProperty);
            var scrollViewer = FindAncestorScrollViewer(element);
            if (!ReferenceEquals(current, scrollViewer))
            {
                Detach(element);
                if (scrollViewer == null) return;

                element.SetValue(TrackedScrollViewerProperty, scrollViewer);
                scrollViewer.ScrollChanged += OnScrollChangedFor(element);
            }
            Sync(element);
        }

        private static void Detach(FrameworkElement element)
        {
            if (element.GetValue(TrackedScrollViewerProperty) is ScrollViewer scrollViewer)
            {
                scrollViewer.ScrollChanged -= OnScrollChangedFor(element);
                element.ClearValue(TrackedScrollViewerProperty);
            }
        }

        /// <summary>Per-element handler delegate, cached so detach removes the same instance attach added.</summary>
        private static readonly DependencyProperty ScrollHandlerProperty =
            DependencyProperty.RegisterAttached(
                "ScrollHandler",
                typeof(ScrollChangedEventHandler),
                typeof(ViewportPinBehavior),
                new PropertyMetadata(null));

        private static ScrollChangedEventHandler OnScrollChangedFor(FrameworkElement element)
        {
            if (element.GetValue(ScrollHandlerProperty) is ScrollChangedEventHandler handler)
                return handler;

            handler = (_, args) =>
            {
                if (args.HorizontalChange != 0 || args.ViewportWidthChange != 0 || args.ExtentWidthChange != 0)
                    Sync(element);
            };
            element.SetValue(ScrollHandlerProperty, handler);
            return handler;
        }

        private static void Sync(FrameworkElement element)
        {
            if (element.GetValue(TrackedScrollViewerProperty) is not ScrollViewer scrollViewer) return;

            if (element.RenderTransform is not TranslateTransform transform || transform.IsFrozen)
                element.RenderTransform = transform = new TranslateTransform();
            transform.X = scrollViewer.HorizontalOffset;
        }

        private static ScrollViewer FindAncestorScrollViewer(DependencyObject start)
        {
            var d = VisualTreeHelper.GetParent(start);
            while (d != null)
            {
                if (d is ScrollViewer sv) return sv;
                d = VisualTreeHelper.GetParent(d);
            }
            return null;
        }
    }
}
