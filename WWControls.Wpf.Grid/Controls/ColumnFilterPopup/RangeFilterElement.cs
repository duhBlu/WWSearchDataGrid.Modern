using System.Linq;
using System.Windows;
using WWControls.Core;

namespace WWControls.Wpf.Grids
{
    /// <summary>
    /// Numeric / date / string range filter element. Hosts a single
    /// <see cref="SearchType.Between"/> (or <see cref="SearchType.BetweenDates"/> for DateTime
    /// columns) template against the column's controller and inflates the appropriate input
    /// editor based on the column's <see cref="ColumnDataType"/>:
    /// <list type="bullet">
    /// <item><b>Number</b> — <see cref="RangeSlider"/> + low/high <see cref="NumericUpDown"/> inputs.</item>
    /// <item><b>DateTime</b> — low/high <see cref="SegmentedDateTimeEditor"/>.</item>
    /// <item><b>String / other</b> — low/high <see cref="SearchTextBox"/>.</item>
    /// </list>
    /// </summary>
    public class RangeFilterElement : FilterElementBase
    {
        static RangeFilterElement()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(RangeFilterElement),
                new FrameworkPropertyMetadata(typeof(RangeFilterElement)));
        }

        public static readonly DependencyProperty SliderMinimumProperty =
            DependencyProperty.Register(
                nameof(SliderMinimum),
                typeof(double),
                typeof(RangeFilterElement),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty SliderMaximumProperty =
            DependencyProperty.Register(
                nameof(SliderMaximum),
                typeof(double),
                typeof(RangeFilterElement),
                new PropertyMetadata(100.0));

        /// <summary>
        /// Lower bound the numeric slider clamps to. Defaults to <c>0</c>; consumers typically
        /// override this in XAML when the column's value range is known.
        /// </summary>
        public double SliderMinimum
        {
            get => (double)GetValue(SliderMinimumProperty);
            set => SetValue(SliderMinimumProperty, value);
        }

        /// <summary>
        /// Upper bound the numeric slider clamps to. Defaults to <c>100</c>; override per column.
        /// </summary>
        public double SliderMaximum
        {
            get => (double)GetValue(SliderMaximumProperty);
            set => SetValue(SliderMaximumProperty, value);
        }

        private static readonly DependencyPropertyKey SearchTemplatePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(SearchTemplate),
                typeof(SearchTemplate),
                typeof(RangeFilterElement),
                new PropertyMetadata(null));

        public static readonly DependencyProperty SearchTemplateProperty = SearchTemplatePropertyKey.DependencyProperty;

        public SearchTemplate SearchTemplate => (SearchTemplate)GetValue(SearchTemplateProperty);

        private static readonly DependencyPropertyKey ColumnDataTypePropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(ColumnDataType),
                typeof(ColumnDataType),
                typeof(RangeFilterElement),
                new PropertyMetadata(ColumnDataType.Unknown));

        /// <summary>
        /// Mirror of the controller's <see cref="Core.ColumnDataType"/>, exposed so the templated
        /// style's <c>DataTrigger</c>s can pick the right input editor (numeric slider, date
        /// pickers, or plain text boxes) without round-tripping through <see cref="SearchTemplate"/>.
        /// </summary>
        public static readonly DependencyProperty ColumnDataTypeProperty = ColumnDataTypePropertyKey.DependencyProperty;

        public ColumnDataType ColumnDataType => (ColumnDataType)GetValue(ColumnDataTypeProperty);

        protected override void OnContextAttached(FilterElementContext context)
        {
            var template = ResolveOrCreateTemplate(context);
            var dataType = context?.Controller?.ColumnDataType ?? ColumnDataType.Unknown;

            if (template != null)
            {
                // DateTime gets BetweenDates so the engine treats values as DateTime; everything
                // else uses Between (string lexical, numeric numeric).
                template.SearchType = dataType == ColumnDataType.DateTime
                    ? SearchType.BetweenDates
                    : SearchType.Between;
            }

            SetValue(ColumnDataTypePropertyKey, dataType);
            SetValue(SearchTemplatePropertyKey, template);
        }

        protected override void OnContextDetached(FilterElementContext context)
        {
            SetValue(SearchTemplatePropertyKey, null);
            SetValue(ColumnDataTypePropertyKey, ColumnDataType.Unknown);
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
