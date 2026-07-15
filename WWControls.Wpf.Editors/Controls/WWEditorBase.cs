using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// Lookless base for the library's first-class editor controls. It carries only what every
    /// editor shares: the edited <see cref="Value"/>, <see cref="IsReadOnly"/>, the
    /// <see cref="ShowBorder"/> flag, focus-forwarding to the concrete input, and the in-cell
    /// self-flatten. Each concrete editor (<see cref="WWTextBox"/> and the spin / combo / date /
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
    public class WWEditorBase : Control
    {
        static WWEditorBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWEditorBase),
                new FrameworkPropertyMetadata(typeof(WWEditorBase)));
        }

        public WWEditorBase()
        {
            Loaded += OnBaseEditLoaded;
        }

        // Bordered by default (standalone use, the edit form); a grid cell provides its own
        // boundary, so the editor renders flat when hosted inside one. DataGridCell is a stock WPF
        // type, so this detection carries no dependency on the SearchDataGrid — the editor stays
        // grid-agnostic. Hosts that aren't cells but provide their own boundary (e.g. the row-edit
        // strip) opt in via the inherited FlattenEditors attached property instead. Loaded is the
        // first point the cell ancestor / inherited value are reliably in the tree; it can re-fire
        // on container recycling, and re-setting the same value is a no-op.
        private void OnBaseEditLoaded(object sender, RoutedEventArgs e)
        {
            if (GetFlattenEditors(this) || VisualTreeHelperMethods.FindVisualAncestor<DataGridCell>(this) != null)
            {
                ShowBorder = false;
                // Flat also means square: the chrome's rounded background would otherwise clip out
                // of the cell rectangle. Local value intentionally overrides the style's default.
                ControlHelper.SetCornerRadius(this, default);
            }
        }

        /// <summary>
        /// Attached, inherited flag a host sets on itself to render every editor in its subtree
        /// flat (<see cref="ShowBorder"/> cleared) — for hosts that draw their own input boundary
        /// but aren't a <see cref="DataGridCell"/>, like the grid's row-edit strip. Keeps the
        /// editors host-agnostic: the host declares the surface, the editor reacts.
        /// </summary>
        public static readonly DependencyProperty FlattenEditorsProperty =
            DependencyProperty.RegisterAttached("FlattenEditors", typeof(bool), typeof(WWEditorBase),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetFlattenEditors(DependencyObject obj) => (bool)obj.GetValue(FlattenEditorsProperty);

        public static void SetFlattenEditors(DependencyObject obj, bool value) => obj.SetValue(FlattenEditorsProperty, value);

        /// <summary>The edited value. Concrete editors give this their natural shape (text for <see cref="WWTextBox"/>).</summary>
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(object), typeof(WWEditorBase),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>Whether the editor blocks input. Concrete editors propagate this to their input element.</summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(WWEditorBase),
                new PropertyMetadata(false));

        /// <summary>
        /// Placeholder text shown while the editor is empty. A shared surface: concrete editors that
        /// host a text input render it as a watermark behind the input (see <see cref="WWTextBox"/>);
        /// editors with no free-text surface ignore it. Null / empty means "no watermark".
        /// </summary>
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register(nameof(Watermark), typeof(string), typeof(WWEditorBase),
                new PropertyMetadata(null));

        /// <summary>
        /// Whether the editor offers an inline clear affordance while it holds a value. A shared
        /// surface: editors that support it (see <see cref="WWTextBox"/>) render the button; others
        /// ignore it. Default <c>false</c> so the affordance is opt-in.
        /// </summary>
        public static readonly DependencyProperty ShowClearButtonProperty =
            DependencyProperty.Register(nameof(ShowClearButton), typeof(bool), typeof(WWEditorBase),
                new PropertyMetadata(false));

        /// <summary>
        /// Whether the chrome border draws. Defaults to <c>true</c> — a standalone editor and the
        /// edit form read as discrete bordered inputs, with an accent edge while focused. A grid
        /// cell flattens it (see <see cref="OnBaseEditLoaded"/>) and the filter row sets it false,
        /// so both surfaces render edge-to-edge.
        /// </summary>
        public static readonly DependencyProperty ShowBorderProperty =
            DependencyProperty.Register(nameof(ShowBorder), typeof(bool), typeof(WWEditorBase),
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

        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        public bool ShowClearButton
        {
            get => (bool)GetValue(ShowClearButtonProperty);
            set => SetValue(ShowClearButtonProperty, value);
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
