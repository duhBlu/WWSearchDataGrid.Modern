using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// One property row in <see cref="WWPropertyGrid"/>. Acts as the <c>DataContext</c> for editor
    /// templates: it exposes <see cref="Value"/> for two-way binding and the reflected metadata
    /// (name, category, description, order, read-only) the row and description panel display.
    /// </summary>
    public class WWPropertyItem : INotifyPropertyChanged, IDisposable
    {
        private readonly object _source;
        private readonly PropertyInfo _propertyInfo;
        private readonly Dispatcher _dispatcher;
        private object _cachedValue;
        private bool _disposed;

        /// <summary>
        /// <param name="source">The object that owns the property.</param>
        /// <param name="propertyInfo">Reflection info for the property.</param>
        /// <param name="overrides">
        /// Optional runtime overrides from <see cref="IPropertyMetadataProvider"/>.
        /// Non-null fields take precedence over static attributes.
        /// </param>
        /// </summary>
        public WWPropertyItem(object source, PropertyInfo propertyInfo, PropertyMetadataOverride overrides = null)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _propertyInfo = propertyInfo ?? throw new ArgumentNullException(nameof(propertyInfo));

            PropertyName = _propertyInfo.Name;
            PropertyType = _propertyInfo.PropertyType;

            // DisplayName — override > [DisplayName] > property name
            var displayAttr = _propertyInfo.GetCustomAttribute<DisplayNameAttribute>();
            DisplayName = overrides?.DisplayName
                ?? (displayAttr != null ? displayAttr.DisplayName : PropertyName);

            // Description — override > [Description] > empty
            var descAttr = _propertyInfo.GetCustomAttribute<DescriptionAttribute>();
            Description = overrides?.Description
                ?? (descAttr != null ? descAttr.Description : string.Empty);

            // Category — override > [Category] > "Misc."
            var catAttr = _propertyInfo.GetCustomAttribute<CategoryAttribute>();
            Category = overrides?.Category
                ?? (catAttr != null ? catAttr.Category : "Misc.");

            // Order — override > [PropertyOrder] > last
            PropertyOrder = overrides?.PropertyOrder ?? ReadPropertyOrder(_propertyInfo);

            // ReadOnly — override > [ReadOnly] > no setter
            var readOnlyAttr = _propertyInfo.GetCustomAttribute<ReadOnlyAttribute>();
            bool attrReadOnly = (readOnlyAttr != null && readOnlyAttr.IsReadOnly) || !_propertyInfo.CanWrite;
            IsReadOnly = overrides?.IsReadOnly ?? attrReadOnly;

            // Enum support
            var underlying = Nullable.GetUnderlyingType(PropertyType) ?? PropertyType;
            if (underlying.IsEnum)
            {
                EnumValues = Enum.GetValues(underlying);
            }

            // Cache the initial value and capture the UI dispatcher
            _cachedValue = _propertyInfo.GetValue(_source);
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

            // Listen for source changes
            if (_source is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += Source_PropertyChanged;
            }
        }

        #region Properties

        /// <summary>The CLR property name on the source object.</summary>
        public string PropertyName { get; }

        /// <summary>Display label from the <c>[DisplayName]</c> attribute (or the property name).</summary>
        public string DisplayName { get; }

        /// <summary>Tooltip / description-panel text from the <c>[Description]</c> attribute.</summary>
        public string Description { get; }

        /// <summary>Category group from the <c>[Category]</c> attribute (or "Misc.").</summary>
        public string Category { get; }

        /// <summary>Sort order within a category from the <c>[PropertyOrder]</c> attribute.</summary>
        public int PropertyOrder { get; }

        /// <summary>True if <c>[ReadOnly(true)]</c> or the property has no setter.</summary>
        public bool IsReadOnly { get; }

        /// <summary>The CLR type of the property.</summary>
        public Type PropertyType { get; }

        /// <summary>For enum properties, the set of possible values. Null otherwise.</summary>
        public Array EnumValues { get; }

        /// <summary>Custom editor template from <see cref="WWEditorDefinition"/>, or null for the default.</summary>
        public DataTemplate EditorTemplate { get; set; }

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
            var newValue = _propertyInfo.GetValue(_source);

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
                _disposed = true;
            }
        }

        #endregion
    }
}
