using System;
using WWControls.Core.DataAnnotations;
using WWControls.Core.Display;

namespace WWControls.Wpf.Editors.Settings
{
    /// <summary>
    /// Maps an <see cref="EditorKind"/> (or a CLR type) to a fresh <see cref="BaseEditorSettings"/>
    /// instance. The single "editor kind → editor settings" mapping shared by every editing host —
    /// the SearchDataGrid's smart-column configurator and the <see cref="WWPropertyGrid"/>'s editor
    /// resolution both build their editors from this factory rather than duplicating the switch.
    /// </summary>
    public static class EditorSettingsFactory
    {
        /// <summary>
        /// Returns a new <see cref="BaseEditorSettings"/> for <paramref name="kind"/>, or
        /// <c>null</c> for <see cref="EditorKind.Default"/> — the caller then picks an editor by
        /// the field's CLR type (see <see cref="CreateSettingsForType"/>).
        /// </summary>
        public static BaseEditorSettings CreateSettings(EditorKind kind) => kind switch
        {
            EditorKind.Text => new TextBoxSettings(),
            EditorKind.CheckBox => new CheckBoxSettings(),
            EditorKind.ComboBox => new ComboBoxSettings(),
            EditorKind.Date => new DatePickerSettings(),
            EditorKind.Spin => new NumericUpDownSettings(),
            _ => null,
        };

        /// <summary>
        /// Picks the default editor settings for a CLR type when no explicit editor was declared:
        /// <c>string</c> → text, <c>bool</c> → checkbox, <c>enum</c> → combo (populated from
        /// <paramref name="enumValues"/>, or the type's values when null), <c>DateTime</c> → date,
        /// a numeric type → a plain text box (<see cref="TextBoxSettings"/> with
        /// <see cref="Core.Display.MaskType.Numeric"/>, mirroring the SearchDataGrid's numeric
        /// default). The up/down spinner is opt-in — assign <see cref="NumericUpDownSettings"/>
        /// explicitly, or declare <see cref="EditorKind.Spin"/> — never the automatic default.
        /// Returns <c>null</c> for a type with no natural editor (a complex object, <c>Guid</c>, …)
        /// so the caller can fall back to a read-only display.
        /// </summary>
        public static BaseEditorSettings CreateSettingsForType(Type propertyType, Array enumValues = null)
        {
            var t = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            if (t == null) return null;

            if (t == typeof(bool)) return new CheckBoxSettings();
            if (t.IsEnum) return new ComboBoxSettings { ItemsSource = enumValues ?? Enum.GetValues(t) };
            if (t == typeof(DateTime)) return new DatePickerSettings();
            if (IsNumeric(t)) return new TextBoxSettings { MaskType = MaskType.Numeric };
            if (t == typeof(string)) return new TextBoxSettings();
            return null;
        }

        private static bool IsNumeric(Type t) =>
            t == typeof(byte) || t == typeof(sbyte)
            || t == typeof(short) || t == typeof(ushort)
            || t == typeof(int) || t == typeof(uint)
            || t == typeof(long) || t == typeof(ulong)
            || t == typeof(float) || t == typeof(double) || t == typeof(decimal);
    }
}
