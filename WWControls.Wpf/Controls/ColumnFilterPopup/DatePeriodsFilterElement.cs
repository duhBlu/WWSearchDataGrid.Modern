using System.Linq;
using System.Windows;
using WWControls.Core;

namespace WWControls.Wpf
{
    /// <summary>
    /// Predefined date-range filter: lists the <see cref="DateInterval"/> enum values
    /// (Today / Yesterday / This Week / Last Week / This Month / This Year / …) as
    /// multi-select checkboxes. Selection drives a single <see cref="SearchType.DateInterval"/>
    /// template whose engine evaluator handles the live-rolling semantics — picking
    /// "This Week" stays correct as the week rolls over, where a frozen Between range
    /// would not.
    /// </summary>
    public class DatePeriodsFilterElement : FilterElementBase
    {
        static DatePeriodsFilterElement()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DatePeriodsFilterElement),
                new FrameworkPropertyMetadata(typeof(DatePeriodsFilterElement)));
        }

        private static readonly DependencyPropertyKey SearchTemplatePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(SearchTemplate),
                typeof(SearchTemplate),
                typeof(DatePeriodsFilterElement),
                new PropertyMetadata(null));

        /// <summary>
        /// Read-only DP holding the single <see cref="SearchType.DateInterval"/> template the
        /// element drives. Templated style binds to <c>SearchTemplate.DateIntervals</c> for
        /// the checkbox list.
        /// </summary>
        public static readonly DependencyProperty SearchTemplateProperty = SearchTemplatePropertyKey.DependencyProperty;

        public SearchTemplate SearchTemplate => (SearchTemplate)GetValue(SearchTemplateProperty);

        protected override void OnContextAttached(FilterElementContext context)
        {
            var template = ResolveOrCreateTemplate(context);
            if (template != null)
            {
                // Coerce to DateInterval so DateIntervals is the active input surface.
                // SearchTemplate.UpdateInputTemplate runs automatically on the setter.
                template.SearchType = SearchType.DateInterval;
            }
            SetValue(SearchTemplatePropertyKey, template);
        }

        protected override void OnContextDetached(FilterElementContext context)
        {
            SetValue(SearchTemplatePropertyKey, null);
        }

        private static SearchTemplate ResolveOrCreateTemplate(FilterElementContext context)
        {
            var controller = context?.Controller;
            if (controller == null) return null;

            var first = controller.SearchGroups?
                .FirstOrDefault()?
                .SearchTemplates?
                .FirstOrDefault();

            if (first != null) return first;

            controller.ClearAndReset();
            return controller.SearchGroups[0].SearchTemplates[0];
        }
    }
}
