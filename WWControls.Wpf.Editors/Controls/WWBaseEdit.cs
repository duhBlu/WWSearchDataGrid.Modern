using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// Lookless base for the library's first-class editor controls. It carries only what every
    /// editor shares: the edited <see cref="Value"/>, <see cref="IsReadOnly"/>, the
    /// <see cref="ShowBorder"/> flag, focus-forwarding to the concrete input, and the in-cell
    /// self-flatten. Each concrete editor (<see cref="WWTextEdit"/> and the spin / combo / date /
    /// check editors) owns its own default style, control template, border, and named parts — the
    /// base supplies no shared chrome template.
    /// </summary>
    /// <remarks>
    /// These controls carry <em>no</em> reference to the grid. Cell-interaction concerns (arrow-key
    /// cell exit, mouse-click caret, decoration-button visibility) live in the grid-side editor
    /// host; an editor only raises normal input events and exposes its input element. Bordered by
    /// default (standalone use, the edit form), an editor renders flat inside a grid cell —
    /// <see cref="OnBaseEditLoaded"/> clears <see cref="ShowBorder"/> when it detects a stock
    /// <see cref="System.Windows.Controls.DataGridCell"/> ancestor.
    /// </remarks>
    public class WWBaseEdit : Control
    {
        static WWBaseEdit()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWBaseEdit),
                new FrameworkPropertyMetadata(typeof(WWBaseEdit)));
        }

        public WWBaseEdit()
        {
            Loaded += OnBaseEditLoaded;
        }

        // Bordered by default (standalone use, the edit form); a grid cell provides its own
        // boundary, so the editor renders flat when hosted inside one. DataGridCell is a stock WPF
        // type, so this detection carries no dependency on the SearchDataGrid — the editor stays
        // grid-agnostic. Loaded is the first point the cell ancestor is reliably in the tree; it can
        // re-fire on container recycling, and re-setting the same value is a no-op.
        private void OnBaseEditLoaded(object sender, RoutedEventArgs e)
        {
            if (VisualTreeHelperMethods.FindVisualAncestor<DataGridCell>(this) != null)
                ShowBorder = false;
        }

        /// <summary>The edited value. Concrete editors give this their natural shape (text for <see cref="WWTextEdit"/>).</summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(object), typeof(WWBaseEdit),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>Whether the editor blocks input. Concrete editors propagate this to their input element.</summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(WWBaseEdit),
                new PropertyMetadata(false));

        /// <summary>
        /// Whether the chrome border draws. Defaults to <c>true</c> — a standalone editor and the
        /// edit form read as discrete bordered inputs, with an accent edge while focused. A grid
        /// cell flattens it (see <see cref="OnBaseEditLoaded"/>) and the filter row sets it false,
        /// so both surfaces render edge-to-edge.
        /// </summary>
        public static readonly DependencyProperty ShowBorderProperty =
            DependencyProperty.Register(nameof(ShowBorder), typeof(bool), typeof(WWBaseEdit),
                new PropertyMetadata(true));

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public bool IsReadOnly
        {
            get => (bool)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public bool ShowBorder
        {
            get => (bool)GetValue(ShowBorderProperty);
            set => SetValue(ShowBorderProperty, value);
        }

        /// <summary>
        /// The element focus should land on when the editor itself is focused (e.g. a host calls
        /// <c>Keyboard.Focus(editor)</c>, or the user tabs to it). Derived editors return their
        /// input element; the chrome control is not itself the input. Null means "no inner target",
        /// and focus stays on the control.
        /// </summary>
        protected virtual IInputElement FocusTarget => null;

        /// <summary>
        /// Forwards focus from the chrome control to its input element, so the editor reads as a
        /// single focusable unit anywhere a host hands it focus — a grid cell, the filter row, or a
        /// standalone form — without the host needing to know the input lives one level in.
        /// </summary>
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            if (ReferenceEquals(e.NewFocus, this) && FocusTarget != null)
                Keyboard.Focus(FocusTarget);
        }
    }
}
