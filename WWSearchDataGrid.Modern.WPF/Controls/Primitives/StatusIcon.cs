using System.Windows;
using System.Windows.Controls;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// A small circular status badge — info / success / warning / error — whose entire visual and
    /// animation are defined in the default template (see
    /// <c>Themes/Controls/Primitives/StatusIcon.xaml</c>). The <see cref="Status"/> drives the
    /// color and glyph (and hides the badge when <see cref="StatusKind.None"/>); the
    /// <see cref="AnimationKind"/> selects how it animates when it appears; <see cref="Message"/>
    /// becomes the tooltip.
    /// </summary>
    /// <remarks>
    /// Purely declarative — there is no code-built visual, so the look and animations can be
    /// retemplated in XAML without touching this class. <see cref="ValidationErrorIcon"/> derives
    /// from it to drive <see cref="Status"/> / <see cref="Message"/> from a property's data
    /// annotations while reusing this template.
    /// </remarks>
    public class StatusIcon : Control
    {
        static StatusIcon()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(StatusIcon),
                new FrameworkPropertyMetadata(typeof(StatusIcon)));
        }

        /// <summary>The severity shown by the badge. <see cref="StatusKind.None"/> hides it.</summary>
        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(
                nameof(Status),
                typeof(StatusKind),
                typeof(StatusIcon),
                new PropertyMetadata(StatusKind.None));

        public StatusKind Status
        {
            get => (StatusKind)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        /// <summary>How the badge animates when it becomes visible. Defaults to
        /// <see cref="StatusIconAnimation.Blink"/>.</summary>
        public static readonly DependencyProperty AnimationKindProperty =
            DependencyProperty.Register(
                nameof(AnimationKind),
                typeof(StatusIconAnimation),
                typeof(StatusIcon),
                new PropertyMetadata(StatusIconAnimation.Blink));

        public StatusIconAnimation AnimationKind
        {
            get => (StatusIconAnimation)GetValue(AnimationKindProperty);
            set => SetValue(AnimationKindProperty, value);
        }

        /// <summary>Tooltip text describing the status (e.g. a validation error message).</summary>
        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register(
                nameof(Message),
                typeof(string),
                typeof(StatusIcon),
                new PropertyMetadata(null));

        public string Message
        {
            get => (string)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }
    }
}
