using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WWControls.Wpf.Controls.Editors.Settings;

namespace WWControls.Wpf.Controls.Editors
{
    /// <summary>
    /// Where a <see cref="WWPropertyGrid"/> row places its header (the property name) relative to the
    /// editor. Set grid-wide via <see cref="WWPropertyGrid.HeaderShowMode"/> or per property via
    /// <see cref="WWPropertyDefinition.HeaderShowMode"/> (which overrides the grid default).
    /// </summary>
    public enum PropertyHeaderShowMode
    {
        /// <summary>Header to the left of the editor — the default two-column row layout.</summary>
        Left,

        /// <summary>Header stacked above the editor, each spanning the full row width.</summary>
        Top,

        /// <summary>Header hidden; the editor takes the full row width.</summary>
        Hidden,

        /// <summary>Editor hidden; the header takes the full row width (a read-only caption row).</summary>
        OnlyHeader
    }

    /// <summary>
    /// A per-property declaration for <see cref="WWPropertyGrid"/> — the property-grid parallel of a
    /// <c>GridColumn</c>. Matched to one or more properties by name, it supplies the editor for those
    /// rows: an <see cref="EditSettings"/> block (the same <see cref="BaseEditorSettings"/> the
    /// SearchDataGrid uses for its cells) and/or a fully custom <see cref="EditTemplate"/> /
    /// <see cref="DisplayTemplate"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="FrameworkContentElement"/> so bindings on it (and on its <see cref="EditSettings"/>)
    /// resolve against a <c>DataContext</c> — the grid propagates its own <c>DataContext</c> down to
    /// each definition (and each definition forwards it to its <see cref="EditSettings"/>), so a
    /// <c>ComboBoxSettings ItemsSource="{Binding AvailableColors}"</c> resolves against the consumer's
    /// view model rather than the reflected row item. Supersedes the earlier <see cref="WWEditorDefinition"/>
    /// (which only carried a single custom edit template).
    /// </remarks>
    public class WWPropertyDefinition : FrameworkContentElement
    {
        private HashSet<string> _names;

        public WWPropertyDefinition()
        {
            // EditSettings is not in the logical/visual tree and doesn't inherit DataContext on its
            // own — forward this definition's DataContext to it so its bindings stay in sync, mirroring
            // how a grid column feeds its EditSettings.
            DataContextChanged += (_, e) =>
            {
                if (EditSettings != null)
                    EditSettings.DataContext = e.NewValue;
            };
        }

        /// <summary>Comma-separated property names this definition applies to (case-insensitive).</summary>
        public string TargetProperties { get; set; }

        /// <summary>
        /// Editor configuration for the matched properties — the same <see cref="BaseEditorSettings"/>
        /// family (<c>TextBoxSettings</c>, <c>ComboBoxSettings</c>, …) the SearchDataGrid builds its
        /// cell editors from. When set, the grid builds the row's editor from it unless
        /// <see cref="EditTemplate"/> overrides.
        /// </summary>
        public static readonly DependencyProperty EditSettingsProperty =
            DependencyProperty.Register(nameof(EditSettings), typeof(BaseEditorSettings), typeof(WWPropertyDefinition),
                new PropertyMetadata(null, OnEditSettingsChanged));

        public BaseEditorSettings EditSettings
        {
            get => (BaseEditorSettings)GetValue(EditSettingsProperty);
            set => SetValue(EditSettingsProperty, value);
        }

        private static void OnEditSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Seed the freshly-assigned settings with the definition's current DataContext so bindings
            // on it resolve even when EditSettings is set after the DataContext propagated.
            if (d is WWPropertyDefinition def && e.NewValue is BaseEditorSettings settings)
                settings.DataContext = def.DataContext;
        }

        /// <summary>
        /// A fully custom edit template for the matched properties. Its <c>DataContext</c> is the
        /// <see cref="WWPropertyItem"/>, so it binds the value via <c>{Binding Value}</c>. When set,
        /// it wins over <see cref="EditSettings"/>.
        /// </summary>
        public static readonly DependencyProperty EditTemplateProperty =
            DependencyProperty.Register(nameof(EditTemplate), typeof(DataTemplate), typeof(WWPropertyDefinition),
                new PropertyMetadata(null));

        public DataTemplate EditTemplate
        {
            get => (DataTemplate)GetValue(EditTemplateProperty);
            set => SetValue(EditTemplateProperty, value);
        }

        /// <summary>
        /// A fully custom read-only display template for the matched properties. Reserved for the
        /// display/edit split; carried for parity with <see cref="BaseEditorSettings.DisplayTemplate"/>.
        /// </summary>
        public static readonly DependencyProperty DisplayTemplateProperty =
            DependencyProperty.Register(nameof(DisplayTemplate), typeof(DataTemplate), typeof(WWPropertyDefinition),
                new PropertyMetadata(null));

