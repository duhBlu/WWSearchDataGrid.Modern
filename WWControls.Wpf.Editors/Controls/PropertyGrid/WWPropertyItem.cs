using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using WWControls.Wpf.Editors.Settings;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// One property row in <see cref="WWPropertyGrid"/>. Acts as the <c>DataContext</c> for editor
    /// templates: it exposes <see cref="Value"/> for two-way binding and the effective metadata
    /// (name, category, description, order, read-only, visibility) the row and description panel show.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Metadata is resolved live from four sources in precedence order — the matched
    /// <see cref="WWPropertyDefinition"/>'s bindable override (A) &gt; an
    /// <see cref="IPropertyMetadataProvider"/> override (B) &gt; the static attribute &gt; the default.
    /// The properties raise <see cref="INotifyPropertyChanged"/>, so a definition binding that flips
    /// (mechanism A) or a provider that signals a change (mechanism B) updates the row without
    /// reassigning <c>SelectedObject</c>.
    /// </para>
    /// <para>
    /// Implements <see cref="IEditorColumn"/> so the shared <c>BaseEditorSettings</c> editor stack
    /// (the same one the SearchDataGrid uses) can build a typed editor for the row: the settings read
    /// the item as a grid-agnostic column and bind the editor straight to the model property via
    /// <see cref="IEditorColumn.CreateFieldBinding"/>. <see cref="IEditorColumn.Host"/> is null because
    /// a property row has no data-grid host.
    /// </para>
    /// </remarks>
    public class WWPropertyItem : INotifyPropertyChanged, IDisposable, IEditorColumn
    {
        private readonly object _source;
        private readonly PropertyInfo _propertyInfo;
        private readonly Dispatcher _dispatcher;
        private object _cachedValue;
        private bool _disposed;

        // The matched definition (mechanism A) and the current provider override (mechanism B).
        private readonly WWPropertyDefinition _definition;
        private PropertyMetadataOverride _overrides;

        // Static, attribute-resolved values (with defaults baked in) — the third precedence tier.
        private readonly string _attrDisplayName;
        private readonly string _attrDescription;
        private readonly string _attrCategory;
        private readonly int _attrOrder;
        private readonly bool _attrReadOnly;
        private readonly bool _attrBrowsable;

        // Grid-level validation context, pushed by the grid (SetValidationContext); the effective
        // per-row toggle resolves the definition override against the grid-level default.
        private bool _gridShowValidationErrors = true;
        private bool _gridAllowCommitOnValidationError;

        /// <summary>
        /// <param name="source">The object that owns the property.</param>
        /// <param name="propertyInfo">Reflection info for the property.</param>
        /// <param name="overrides">
        /// Optional runtime overrides from <see cref="IPropertyMetadataProvider"/> (mechanism B).
        /// </param>
        /// <param name="definition">
        /// Optional matched <see cref="WWPropertyDefinition"/> whose bindable overrides drive
        /// mechanism A. The item subscribes to its <see cref="WWPropertyDefinition.MetadataChanged"/>
        /// so changes reflect live.
        /// </param>
        /// </summary>
        public WWPropertyItem(object source, PropertyInfo propertyInfo,
            PropertyMetadataOverride overrides = null, WWPropertyDefinition definition = null)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _propertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));
            _overrides = overrides;
            _definition = definition;

            PropertyName = _propertyInfo.Name;
            PropertyType = _propertyInfo.PropertyType;

            // ── Static attribute tier (defaults baked in) ──────────────────────────────────────
            // DataAnnotations [Display] carries name / group / order / description in one attribute
            // and is preferred over the older System.ComponentModel attributes when it supplies a
            // given facet (each Get* returns null when that facet is unset, so the fallbacks flow).
            var display = _propertyInfo.GetCustomAttribute<DisplayAttribute>();

            var displayNameAttr = _propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
            _attrDisplayName = display?.GetName() ?? displayNameAttr?.DisplayName ?? PropertyName;

            var descAttr = _propertyInfo.GetCustomAttribute<DescriptionAttribute>();
            _attrDescription = display?.GetDescription() ?? descAttr?.Description ?? string.Empty;

            var catAttr = _propertyInfo.GetCustomAttribute<CategoryAttribute>();
            _attrCategory = display?.GroupName ?? catAttr?.Category ?? "Misc.";

            _attrOrder = display?.GetOrder() ?? ReadPropertyOrder(_propertyInfo);

            var editableAttr = _propertyInfo.GetCustomAttribute<EditableAttribute>();
            var readOnlyAttr = _propertyInfo.GetCustomAttribute<ReadOnlyAttribute>();
            _attrReadOnly = (editableAttr != null && !editableAttr.AllowEdit)
                || (readOnlyAttr != null && readOnlyAttr.IsReadOnly)
                || !_propertyInfo.CanWrite;

            var browsableAttr = _propertyInfo.GetCustomAttribute<BrowsableAttribute>();
            _attrBrowsable = browsableAttr?.Browsable ?? true;

            // Enum support
            var underlying = Nullable.GetUnderlyingType(PropertyType) ?? PropertyType;
            if (underlying.IsEnum)
            {
                EnumValues = Enum.GetValues(underlying);
            }

            // Cache the initial value and capture the UI dispatcher. Guarded because the grid now
            // builds an item for every property (visibility is a runtime filter, not a build-time
            // skip), so a property whose getter throws must not abort the whole rebuild.
            try { _cachedValue = _propertyInfo.GetValue(_source); }
            catch { _cachedValue = null; }
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            // Resolve the effective metadata from all tiers, then wire the live sources.
            RecomputeMetadata();

            if (_definition != null)
                _definition.MetadataChanged += OnDefinitionMetadataChanged;

            if (_source is INotifyPropertyChanged npc)
                npc.PropertyChanged += Source_PropertyChanged;
        }

        #region Properties

        /// <summary>The CLR property name on the source object.</summary>
        public string PropertyName { get; }

        /// <summary>
        /// The object that owns the property (the edited model). The validation badge validates this
        /// object — it carries the data-annotation attributes / <c>INotifyDataErrorInfo</c> errors,
        /// distinct from this item, which is the row's visual DataContext.
        /// </summary>
        public object Source => _source;

        /// <summary>Effective display label (definition &gt; provider &gt; <c>[Display]</c>/<c>[DisplayName]</c> &gt; property name).</summary>
        public string DisplayName
        {
            get => _displayName;
            private set { if (_displayName != value) { _displayName = value; OnPropertyChanged(nameof(DisplayName)); } }
        }
        private string _displayName;

        /// <summary>Effective tooltip / description-panel text (definition &gt; provider &gt; <c>[Display]</c>/<c>[Description]</c> &gt; empty).</summary>
        public string Description
        {
            get => _description;
            private set { if (_description != value) { _description = value; OnPropertyChanged(nameof(Description)); } }
        }
        private string _description;

        /// <summary>Effective category group (definition &gt; provider &gt; <c>[Display]</c>/<c>[Category]</c> &gt; "Misc.").</summary>
        public string Category
        {
            get => _category;
            private set { if (_category != value) { _category = value; OnPropertyChanged(nameof(Category)); } }
        }
        private string _category;

        /// <summary>Effective sort order within a category (definition &gt; provider &gt; <c>[Display]</c>/<c>[PropertyOrder]</c> &gt; last).</summary>
        public int PropertyOrder
        {
            get => _propertyOrder;
            private set { if (_propertyOrder != value) { _propertyOrder = value; OnPropertyChanged(nameof(PropertyOrder)); } }
        }
        private int _propertyOrder;

        /// <summary>Effective read-only state (definition &gt; provider &gt; <c>[Editable]</c>/<c>[ReadOnly]</c>/no-setter &gt; false).</summary>
        public bool IsReadOnly
        {
            get => _isReadOnly;
            private set { if (_isReadOnly != value) { _isReadOnly = value; OnPropertyChanged(nameof(IsReadOnly)); } }
        }
        private bool _isReadOnly;

        /// <summary>
        /// Effective visibility (definition &gt; provider <c>Browsable</c> &gt; <c>[Browsable]</c> &gt; true).
        /// The grid's collection view filters on this, so flipping it shows / hides the row live.
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            private set { if (_isVisible != value) { _isVisible = value; OnPropertyChanged(nameof(IsVisible)); } }
        }
        private bool _isVisible = true;

        /// <summary>
        /// Effective "show validation errors" toggle for this row: the matched
        /// <see cref="WWPropertyDefinition.ShowValidationErrors"/> override, else the grid-level
        /// <c>WWPropertyGrid.ShowValidationErrors</c>. Bound by the row's validation presenter as
        /// <c>IsValidationEnabled</c>, and read by the shared <c>DataAnnotationsValidationRule</c>.
        /// </summary>
        public bool ActualShowValidationErrors
        {
            get => _actualShowValidationErrors;
            private set { if (_actualShowValidationErrors != value) { _actualShowValidationErrors = value; OnPropertyChanged(nameof(ActualShowValidationErrors)); } }
        }
        private bool _actualShowValidationErrors = true;

        /// <summary>The CLR type of the property.</summary>
        public Type PropertyType { get; }

        /// <summary>For enum properties, the set of possible values. Null otherwise.</summary>
        public Array EnumValues { get; }

        /// <summary>
        /// A fully custom editor template (from a <see cref="WWPropertyDefinition.EditTemplate"/> or a
        /// legacy <see cref="WWEditorDefinition"/>), or null when the editor comes from
        /// <see cref="EditSettings"/> / the type default. Wins over <see cref="EditSettings"/>.
        /// </summary>
        public DataTemplate EditorTemplate { get; set; }

        /// <summary>
        /// The resolved editor settings for this row — from a matched <see cref="WWPropertyDefinition"/>,
        /// a <c>[PropertyGridEditor]</c> / <c>[DefaultEditor]</c> attribute, or the CLR type default.
        /// Null when no editor could be resolved (the row falls back to the read-only placeholder).
        /// The grid resolves this once per rebuild; the editor selector builds the row's template from it.
        /// </summary>
        public BaseEditorSettings EditSettings { get; set; }

        /// <summary>
        /// Callback invoked after a value is written through the editor. The parent grid uses it to
        /// refresh sibling properties (so dependent/derived values update even when the source
        /// doesn't implement <see cref="INotifyPropertyChanged"/>).
        /// </summary>
        internal Action ValueWritten;

        /// <summary>True when this item is the selected row in the property grid.</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }
        private bool _isSelected;

        /// <summary>
        /// The property value. Two-way: reads via <see cref="PropertyInfo.GetValue(object)"/>, writes
        /// via <see cref="PropertyInfo.SetValue(object,object)"/>. Editor templates bind to this with
        /// <c>{Binding Value, Mode=TwoWay}</c>.
        ///
        /// Backed by a cached field so WPF reliably detects changes regardless of which thread raises
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/> on the source.
        /// </summary>
        public object Value
        {
            get => _cachedValue;
            set
            {
                if (!_propertyInfo.CanWrite || IsReadOnly)
                    return;

                try
                {
                    var converted = ConvertValue(value, _propertyInfo.PropertyType);
                    _propertyInfo.SetValue(_source, converted);
                    // Re-read in case the setter coerced/transformed the value
                    _cachedValue = _propertyInfo.GetValue(_source);
                    OnPropertyChanged(nameof(Value));
                    ValueWritten?.Invoke();
                }
                catch
                {
                    // Ignore conversion failures — an invalid edit leaves the last good value in place.
                }
            }
        }

        #endregion

        #region Live metadata

        /// <summary>
        /// Recomputes every effective-metadata property from its four tiers in precedence order:
        /// the definition's bindable override (A) &gt; the provider override (B) &gt; the static
        /// attribute &gt; the default. The property setters raise <see cref="INotifyPropertyChanged"/>
        /// only for the fields that actually changed.
        /// </summary>
        private void RecomputeMetadata()
        {
            DisplayName = _definition?.DisplayName ?? _overrides?.DisplayName ?? _attrDisplayName;
            Description = _definition?.Description ?? _overrides?.Description ?? _attrDescription;
            Category = _definition?.Category ?? _overrides?.Category ?? _attrCategory;
            PropertyOrder = _definition?.PropertyOrder ?? _overrides?.PropertyOrder ?? _attrOrder;
            IsReadOnly = _definition?.IsReadOnly ?? _overrides?.IsReadOnly ?? _attrReadOnly;
            IsVisible = _definition?.IsVisible ?? _overrides?.Browsable ?? _attrBrowsable;
            // The validation toggle has no provider tier — a definition override, else the grid level.
            ActualShowValidationErrors = _definition?.ShowValidationErrors ?? _gridShowValidationErrors;
        }

        /// <summary>
        /// Pushes the grid-level validation context onto the row and recomputes the effective toggle.
        /// The commit gate has no per-definition tier, so it is grid-level as-is.
        /// </summary>
        internal void SetValidationContext(bool showValidationErrors, bool allowCommitOnValidationError)
        {
            _gridShowValidationErrors = showValidationErrors;
            _gridAllowCommitOnValidationError = allowCommitOnValidationError;
            ActualShowValidationErrors = _definition?.ShowValidationErrors ?? _gridShowValidationErrors;
        }

        /// <summary>
        /// Replaces the provider (mechanism B) override and recomputes. Called by the grid when an
        /// <see cref="IObservablePropertyMetadataProvider"/> signals a change for this property.
        /// </summary>
        internal void SetMetadataOverride(PropertyMetadataOverride overrides)
        {
            _overrides = overrides;
            RecomputeMetadata();
        }

        private void OnDefinitionMetadataChanged(object sender, EventArgs e)
        {
            if (_dispatcher.CheckAccess())
                RecomputeMetadata();
            else
                _dispatcher.BeginInvoke(new Action(RecomputeMetadata));
        }

        #endregion

        #region IEditorColumn

        // Presents the row to the shared BaseEditorSettings editor stack as a grid-agnostic column.
        // The editor binds straight to the model property (Source = the owning object, Path = the
        // property name), so a settings-built editor edits the model exactly like a grid cell —
        // while the reflection-backed Value stays available for legacy custom templates. Members are
        // explicit so they don't widen WWPropertyItem's public surface; the settings reach them
        // through the IEditorColumn reference they already receive.

        string IEditorColumn.FieldName => PropertyName;

        BindingBase IEditorColumn.Binding => null;

        string IEditorColumn.DisplayStringFormat => null;

        IValueConverter IEditorColumn.DisplayValueConverter => null;

        object IEditorColumn.DisplayConverterParameter => null;

        string IEditorColumn.DisplayMask => null;

        TextAlignment IEditorColumn.TextAlignment => TextAlignment.Left;

        // The shared DataAnnotationsValidationRule (attached by the settings-built editor's value
        // binding) reads these: it no-ops when errors are off, and reports success — letting the
        // value commit while the badge shows it advisory-style — when commit-on-error is on.
        bool IEditorColumn.ActualShowValidationAttributeErrors => ActualShowValidationErrors;

        bool IEditorColumn.AllowCommitOnValidationError => _gridAllowCommitOnValidationError;

        // A get-only source property (no setter) can't take a two-way editor binding — the property
        // grid always materializes its editor, so the value binding is forced one-way for these.
        bool IEditorColumn.IsValueReadOnly => !_propertyInfo.CanWrite;

        // A property row has no data-grid host — this is the seam that keeps grid-cell wiring
        // (arrow-exit, mouse-caret, commit gating) from attaching in the property grid.
        IEditingGridHost IEditorColumn.Host => null;

        Binding IEditorColumn.CreateFieldBinding() => new Binding(PropertyName) { Source = _source };

        #endregion

        #region Private Methods

        /// <summary>
        /// Reads <c>[PropertyOrder(n)]</c> by attribute name so the item carries no dependency on the
        /// assembly that defines the attribute. Matches any attribute named "PropertyOrderAttribute"
        /// with an "Order" property.
        /// </summary>
        private static int ReadPropertyOrder(PropertyInfo prop)
        {
            foreach (var attr in prop.GetCustomAttributes(true))
            {
                var attrType = attr.GetType();
                if (attrType.Name == "PropertyOrderAttribute")
                {
                    var orderProp = attrType.GetProperty("Order");
                    if (orderProp != null)
                    {
                        return (int)orderProp.GetValue(attr);
                    }
                }
            }
            return int.MaxValue; // unordered properties sort last
        }

        private static object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlying.IsInstanceOfType(value))
                return value;

            if (underlying.IsEnum)
            {
                if (value is string s)
                    return Enum.Parse(underlying, s);
                return Enum.ToObject(underlying, value);
            }

            return Convert.ChangeType(value, underlying);
        }

        private void Source_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == PropertyName)
            {
                RefreshValue();
            }
        }

        /// <summary>
        /// Re-reads the property value from the source and raises <see cref="Value"/> changed on the
        /// UI thread when it actually changed.
        /// </summary>
        internal void RefreshValue()
        {
            object newValue;
            try { newValue = _propertyInfo.GetValue(_source); }
            catch { return; }

            // For reference types, instance identity matters — bindings target whichever object is in
            // the cache. A source that hands back a fresh wrapper whose values equal the cached one
            // would be skipped by value-comparison, leaving bindings pointed at the stale instance, so
            // reference types compare by identity and value types by equality.
            var underlying = Nullable.GetUnderlyingType(PropertyType) ?? PropertyType;
            bool unchanged = underlying.IsValueType || underlying == typeof(string)
                ? Equals(_cachedValue, newValue)
                : ReferenceEquals(_cachedValue, newValue);

            if (unchanged)
                return;

            _cachedValue = newValue;

            if (_dispatcher.CheckAccess())
            {
                OnPropertyChanged(nameof(Value));
            }
            else
            {
                _dispatcher.BeginInvoke(new Action(() => OnPropertyChanged(nameof(Value))));
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        internal void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_source is INotifyPropertyChanged npc)
                {
                    npc.PropertyChanged -= Source_PropertyChanged;
                }
                if (_definition != null)
                {
                    _definition.MetadataChanged -= OnDefinitionMetadataChanged;
                }
                _disposed = true;
            }
        }

        #endregion
    }
}
