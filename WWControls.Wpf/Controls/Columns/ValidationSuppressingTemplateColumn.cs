using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// A <see cref="DataGridTemplateColumn"/> that strips WPF's default red validation adorner from
    /// the cell content it generates. <see cref="DataGridTemplateColumn.GenerateElement"/> and
    /// <see cref="DataGridTemplateColumn.GenerateEditingElement"/> wrap the cell template in a
    /// <see cref="ContentPresenter"/> whose <see cref="ContentControl.Content"/> is bound with a
    /// path-less <c>Binding</c> to the row item. <see cref="System.Windows.Data.Binding.ValidatesOnNotifyDataErrors"/>
    /// defaults on, so for an <see cref="System.ComponentModel.INotifyDataErrorInfo"/> row that
    /// presenter reports the row's entity-level errors as the framework adorner — a red border on
    /// every cell of an erroring row. The library surfaces validation through the cell's
    /// <see cref="ValidationErrorIcon"/> badge, so the adorner is redundant chrome; nulling
    /// <see cref="Validation.ErrorTemplateProperty"/> on the generated presenter removes it. The
    /// binding's error state is untouched, so commit gating keeps working.
    /// </summary>
    internal sealed class ValidationSuppressingTemplateColumn : DataGridTemplateColumn
    {
        protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
            => SuppressAdorner(base.GenerateElement(cell, dataItem));

        protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
            => SuppressAdorner(base.GenerateEditingElement(cell, dataItem));

        private static FrameworkElement SuppressAdorner(FrameworkElement element)
        {
            if (element != null)
                Validation.SetErrorTemplate(element, null);
            return element;
        }
    }
}
