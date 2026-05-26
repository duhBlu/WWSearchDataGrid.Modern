using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Base for chip controls that swap to an inline editor on click. Currently only
    /// <see cref="ValueTokenEditor"/> uses this — the column and search-type chips open a popup
    /// menu of choices instead of entering an edit state. Provides the display-vs-editor swap
    /// driven by <see cref="UIElement.IsKeyboardFocusWithin"/>; chip background lives on the
    /// derived control's XAML template, display text lives on a dependency property here.
    /// </summary>
    public class EditableTokenBase : Control
    {
        public static readonly DependencyProperty DisplayTextProperty =
            DependencyProperty.Register(nameof(DisplayText), typeof(string), typeof(EditableTokenBase),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty IsEditingProperty =
            DependencyProperty.Register(nameof(IsEditing), typeof(bool), typeof(EditableTokenBase),
                new PropertyMetadata(false));

        public string DisplayText
        {
            get => (string)GetValue(DisplayTextProperty);
            set => SetValue(DisplayTextProperty, value);
        }

        /// <summary>
        /// Whether the chip is currently showing its editor template. Normally driven by the
        /// control template via <see cref="UIElement.IsKeyboardFocusWithin"/>; the setter is kept
        /// public so unit tests and external triggers can flip it explicitly.
        /// </summary>
        public bool IsEditing
        {
            get => (bool)GetValue(IsEditingProperty);
            set => SetValue(IsEditingProperty, value);
        }

        static EditableTokenBase()
        {
            FocusableProperty.OverrideMetadata(typeof(EditableTokenBase),
                new FrameworkPropertyMetadata(true));
        }

        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            // Clicking anywhere on the chip enters edit mode and focuses the chip so
            // IsKeyboardFocusWithin = true makes the editor portion of the template visible.
            // The editor controls inside (ComboBox / TextBox / etc.) handle their own focus
            // once they're visible; LostKeyboardFocus on the chip resets the flag.
            if (!IsEditing)
            {
                IsEditing = true;
                Dispatcher.BeginInvoke(new System.Action(() => MoveFocusInto()));
            }
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);
            // Only collapse back to display mode when focus leaves the entire chip subtree;
            // IsKeyboardFocusWithin lags by a beat so check the new focus target instead.
            if (!IsKeyboardFocusWithin)
            {
                IsEditing = false;
            }
        }

        private void MoveFocusInto()
        {
            if (!IsKeyboardFocusWithin)
            {
                MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }
        }
    }
}
