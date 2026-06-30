using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using WWControls.Core.Display;
using CoreAnnotations = WWControls.Core.DataAnnotations;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Applies a column's bound-property data annotations to its descriptor when the column is
    /// generated in <em>smart</em> mode (<see cref="ColumnDataBase.IsSmart"/>). Reads the standard
    /// <see cref="System.ComponentModel.DataAnnotations"/> metadata (<c>Display</c>,
    /// <c>DataType</c>, <c>DisplayFormat</c>) plus the library's
    /// <see cref="CoreAnnotations.MaskAttribute"/> / <see cref="CoreAnnotations.EditorAttributeBase"/>
    /// attributes, and configures header, display format, and the editor surface on top of the
    /// CLR-type defaults <see cref="ColumnDataBase.ApplyTypeBasedDefaults"/> already provides.
    /// </summary>
    /// <remarks>
    /// The configurator never overwrites a property the consumer set explicitly: header is only
    /// filled when empty, <see cref="ColumnDataBase.DisplayStringFormat"/> only when unset, and
    /// <see cref="ColumnDataBase.EditSettings"/> only when null. So a smart column can still pin
    /// any individual facet by declaring it in XAML.
    /// </remarks>
    internal static class SmartColumnConfigurator
    {
        /// <summary>
        /// Configures <paramref name="column"/> from the annotations on <paramref name="pd"/>.
        /// No-op when the column is not smart or the property descriptor is unavailable.
        /// </summary>
        internal static void Apply(ColumnDataBase column, PropertyDescriptor pd)
        {
            if (column == null || pd == null || !column.IsSmart)
                return;

            var attrs = pd.Attributes;

            ApplyHeader(column, pd, attrs);
            ApplyDisplayFormat(column, attrs);
            ApplyEditor(column, attrs);

            // Record whether the property has any validation attributes so the column knows to
            // overlay the display-mode error badge (ValidationErrorIcon) on its cells.
            column.HasValidationAttributes = attrs.OfType<ValidationAttribute>().Any();

            // A row that self-reports through INotifyDataErrorInfo can raise an error on any
            // property at runtime, so flag every column on such a type for the badge overlay even
            // when the property carries no attributes. ComponentType is the bound row type.
            column.RowImplementsDataErrorInfo =
                typeof(INotifyDataErrorInfo).IsAssignableFrom(pd.ComponentType);
        }

        private static void ApplyHeader(ColumnDataBase column, PropertyDescriptor pd, AttributeCollection attrs)
        {
            var display = attrs.OfType<DisplayAttribute>().FirstOrDefault();
            string name = display?.GetName();
            if (string.IsNullOrEmpty(name)
                && !string.Equals(pd.DisplayName, pd.Name, StringComparison.Ordinal))
            {
                name = pd.DisplayName;
            }
            if (string.IsNullOrEmpty(name))
                return;

            // Header drives the column-header text; ColumnDisplayName drives Column Chooser /
            // Filter Panel labels. Fill each only when the consumer left it blank.
            if (column.Header == null || (column.Header is string s && string.IsNullOrEmpty(s)))
                column.Header = name;
            if (string.IsNullOrEmpty(column.ColumnDisplayName))
                column.ColumnDisplayName = name;
        }

        private static void ApplyDisplayFormat(ColumnDataBase column, AttributeCollection attrs)
        {
            if (!string.IsNullOrEmpty(column.DisplayStringFormat))
                return;

            // Explicit [DisplayFormat] wins over [DataType] — it carries a concrete format string.
            var displayFormat = attrs.OfType<DisplayFormatAttribute>().FirstOrDefault();
            if (!string.IsNullOrEmpty(displayFormat?.DataFormatString))
            {
                column.DisplayStringFormat = displayFormat.DataFormatString;
                return;
            }

            var dataType = attrs.OfType<DataTypeAttribute>().FirstOrDefault();
            string format = MapDataTypeToFormat(dataType?.DataType);
            if (!string.IsNullOrEmpty(format))
                column.DisplayStringFormat = format;
        }

        private static string MapDataTypeToFormat(DataType? dataType) => dataType switch
        {
            DataType.Currency => "C2",
            DataType.Date => "d",
            DataType.Time => "t",
            DataType.DateTime => "g",
            _ => null,
        };

        private static void ApplyEditor(ColumnDataBase column, AttributeCollection attrs)
        {
            // Respect an explicitly configured editor — smart mode only fills the gap.
            if (column.EditSettings != null)
                return;

            CoreAnnotations.EditorKind kind = ResolveEditorKind(attrs);
            CoreAnnotations.MaskAttribute mask = attrs.OfType<CoreAnnotations.MaskAttribute>().FirstOrDefault();

            BaseEditSettings settings = CreateEditor(kind);

            if (mask != null)
            {
                // Masks apply only to text / date editors. When no editor kind was specified,
                // the mask's engine picks the editor: a DateTime mask wants a date editor, every
                // other mask wants a text editor. When the consumer explicitly chose a non-text
                // editor (Spin / ComboBox / CheckBox), the mask is not applicable and is ignored.
                bool wantsDate = mask.MaskType == MaskType.DateTime;
                if (settings == null)
                    settings = wantsDate ? new DateEditSettings() : new TextEditSettings();

                if (settings is TextEditSettings textSettings)
                {
                    textSettings.Mask = mask.Mask;
                    textSettings.MaskType = mask.MaskType;
                    textSettings.UseMaskAsDisplayFormat = mask.UseMaskAsDisplayFormat;
                }
                else if (settings is DateEditSettings dateSettings)
                {
                    dateSettings.Mask = mask.Mask;
                    dateSettings.MaskType = mask.MaskType;
                    dateSettings.UseMaskAsDisplayFormat = mask.UseMaskAsDisplayFormat;
                }
            }

            // A ComboBox editor over an enum field self-populates its drop-down from the enum
            // values — the most common smart-combo case. Skipped when the consumer already wired
            // an ItemsSource.
            if (settings is ComboBoxEditSettings combo && combo.ItemsSource == null)
            {
                Type underlying = Nullable.GetUnderlyingType(column.FieldType) ?? column.FieldType;
                if (underlying != null && underlying.IsEnum)
                    combo.ItemsSource = Enum.GetValues(underlying);
            }

            if (settings != null)
                column.EditSettings = settings;
        }

        /// <summary>
        /// Resolves the editor kind for the grid host: a <see cref="CoreAnnotations.GridEditorAttribute"/>
        /// wins, then a <see cref="CoreAnnotations.DefaultEditorAttribute"/>, otherwise
        /// <see cref="CoreAnnotations.EditorKind.Default"/>. LayoutControl / PropertyGrid editor
        /// attributes are ignored — those hosts don't exist yet.
        /// </summary>
        private static CoreAnnotations.EditorKind ResolveEditorKind(AttributeCollection attrs)
        {
            var editorAttrs = attrs.OfType<CoreAnnotations.EditorAttributeBase>().ToList();
            var grid = editorAttrs.FirstOrDefault(a => a.Context == CoreAnnotations.EditorContext.Grid);
            if (grid != null)
                return grid.Editor;
            var def = editorAttrs.FirstOrDefault(a => a.Context == CoreAnnotations.EditorContext.Default);
            return def?.Editor ?? CoreAnnotations.EditorKind.Default;
        }

        private static BaseEditSettings CreateEditor(CoreAnnotations.EditorKind kind) => kind switch
        {
            CoreAnnotations.EditorKind.Text => new TextEditSettings(),
            CoreAnnotations.EditorKind.CheckBox => new CheckBoxEditSettings(),
            CoreAnnotations.EditorKind.ComboBox => new ComboBoxEditSettings(),
            CoreAnnotations.EditorKind.Date => new DateEditSettings(),
            CoreAnnotations.EditorKind.Spin => new SpinEditSettings(),
            _ => null, // Default → let ColumnDataBase.AutoCreateEditSettings pick by CLR type.
        };
    }
}
