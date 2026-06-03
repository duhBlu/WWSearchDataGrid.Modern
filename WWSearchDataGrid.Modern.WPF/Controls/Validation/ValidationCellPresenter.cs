using System;
using System.Windows;
using System.Windows.Controls;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Hosts a validated cell's display content alongside a <see cref="ValidationErrorIcon"/>
    /// badge. The grid sets this as a validated column's cell template (see
    /// <c>ColumnDataBase.BuildValidatingCellTemplate</c>): <see cref="ContentControl.Content"/> is
    /// the row item and <see cref="ContentControl.ContentTemplate"/> is the column's real display
    /// template, while <see cref="PropertyName"/> and <see cref="IsValidationEnabled"/> drive the
    /// badge.
    /// </summary>
    /// <remarks>
    /// The entire cell layout — badge gutter on the left, content filling the rest — lives in the
    /// default template (keyed <see cref="ThemeKeys.ValidationCellPresenter"/> in
    /// <c>Themes/Controls/Validation/ValidationCellPresenter.xaml</c>), so it can be retemplated in
    /// XAML without touching code.
    /// </remarks>
    public class ValidationCellPresenter : ContentControl
    {
        static ValidationCellPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ValidationCellPresenter),
                new FrameworkPropertyMetadata(typeof(ValidationCellPresenter)));
        }

        public ValidationCellPresenter()
        {
            // The hosted editor's value binding sets NotifyOnValidationError, so a failed
            // data-annotation rule raises Validation.Error, which bubbles up through this
            // presenter (the editor is a descendant of the templated ContentPresenter). Capture it
            // into EditorErrorMessage so the badge can show the in-progress invalid value — the
            // reflection path can't, because with commit-on-error off the failing rule blocks the
            // source update and the committed value stays valid.
            AddHandler(Validation.ErrorEvent, new EventHandler<ValidationErrorEventArgs>(OnEditorValidationError));
        }

        /// <summary>Read-only key for <see cref="EditorErrorMessage"/>.</summary>
        private static readonly DependencyPropertyKey EditorErrorMessagePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(EditorErrorMessage),
                typeof(string),
                typeof(ValidationCellPresenter),
                new PropertyMetadata(null));

        /// <summary>
        /// The current validation error message from the hosted editor's binding, or null when the
        /// editor has no error. Bound to the badge's <see cref="ValidationErrorIcon.OverrideErrorMessage"/>
        /// so a value the user typed but couldn't commit still raises the badge.
        /// </summary>
        public static readonly DependencyProperty EditorErrorMessageProperty =
            EditorErrorMessagePropertyKey.DependencyProperty;

        public string EditorErrorMessage => (string)GetValue(EditorErrorMessageProperty);

        private void OnEditorValidationError(object sender, ValidationErrorEventArgs e)
        {
            // Read the editor's current error set rather than tracking Added/Removed transitions:
            // re-validating an already-invalid binding (e.g. a second navigation attempt without a
            // fix) fires a Removed for the old error AND an Added for the new one in the same pass,
            // so a transition-based handler can land on the Removed and wrongly blank the badge.
            // The collection is post-event state, so the last event reflects reality either way.
            var target = e.OriginalSource as DependencyObject;
            string message = null;
            if (target != null)
            {
                foreach (var error in Validation.GetErrors(target))
                {
                    var text = error.ErrorContent?.ToString();
                    if (!string.IsNullOrEmpty(text))
                    {
                        message = text;
                        break;
                    }
                }
            }
            SetValue(EditorErrorMessagePropertyKey, message);
        }

        /// <summary>The property on the row item (the <see cref="FrameworkElement.DataContext"/>)
        /// that the badge validates.</summary>
        public static readonly DependencyProperty PropertyNameProperty =
            DependencyProperty.Register(
                nameof(PropertyName),
                typeof(string),
                typeof(ValidationCellPresenter),
                new PropertyMetadata(null));

        public string PropertyName
        {
            get => (string)GetValue(PropertyNameProperty);
            set => SetValue(PropertyNameProperty, value);
        }

        /// <summary>Whether the badge may show — bound to the column's resolved
        /// <c>ActualShowValidationAttributeErrors</c>.</summary>
        public static readonly DependencyProperty IsValidationEnabledProperty =
            DependencyProperty.Register(
                nameof(IsValidationEnabled),
                typeof(bool),
                typeof(ValidationCellPresenter),
                new PropertyMetadata(true));

        public bool IsValidationEnabled
        {
            get => (bool)GetValue(IsValidationEnabledProperty);
            set => SetValue(IsValidationEnabledProperty, value);
        }
    }
}
