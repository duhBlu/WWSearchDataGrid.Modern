using System.Windows;
using System.Windows.Data;

namespace WWControls.Wpf.Controls.Editors
{
    /// <summary>
    /// Grid-agnostic view of a column that an editor's <c>EditSettings</c> reads to build its
    /// display / edit templates and value binding. Implemented by the grid's <c>ColumnDataBase</c>;
    /// lets the <c>EditSettings</c> adapters live in the Editors assembly without referencing the grid.
    /// </summary>
    public interface IEditorColumn
    {
        /// <summary>The column's field name (binding identity, and the key validation is scoped to).</summary>
        string FieldName { get; }

        /// <summary>Optional explicit binding override for the column's value path.</summary>
        BindingBase Binding { get; }

        /// <summary>Display-mode string format (e.g. "C2").</summary>
        string DisplayStringFormat { get; }

        /// <summary>Display-mode value converter.</summary>
        IValueConverter DisplayValueConverter { get; }

        /// <summary>Parameter passed to <see cref="DisplayValueConverter"/>.</summary>
        object DisplayConverterParameter { get; }

        /// <summary>Display-mode formatting mask.</summary>
        string DisplayMask { get; }

        /// <summary>Content alignment for the editor / display element.</summary>
        TextAlignment TextAlignment { get; }

        /// <summary>Whether data-annotation attribute errors surface for this column.</summary>
        bool ActualShowValidationAttributeErrors { get; }

        /// <summary>
        /// Whether a value that fails its data-annotation attributes may still commit to the source.
        /// A grid-hosted column delegates to its <see cref="Host"/>; a hostless column (e.g. a
        /// property-grid row) answers directly. Drives <c>DataAnnotationsValidationRule</c> when
        /// there is no <see cref="Host"/> to consult.
        /// </summary>
        bool AllowCommitOnValidationError { get; }

        /// <summary>Builds the two-way value binding to the column's effective value path.</summary>
        Binding CreateFieldBinding();

        /// <summary>
        /// True when the column's value path cannot be written back (a get-only source property). A
        /// two-way editor binding to a read-only CLR property throws, so
        /// <see cref="BaseEditorSettings.CreateValueBinding"/> forces the value binding one-way when
        /// this is set. Grid columns leave this false (a read-only cell simply never enters edit mode,
        /// so its two-way edit binding is never created); a standalone host that always materializes
        /// its editor (the property grid) sets it for get-only properties.
        /// </summary>
        bool IsValueReadOnly { get; }

        /// <summary>The grid host, when this column is hosted in a data grid; null otherwise (e.g. standalone / filter-row).</summary>
        IEditingGridHost Host { get; }
    }
}
