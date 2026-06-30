using System.Linq;
using System.Windows;
using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Single-rule filter element: hosts the column's first <see cref="SearchTemplate"/> with a
    /// SearchType combobox and the type-appropriate input editor (single text box, dual range,
    /// date interval check list, etc.). Mirrors the "Filter Rules" tab of the default tabbed
    /// popup but without the tab control or the values-tab.
    /// </summary>
    public class MultiFilterElement : FilterElementBase
    {
        static MultiFilterElement()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(MultiFilterElement),
                new FrameworkPropertyMetadata(typeof(MultiFilterElement)));
        }

        /// <summary>
        /// When <c>true</c> (default), suggestion-style inputs (the <see cref="SearchTextBox"/>
        /// primitives feeding from <c>SearchTemplateController.DisplayColumnValues</c>) show the
        /// per-value occurrence count alongside each entry. Set to <c>false</c> to suppress
        /// counts — the templated style nulls the <c>ColumnValueCounts</c> binding so the
        /// SearchTextBox dropdown renders values only.
        /// </summary>
        public static readonly DependencyProperty ShowCountsProperty =
            DependencyProperty.Register(
                nameof(ShowCounts),
                typeof(bool),
                typeof(MultiFilterElement),
                new PropertyMetadata(true));

        public bool ShowCounts
        {
            get => (bool)GetValue(ShowCountsProperty);
            set => SetValue(ShowCountsProperty, value);
        }

        private static readonly DependencyPropertyKey SearchTemplatePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(SearchTemplate),
                typeof(SearchTemplate),
                typeof(MultiFilterElement),
                new PropertyMetadata(null));

        /// <summary>
        /// Read-only DP exposed for the templated style to bind against. Set in
        /// <see cref="OnContextAttached"/> to the first template of the first search group on
        /// the column's controller; <c>null</c> while detached.
        /// </summary>
        public static readonly DependencyProperty SearchTemplateProperty = SearchTemplatePropertyKey.DependencyProperty;

        public SearchTemplate SearchTemplate => (SearchTemplate)GetValue(SearchTemplateProperty);

        protected override void OnContextAttached(FilterElementContext context)
        {
            var template = ResolveOrCreateSingleTemplate(context);
            SetValue(SearchTemplatePropertyKey, template);
        }

        protected override void OnContextDetached(FilterElementContext context)
        {
            SetValue(SearchTemplatePropertyKey, null);
        }

        /// <summary>
        /// Returns the first <see cref="SearchTemplate"/> from the controller, creating a default
        /// group + template if the controller is empty. The host editor (<c>ColumnFilterPopup</c>)
        /// normalizes the controller to a single template on open, so in practice the existing
        /// template is what we surface; the create-default path covers the cold case where the
        /// element is hosted without going through that editor.
        /// </summary>
        private static SearchTemplate ResolveOrCreateSingleTemplate(FilterElementContext context)
        {
            var controller = context?.Controller;
            if (controller == null) return null;

            var first = controller.SearchGroups?
                .FirstOrDefault()?
                .SearchTemplates?
                .FirstOrDefault();

            if (first != null) return first;

            // Cold path: empty controller. Reset to a fresh empty template so the combobox has
            // something to bind against. ClearAndReset both clears and seeds the default group.
            controller.ClearAndReset();
            return controller.SearchGroups[0].SearchTemplates[0];
        }
    }
}
