using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Lookless base for the library's first-class editor controls. Owns the shared editor
    /// chrome in one place: the outer border (flat by default, a 1px edge with an accent on
    /// focus when <see cref="ShowBorder"/> is set), background, padding, the disabled visual,
    /// a <c>PART_ContentHost</c> site that presents the concrete input
    /// (<see cref="EditContent"/>), and a decoration-button slot
    /// (<see cref="ButtonContent"/> / <see cref="ShowButtons"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Concrete editors (<see cref="WWTextEdit"/>, and the spin / combo / date / check editors
    /// that follow) derive from this type, point their default style key at
    /// <see cref="WWBaseEdit"/> so they reuse this one chrome template, and fill
    /// <see cref="EditContent"/> with their input element. Because the border lives here and
    /// nowhere else, every editor reads consistently — bordered when hosted standalone or in
    /// the edit form, flat in a grid cell — without each editor repeating the border triggers.
    /// </para>
    /// <para>
    /// These controls carry <em>no</em> reference to the grid. Cell-interaction concerns
    /// (arrow-key cell exit, mouse-click caret, decoration-button visibility) live in the
    /// grid-side editor host; an editor only raises normal input events and exposes its input
    /// element.
    /// </para>
    /// </remarks>
    public class WWBaseEdit : Control
    {
        static WWBaseEdit()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWBaseEdit),
                new FrameworkPropertyMetadata(typeof(WWBaseEdit)));
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
        /// Whether the chrome border draws. Flat (<c>false</c>) is the in-cell / filter-row look;
        /// a host that wants the editor to read as a discrete bordered input (the edit form, a
        /// standalone form) sets this true, and the border gains an accent edge while focused.
        /// </summary>
        public static readonly DependencyProperty ShowBorderProperty =
            DependencyProperty.Register(nameof(ShowBorder), typeof(bool), typeof(WWBaseEdit),
                new PropertyMetadata(false));

        /// <summary>Whether the decoration-button slot (<see cref="ButtonContent"/>) is shown.</summary>
        public static readonly DependencyProperty ShowButtonsProperty =
            DependencyProperty.Register(nameof(ShowButtons), typeof(bool), typeof(WWBaseEdit),
                new PropertyMetadata(false));

        /// <summary>
        /// Content of the decoration-button slot rendered at the trailing edge of the chrome
        /// (combo toggle, spinner, calendar dropdown). Visible only while <see cref="ShowButtons"/>
        /// is true. Plain text editors leave this null.
        /// </summary>
        public static readonly DependencyProperty ButtonContentProperty =
            DependencyProperty.Register(nameof(ButtonContent), typeof(object), typeof(WWBaseEdit),
                new PropertyMetadata(null));

        /// <summary>
        /// The concrete input element, presented inside the chrome at <c>PART_ContentHost</c>.
        /// Set by the derived editor (e.g. <see cref="WWTextEdit"/> assigns its TextBox); not
        /// intended as a consumer-facing knob.
        /// </summary>
        public static readonly DependencyProperty EditContentProperty =
            DependencyProperty.Register(nameof(EditContent), typeof(object), typeof(WWBaseEdit),
                new PropertyMetadata(null));

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

        public bool ShowButtons
        {
            get => (bool)GetValue(ShowButtonsProperty);
            set => SetValue(ShowButtonsProperty, value);
        }

        public object ButtonContent
        {
            get => GetValue(ButtonContentProperty);
            set => SetValue(ButtonContentProperty, value);
        }

        /// <summary>The concrete input element hosted inside the chrome. Assigned by derived editors.</summary>
        protected object EditContent
        {
            get => GetValue(EditContentProperty);
            set => SetValue(EditContentProperty, value);
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
