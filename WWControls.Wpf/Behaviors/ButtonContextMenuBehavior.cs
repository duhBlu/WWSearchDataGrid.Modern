using System.Windows;
using System.Windows.Controls.Primitives;

namespace WWControls.Wpf.Behaviors
{
    /// <summary>
    /// Attached behavior that opens an element's <see cref="FrameworkElement.ContextMenu"/> on
    /// <see cref="ButtonBase.Click"/>. Lets a regular <c>Button</c> act as a dropdown trigger
    /// without writing code-behind: declare the menu under <c>Button.ContextMenu</c>, opt in via
    /// <c>behaviors:ButtonContextMenuBehavior.OpenOnClick="True"</c>, and items bind through the
    /// button's <see cref="FrameworkElement.DataContext"/> — the behavior copies it to the menu
    /// at open time so menu-item Command bindings resolve against the same view model.
    /// </summary>
    public static class ButtonContextMenuBehavior
    {
        public static readonly DependencyProperty OpenOnClickProperty =
            DependencyProperty.RegisterAttached(
                "OpenOnClick",
                typeof(bool),
                typeof(ButtonContextMenuBehavior),
                new PropertyMetadata(false, OnOpenOnClickChanged));

        public static bool GetOpenOnClick(DependencyObject obj) => (bool)obj.GetValue(OpenOnClickProperty);
        public static void SetOpenOnClick(DependencyObject obj, bool value) => obj.SetValue(OpenOnClickProperty, value);

        private static void OnOpenOnClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ButtonBase button)
            {
                if ((bool)e.NewValue)
                    button.Click += OnButtonClick;
                else
                    button.Click -= OnButtonClick;
            }
        }

        private static void OnButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.ContextMenu != null)
            {
                fe.ContextMenu.PlacementTarget = fe;
                fe.ContextMenu.Placement = PlacementMode.Bottom;
                // ContextMenu lives in a separate visual tree (Popup); DataContext doesn't inherit
                // from PlacementTarget on its own. Copy the button's DataContext only when the
                // menu doesn't already have one set explicitly (e.g. via TemplateBinding to a
                // templated parent) — overwriting an explicit setting would break those bindings.
                var dcSource = DependencyPropertyHelper.GetValueSource(fe.ContextMenu, FrameworkElement.DataContextProperty);
                if (dcSource.BaseValueSource == BaseValueSource.Default ||
                    dcSource.BaseValueSource == BaseValueSource.Inherited)
                {
                    fe.ContextMenu.DataContext = fe.DataContext;
                }
                fe.ContextMenu.IsOpen = true;
            }
        }
    }
}