        public DataTemplate DisplayTemplate
        {
            get => (DataTemplate)GetValue(DisplayTemplateProperty);
            set => SetValue(DisplayTemplateProperty, value);
        }

        #region Bindable metadata overrides (mechanism A)

        // Each of these is a nullable override the consumer may bind against the view model
        // (e.g. IsReadOnly="{Binding IsLocked}"). A null value means "don't override" — the matched
        // WWPropertyItem then falls through to the provider override, the static attribute, and finally
        // the default. Any change raises MetadataChanged so the subscribed items recompute live.

        /// <summary>Raised whenever one of the bindable metadata overrides changes (mechanism A live signal).</summary>
        public event EventHandler MetadataChanged;

        private static void OnMetadataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => ((WWPropertyDefinition)d).MetadataChanged?.Invoke(d, EventArgs.Empty);

        /// <summary>Overrides the matched rows' read-only state. Null = don't override.</summary>
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool?), typeof(WWPropertyDefinition),
                new PropertyMetadata(null, OnMetadataChanged));

        public bool? IsReadOnly
        {
            get => (bool?)GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>Overrides the matched rows' visibility. Null = don't override (default visible).</summary>
        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register(nameof(IsVisible), typeof(bool?), typeof(WWPropertyDefinition),
                new PropertyMetadata(null, OnMetadataChanged));

        public bool? IsVisible
        {
            get => (bool?)GetValue(IsVisibleProperty);
            set => SetValue(IsVisibleProperty, value);
        }

        /// <summary>Overrides the matched rows' display label. Null = don't override.</summary>
        public static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register(nameof(DisplayName), typeof(string), typeof(WWPropertyDefinition),
                new PropertyMetadata(null, OnMetadataChanged));

        public string DisplayName
        {
            get => (string)GetValue(DisplayNameProperty);
            set => SetValue(DisplayNameProperty, value);
        }

        /// <summary>Overrides the matched rows' category group. Null = don't override.</summary>
        public static readonly DependencyProperty CategoryProperty =
            DependencyProperty.Register(nameof(Category), typeof(string), typeof(WWPropertyDefinition),
                new PropertyMetadata(null, OnMetadataChanged));

        public string Category
        {
            get => (string)GetValue(CategoryProperty);
            set => SetValue(CategoryProperty, value);
        }

        /// <summary>Overrides the matched rows' description-panel text. Null = don't override.</summary>
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(WWPropertyDefinition),
                new PropertyMetadata(null, OnMetadataChanged));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <summary>Overrides the matched rows' sort order within a category. Null = don't override.</summary>
        public static readonly DependencyProperty PropertyOrderProperty =
            DependencyProperty.Register(nameof(PropertyOrder), typeof(int?), typeof(WWPropertyDefinition),
                new PropertyMetadata(null, OnMetadataChanged));

        public int? PropertyOrder
        {
            get => (int?)GetValue(PropertyOrderProperty);
            set => SetValue(PropertyOrderProperty, value);
        }

        /// <summary>
        /// Overrides whether validation errors surface for the matched rows. Null = don't override.
        /// Consumed by the validation phase; carried here so the definition is the single per-property
        /// metadata surface.
        /// </summary>
        public static readonly DependencyProperty ShowValidationErrorsProperty =
            DependencyProperty.Register(nameof(ShowValidationErrors), typeof(bool?), typeof(WWPropertyDefinition),
                new PropertyMetadata(null, OnMetadataChanged));

        public bool? ShowValidationErrors
        {
            get => (bool?)GetValue(ShowValidationErrorsProperty);
            set => SetValue(ShowValidationErrorsProperty, value);
        }

        /// <summary>
        /// Overrides where the matched rows place their header relative to the editor. Null = don't
        /// override (the row inherits the grid-level <see cref="WWPropertyGrid.HeaderShowMode"/>).
        /// </summary>
        public static readonly DependencyProperty HeaderShowModeProperty =
            DependencyProperty.Register(nameof(HeaderShowMode), typeof(PropertyHeaderShowMode?), typeof(WWPropertyDefinition),
                new PropertyMetadata(null, OnMetadataChanged));

        public PropertyHeaderShowMode? HeaderShowMode
        {
            get => (PropertyHeaderShowMode?)GetValue(HeaderShowModeProperty);
            set => SetValue(HeaderShowModeProperty, value);
        }

        #endregion

        /// <summary>Returns true when this definition targets the given property name.</summary>
        public bool Matches(string propertyName)
        {
            if (string.IsNullOrEmpty(TargetProperties) || string.IsNullOrEmpty(propertyName))
                return false;

            if (_names == null)
            {
                _names = new HashSet<string>(
                    TargetProperties.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim()),
                    StringComparer.OrdinalIgnoreCase);
            }

            return _names.Contains(propertyName);
        }
    }
}
