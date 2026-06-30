using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using WWControls.Core.Validation;

namespace WWControls.Wpf
{
    /// <summary>
    /// A <see cref="StatusIcon"/> that drives its <see cref="StatusIcon.Status"/> and
    /// <see cref="StatusIcon.Message"/> from a property's <see cref="System.ComponentModel.DataAnnotations"/>
    /// validation attributes. Backs the display-mode half of Phase 2.2 — it surfaces validation
    /// errors on data that's already loaded (not just while editing), reusing the
    /// <see cref="StatusIcon"/> template for its visual and (blink) animation.
    /// </summary>
    /// <remarks>
    /// The grid drops one onto each validated column's display template (see
    /// <c>ColumnDataBase.CreateDataGridColumn</c>), binding <see cref="Item"/> to the row item and
    /// <see cref="IsValidationEnabled"/> to the column's resolved
    /// <c>ActualShowValidationAttributeErrors</c>. It re-validates whenever those inputs change and
    /// whenever the row item raises <see cref="INotifyPropertyChanged"/> for the watched property,
    /// then sets <see cref="StatusIcon.Status"/> to <see cref="StatusKind.Error"/> (or
    /// <see cref="StatusKind.None"/>) — the template handles showing, hiding, and animating.
    /// </remarks>
    public sealed class ValidationErrorIcon : StatusIcon
    {
        static ValidationErrorIcon()
        {
            // Reuse StatusIcon's default style / template rather than looking for one keyed to
            // this subclass.
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ValidationErrorIcon),
                new FrameworkPropertyMetadata(typeof(StatusIcon)));
        }

        private INotifyPropertyChanged _observed;
        private INotifyDataErrorInfo _observedErrors;

        public ValidationErrorIcon()
        {
            Loaded += (_, __) => { HookItem(Item); Revalidate(); };
            Unloaded += (_, __) => UnhookItem();
        }

        /// <summary>The row item whose property is validated. Bound to the cell's data context.</summary>
        public static readonly DependencyProperty ItemProperty =
            DependencyProperty.Register(nameof(Item), typeof(object), typeof(ValidationErrorIcon),
                new PropertyMetadata(null, OnItemChanged));

        public object Item
        {
            get => GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        /// <summary>The property name on <see cref="Item"/> to validate.</summary>
        public static readonly DependencyProperty PropertyNameProperty =
            DependencyProperty.Register(nameof(PropertyName), typeof(string), typeof(ValidationErrorIcon),
                new PropertyMetadata(null, OnInputChanged));

        public string PropertyName
        {
            get => (string)GetValue(PropertyNameProperty);
            set => SetValue(PropertyNameProperty, value);
        }

        /// <summary>
        /// When false the badge never shows, regardless of validity. Bound to the column's
        /// resolved <c>ActualShowValidationAttributeErrors</c> so the grid/column toggle hides it.
        /// </summary>
        public static readonly DependencyProperty IsValidationEnabledProperty =
            DependencyProperty.Register(nameof(IsValidationEnabled), typeof(bool), typeof(ValidationErrorIcon),
                new PropertyMetadata(true, OnInputChanged));

        public bool IsValidationEnabled
        {
            get => (bool)GetValue(IsValidationEnabledProperty);
            set => SetValue(IsValidationEnabledProperty, value);
        }

        /// <summary>
        /// An error message supplied from outside the reflection path — set by the hosting
        /// <see cref="ValidationCellPresenter"/> from the live editor's WPF validation error while
        /// a cell is being edited. Takes precedence over <see cref="ResolveErrorMessage"/> when
        /// non-null: with commit-on-error off the failing rule blocks the source update, so the
        /// committed value the reflection path sees is still the old valid one — this is how the
        /// badge reflects the invalid value the user just typed. Null falls back to reflection.
        /// </summary>
        public static readonly DependencyProperty OverrideErrorMessageProperty =
            DependencyProperty.Register(nameof(OverrideErrorMessage), typeof(string), typeof(ValidationErrorIcon),
                new PropertyMetadata(null, OnInputChanged));

        public string OverrideErrorMessage
        {
            get => (string)GetValue(OverrideErrorMessageProperty);
            set => SetValue(OverrideErrorMessageProperty, value);
        }

        private static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var icon = (ValidationErrorIcon)d;
            icon.UnhookItem();
            icon.HookItem(e.NewValue);
            icon.Revalidate();
        }

        private static void OnInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((ValidationErrorIcon)d).Revalidate();

        private void HookItem(object item)
        {
            if (item is INotifyPropertyChanged inpc)
            {
                _observed = inpc;
                _observed.PropertyChanged += OnObservedPropertyChanged;
            }

            // A self-reporting model recomputes its own errors and signals via ErrorsChanged —
            // which can fire without a matching PropertyChanged (e.g. an object-level rule
            // re-evaluating). Hook it so the badge refreshes on the model's own schedule.
            if (item is INotifyDataErrorInfo nde)
            {
                _observedErrors = nde;
                _observedErrors.ErrorsChanged += OnObservedErrorsChanged;
            }
        }

        private void UnhookItem()
        {
            if (_observed != null)
            {
                _observed.PropertyChanged -= OnObservedPropertyChanged;
                _observed = null;
            }

            if (_observedErrors != null)
            {
                _observedErrors.ErrorsChanged -= OnObservedErrorsChanged;
                _observedErrors = null;
            }
        }

        private void OnObservedPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Empty name means "many properties changed" — revalidate to be safe.
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == PropertyName)
                Revalidate();
        }

        private void OnObservedErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            // Empty name means "errors changed across the object" — revalidate to be safe.
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == PropertyName)
                Revalidate();
        }

        private void Revalidate()
        {
            // A live editor error (OverrideErrorMessage) wins over the committed-value reflection
            // check; both are gated by IsValidationEnabled so the grid/column toggle still hides
            // the badge entirely.
            string message = IsValidationEnabled
                ? (OverrideErrorMessage ?? ResolveErrorMessage())
                : null;
            Message = message;
            Status = message != null ? ResolveSeverity() : StatusKind.None;
        }

        /// <summary>
        /// Resolves the badge's severity once an error message exists. Defaults to
        /// <see cref="StatusKind.Error"/>; a model that opts into <see cref="IValidationSeverityProvider"/>
        /// can downgrade a property's badge to a warning or informational tone.
        /// </summary>
        private StatusKind ResolveSeverity()
        {
            if (Item is IValidationSeverityProvider provider && !string.IsNullOrEmpty(PropertyName))
                return ToStatusKind(provider.GetSeverity(PropertyName));
            return StatusKind.Error;
        }

        private static StatusKind ToStatusKind(ValidationSeverity severity)
        {
            switch (severity)
            {
                case ValidationSeverity.Info: return StatusKind.Info;
                case ValidationSeverity.Warning: return StatusKind.Warning;
                default: return StatusKind.Error;
            }
        }

        private string ResolveErrorMessage()
        {
            object item = Item;
            string propertyName = PropertyName;
            if (!IsValidationEnabled || item == null || string.IsNullOrEmpty(propertyName))
                return null;

            // Base layer: the model self-reports via INotifyDataErrorInfo (CommunityToolkit's
            // ObservableValidator, a hand-rolled implementation, etc.). When the item implements it,
            // its report is authoritative — including "no error" — so we don't also run the
            // attribute reflection below and risk two sources disagreeing.
            if (item is INotifyDataErrorInfo nde)
            {
                foreach (var error in nde.GetErrors(propertyName))
                {
                    var text = error?.ToString();
                    if (!string.IsNullOrEmpty(text))
                        return text;
                }
                return null;
            }

            return ResolveErrorMessageFromAttributes(item, propertyName);
        }

        /// <summary>
        /// Fallback error source for items that don't implement <see cref="INotifyDataErrorInfo"/>:
        /// validates the committed value against the property's data-annotation attributes by
        /// reflection.
        /// </summary>
        private static string ResolveErrorMessageFromAttributes(object item, string propertyName)
        {
            var prop = item.GetType().GetProperty(propertyName);
            if (prop == null)
                return null;

            object value = prop.GetValue(item);
            var context = new System.ComponentModel.DataAnnotations.ValidationContext(item) { MemberName = propertyName };
            var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            if (System.ComponentModel.DataAnnotations.Validator.TryValidateProperty(value, context, results))
                return null;

            return results.FirstOrDefault(r => !string.IsNullOrEmpty(r.ErrorMessage))?.ErrorMessage
                   ?? "The value is not valid.";
        }
    }
}
