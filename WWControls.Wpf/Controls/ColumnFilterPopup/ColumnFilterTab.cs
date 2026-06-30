using System.Windows;
using System.Windows.Controls;

namespace WWControls.Wpf
{
    /// <summary>
    /// One tab inside the column filter popup. Used inside
    /// <see cref="ColumnDataBase.CustomColumnFilterTabs"/> to compose multi-tab custom popups, the
    /// same way the default popup exposes a Rules tab and a Values tab. Each tab carries a
    /// <see cref="Header"/> for the <see cref="TabItem"/> chrome and a <see cref="Template"/>
    /// that is inflated when the popup opens; if the inflated tree contains a
    /// <see cref="FilterElementBase"/> (named <c>PART_FilterElement</c> or any descendant of
    /// the type), the editor wires its <see cref="FilterElementContext"/> automatically.
    /// </summary>
    public class ColumnFilterTab : Freezable
    {
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(object),
                typeof(ColumnFilterTab),
                new PropertyMetadata(null));

        /// <summary>
        /// Header content for the generated <see cref="TabItem"/>. Strings render as text; any
        /// other UI element renders inline (e.g., an icon + caption stack).
        /// </summary>
        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty TemplateProperty =
            DependencyProperty.Register(
                nameof(Template),
                typeof(ControlTemplate),
                typeof(ColumnFilterTab),
                new PropertyMetadata(null));

        /// <summary>
        /// Template inflated into the tab's content area. Root either is or contains a
        /// <see cref="FilterElementBase"/> — the editor finds it via <c>x:Name="PART_FilterElement"</c>
        /// first, falling back to the first <see cref="FilterElementBase"/> descendant.
        /// Tabs without a <see cref="FilterElementBase"/> are still hosted (e.g., read-only
        /// summary tabs); they simply do not receive a <see cref="FilterElementContext"/>.
        /// </summary>
        public ControlTemplate Template
        {
            get => (ControlTemplate)GetValue(TemplateProperty);
            set => SetValue(TemplateProperty, value);
        }

        protected override Freezable CreateInstanceCore() => new ColumnFilterTab();
    }
}
