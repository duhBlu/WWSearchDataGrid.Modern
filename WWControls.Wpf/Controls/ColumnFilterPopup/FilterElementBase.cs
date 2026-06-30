using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf
{
    /// <summary>
    /// Abstract base for column-filter-popup content controls. Concrete subclasses
    /// (<c>CheckedListFilterElement</c>, <c>RangeFilterElement</c>,
    /// <c>DatePeriodsFilterElement</c>, etc.) supply the visual + interaction; the editor
    /// hands them a <see cref="FilterElementContext"/> and reads / writes filter rules via
    /// the controller it carries.
    /// </summary>
    /// <remarks>
    /// Consumers wire a filter element into a column via
    /// <see cref="ColumnDataBase.CustomColumnFilterTabs"/> and
    /// <see cref="FilterPopupMode"/><c>=Custom</c>:
    /// <code>
    /// &lt;sdg:GridColumn FieldName="ProductName" FilterPopupMode="Custom"&gt;
    ///     &lt;sdg:GridColumn.CustomColumnFilterTabs&gt;
    ///         &lt;sdg:ColumnFilterTab Header="Values"&gt;
    ///             &lt;sdg:ColumnFilterTab.Template&gt;
    ///                 &lt;ControlTemplate&gt;
    ///                     &lt;sdg:CheckedListFilterElement x:Name="PART_FilterElement" /&gt;
    ///                 &lt;/ControlTemplate&gt;
    ///             &lt;/sdg:ColumnFilterTab.Template&gt;
    ///         &lt;/sdg:ColumnFilterTab&gt;
    ///     &lt;/sdg:GridColumn.CustomColumnFilterTabs&gt;
    /// &lt;/sdg:GridColumn&gt;
    /// </code>
    /// The element marked <c>x:Name="PART_FilterElement"</c> inside each tab template gets its
    /// <see cref="Context"/> set by the popup host on inflation. Multiple tabs can each carry
    /// their own filter element — the popup wires one context per tab.
    /// </remarks>
    public abstract class FilterElementBase : Control
    {
        /// <summary>
        /// Conventional name a filter element carries inside a <see cref="ColumnFilterTab.Template"/>
        /// so the popup can find and attach context to it.
        /// </summary>
        public const string FilterElementPartName = "PART_FilterElement";

        public static readonly DependencyProperty ContextProperty =
            DependencyProperty.Register(
                nameof(Context),
                typeof(FilterElementContext),
                typeof(FilterElementBase),
                new PropertyMetadata(null, OnContextChanged));

        /// <summary>
        /// Filter context for the column this element drives. Set by the owning popup when the
        /// template is inflated; <c>null</c> while the element is detached.
        /// </summary>
        public FilterElementContext Context
        {
            get => (FilterElementContext)GetValue(ContextProperty);
            set => SetValue(ContextProperty, value);
        }

        private static void OnContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FilterElementBase fe) return;
            fe.OnContextDetached((FilterElementContext)e.OldValue);
            fe.OnContextAttached((FilterElementContext)e.NewValue);
        }

        /// <summary>
        /// Called when a new <see cref="Context"/> is assigned. Subclasses subscribe to the
        /// controller / filter-value manager here. Safe to call <see cref="Control.ApplyTemplate"/>-
        /// dependent setup since the popup inflates the template before assigning context.
        /// </summary>
        protected virtual void OnContextAttached(FilterElementContext context) { }

        /// <summary>
        /// Called when <see cref="Context"/> is cleared or replaced. Subclasses unsubscribe and
        /// release per-context state here so the element can be reused across columns / popup
        /// opens without leaking handlers.
        /// </summary>
        protected virtual void OnContextDetached(FilterElementContext context) { }
    }
}
