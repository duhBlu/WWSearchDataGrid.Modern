using System.Windows;
using System.Windows.Data;

namespace WWControls.Wpf.Editors
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

        /// <summary>Builds the two-way value binding to the column's effective value path.</summary>
        Binding CreateFieldBinding();

        /// <summary>The grid host, when this column is hosted in a data grid; null otherwise (e.g. standalone / filter-row).</summary>
        IEditingGridHost Host { get; }
    }
}
