using System.Windows;
using System.Windows.Data;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Base class for column editor configurations. Each concrete implementation produces a
    /// <see cref="DataTemplate"/> for the cell's display (read-only) and edit modes. When a
    /// <see cref="GridColumn"/> has its <see cref="GridColumn.EditSettings"/> set, the grid
    /// generates a <see cref="System.Windows.Controls.DataGridTemplateColumn"/> using these
    /// templates instead of the default text or checkbox column.
    /// </summary>
    /// <remarks>
    /// Inherits from <see cref="FrameworkContentElement"/> (not <see cref="Freezable"/>) so that
    /// XAML bindings on properties like <c>ItemsSource</c> can resolve against an inherited
    /// <c>DataContext</c>. <see cref="SearchDataGrid"/> propagates its DataContext down through
    /// <see cref="GridColumn"/> to its <see cref="GridColumn.EditSettings"/> when columns are
    /// generated, and re-propagates whenever the grid's DataContext changes.
    /// </remarks>
    public abstract class BaseEditSettings : FrameworkContentElement
    {
        /// <summary>
        /// Optional user-supplied template for the read-only display cell. When set, the library
        /// uses this template verbatim and skips <see cref="CreateDisplayTemplate"/>. Bindings
        /// inside the template should reach the owning ViewModel via
        /// <c>RelativeSource AncestorType=Window</c> (or similar) — the cell DataContext is the
        /// row item, so the column's value binds directly via the field name.
        /// </summary>
        public static readonly DependencyProperty DisplayTemplateProperty =
            DependencyProperty.Register(nameof(DisplayTemplate), typeof(DataTemplate), typeof(BaseEditSettings),
                new PropertyMetadata(null));

        /// <summary>
        /// Optional user-supplied template for the in-place edit cell. When set, the library uses
        /// this template verbatim and skips <see cref="CreateEditTemplate"/>. See
        /// <see cref="DisplayTemplate"/> for binding guidance.
        /// </summary>
        public static readonly DependencyProperty EditTemplateProperty =
            DependencyProperty.Register(nameof(EditTemplate), typeof(DataTemplate), typeof(BaseEditSettings),
                new PropertyMetadata(null));

        /// <summary>
        /// Optional <see cref="System.Windows.Style"/> applied to the display-mode element
        /// (TextBlock for most editors, CheckBox for <see cref="CheckBoxEditSettings"/>) when
        /// the library builds its default display template. Beats the library's default style.
        /// Ignored when <see cref="DisplayTemplate"/> is set (full template override wins).
        /// </summary>
        public static readonly DependencyProperty DisplayStyleProperty =
            DependencyProperty.Register(nameof(DisplayStyle), typeof(Style), typeof(BaseEditSettings),
                new PropertyMetadata(null));

        /// <summary>
        /// Optional <see cref="System.Windows.Style"/> applied to the edit-mode element
        /// (TextBox / ComboBox / DatePicker / CheckBox depending on editor) when the library builds
        /// its default edit template. Beats the library's default style. Ignored when
        /// <see cref="EditTemplate"/> is set.
        /// </summary>
        public static readonly DependencyProperty EditorStyleProperty =
            DependencyProperty.Register(nameof(EditorStyle), typeof(Style), typeof(BaseEditSettings),
                new PropertyMetadata(null));

        public DataTemplate DisplayTemplate
        {
            get => (DataTemplate)GetValue(DisplayTemplateProperty);
            set => SetValue(DisplayTemplateProperty, value);
        }

        public DataTemplate EditTemplate
        {
            get => (DataTemplate)GetValue(EditTemplateProperty);
            set => SetValue(EditTemplateProperty, value);
        }

        public Style DisplayStyle
        {
            get => (Style)GetValue(DisplayStyleProperty);
            set => SetValue(DisplayStyleProperty, value);
        }

        public Style EditorStyle
        {
            get => (Style)GetValue(EditorStyleProperty);
            set => SetValue(EditorStyleProperty, value);
        }

        /// <summary>
        /// Helper for subclasses: applies the user-supplied <see cref="DisplayStyle"/> as a
        /// local value if set; otherwise looks up the library's default style by key from
        /// <see cref="Application.Current"/>.<see cref="Application.Resources"/> and applies
        /// that as a local value. Resolved at template-build time (not deferred) so it works
        /// reliably with <see cref="FrameworkElementFactory"/>, where SetResourceReference
        /// has known quirks for the StyleProperty.
        /// </summary>
        protected void ApplyDisplayStyle(FrameworkElementFactory factory, string defaultStyleKey)
        {
            var style = DisplayStyle ?? ResolveLibraryStyle(defaultStyleKey);
            if (style != null)
                factory.SetValue(FrameworkElement.StyleProperty, style);
        }

        /// <summary>
        /// Helper for subclasses: applies <see cref="EditorStyle"/> as a local value if set,
        /// else falls back to the library's keyed default Style looked up at build time.
        /// </summary>
        protected void ApplyEditorStyle(FrameworkElementFactory factory, string defaultStyleKey)
        {
            var style = EditorStyle ?? ResolveLibraryStyle(defaultStyleKey);
            if (style != null)
                factory.SetValue(FrameworkElement.StyleProperty, style);
        }

        private static Style ResolveLibraryStyle(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            var app = Application.Current;
            if (app == null) return null;
            return app.TryFindResource(key) as Style;
        }

        /// <summary>
        /// Returns the user-supplied <see cref="DisplayTemplate"/> if set; otherwise builds the
        /// editor's default via <see cref="CreateDisplayTemplate"/>. This is the entry point
        /// <see cref="GridColumn"/> calls — subclasses still implement <see cref="CreateDisplayTemplate"/>
        /// for the default; users override at this layer.
        /// </summary>
        public DataTemplate ResolveDisplayTemplate(GridColumn column)
            => DisplayTemplate ?? CreateDisplayTemplate(column);

        /// <summary>
        /// Returns the user-supplied <see cref="EditTemplate"/> if set; otherwise builds the
        /// editor's default via <see cref="CreateEditTemplate"/>.
        /// </summary>
        public DataTemplate ResolveEditTemplate(GridColumn column)
            => EditTemplate ?? CreateEditTemplate(column);

        /// <summary>
        /// Builds the read-only display template. Receives the owning <see cref="GridColumn"/> so
        /// the implementation can reach the binding path, display formatting, and converters.
        /// </summary>
        public abstract DataTemplate CreateDisplayTemplate(GridColumn column);

        /// <summary>
        /// Builds the in-place edit template. Receives the owning <see cref="GridColumn"/> so the
        /// implementation can wire a two-way binding to the field.
        /// </summary>
        public abstract DataTemplate CreateEditTemplate(GridColumn column);

        /// <summary>
        /// Helper for subclasses: build a two-way binding to the column's <see cref="GridColumn.FieldName"/>,
        /// updating the source when focus is lost (the standard editing UX).
        /// </summary>
        protected static Binding CreateValueBinding(GridColumn column, BindingMode mode = BindingMode.TwoWay)
        {
            return new Binding(column.FieldName)
            {
                Mode = mode,
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                ValidatesOnDataErrors = true,
                ValidatesOnExceptions = true
            };
        }
    }
}
