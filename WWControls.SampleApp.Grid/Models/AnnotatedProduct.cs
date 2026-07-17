using System;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using WWControls.Core.DataAnnotations;

namespace WWControls.SampleApp.Grid.Models
{
    /// <summary>Product category — a smart ComboBox editor over an enum self-populates its drop-down.</summary>
    public enum ProductCategory
    {
        Hardware,
        Lumber,
        Finish,
        Fastener,
        Tooling,
    }

    /// <summary>
    /// Model for the Smart Columns sample. Every column facet lives here as a data annotation —
    /// nothing but <c>FieldName</c> + <c>IsSmart="True"</c> is declared in XAML:
    /// <list type="bullet">
    ///   <item><see cref="DisplayAttribute"/> — header text and Column Chooser / Filter Panel label;
    ///   <c>Order</c> also drives column order under auto-generation.</item>
    ///   <item><see cref="DisplayFormatAttribute"/> / <see cref="DataTypeAttribute"/> — display
    ///   format; an explicit <c>[DisplayFormat]</c> wins over the <c>[DataType]</c> mapping
    ///   (Currency → C2, Date → d, Time → t, DateTime → g).</item>
    ///   <item><see cref="GridEditorAttribute"/> / <see cref="DefaultEditorAttribute"/> — editor
    ///   kind; <c>[GridEditor]</c> wins inside a grid, <c>[DefaultEditor]</c> is the cross-host
    ///   fallback. A ComboBox editor over an enum fills its items from the enum values.</item>
    ///   <item><see cref="SimpleMaskAttribute"/> / <see cref="NumericMaskAttribute"/> /
    ///   <see cref="DateTimeMaskAttribute"/> — input mask; <c>UseMaskAsDisplayFormat</c> pushes the
    ///   mask into the read-only display cell too. A DateTime mask picks a date editor by itself.</item>
    /// </list>
    /// Properties hold raw, unformatted values — masks add the literal punctuation at display /
    /// edit time and parse it back out on commit.
    /// </summary>
    public sealed partial class AnnotatedProduct : ObservableObject
    {
        [ObservableProperty]
        [property: Display(Name = "SKU", Order = 0)]
        [property: SimpleMask("LL-0000", UseMaskAsDisplayFormat = true)]
        private string _sku = string.Empty;

        [ObservableProperty]
        [property: Display(Name = "Product", Order = 1)]
        private string _name = string.Empty;

        [ObservableProperty]
        [property: Display(Name = "Category", Order = 2)]
        [property: DefaultEditor(EditorKind.Text)]
        [property: GridEditor(EditorKind.ComboBox)]
        private ProductCategory _category;

        [ObservableProperty]
        [property: Display(Name = "Unit Price", Order = 3)]
        [property: DataType(DataType.Currency)]
        [property: NumericMask("C2")]
        private decimal _unitPrice;

        [ObservableProperty]
        [property: Display(Name = "Discount", Order = 4)]
        [property: DisplayFormat(DataFormatString = "P1")]
        private double _discount;

        [ObservableProperty]
        [property: Display(Name = "Quantity", Order = 5)]
        [property: GridEditor(EditorKind.Spin)]
        private int _quantity;

        [ObservableProperty]
        [property: Display(Name = "Restock Date", Order = 6)]
        [property: DataType(DataType.Date)]
        [property: DateTimeMask("MM/dd/yyyy")]
        private DateTime _restockDate;

        [ObservableProperty]
        [property: Display(Name = "Support Phone", Order = 7)]
        [property: SimpleMask("(000) 000-0000", UseMaskAsDisplayFormat = true)]
        private string _supportPhone = string.Empty;

        [ObservableProperty]
        [property: Display(Name = "Discontinued", Order = 8)]
        private bool _discontinued;
    }
}
