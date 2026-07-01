using System;
using System.Windows;
using WWControls.Core.Display;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// Date editor. A lookless control whose template wraps the existing
    /// <see cref="SegmentedDateTimeEditor"/> (masked segment entry + a calendar dropdown) as
    /// <c>PART_Editor</c> inside its own chrome, forwarding the date essentials via TemplateBindings
    /// while the chrome owns the border — so the date editor reads as one bordered input on a form
    /// and flat in a grid cell, the same as every other editor.
    /// </summary>
    /// <remarks>
    /// Wrapping (rather than re-parenting the segmented control onto <see cref="WWBaseEdit"/>) keeps
    /// that control's <c>Value</c> / segment logic intact and sidesteps a DP collision. The inner
    /// editor renders flat so only the chrome's border draws; the segmented control still
    /// self-focuses its TextBox on edit-mode entry.
    /// </remarks>
    [TemplatePart(Name = PartEditor, Type = typeof(SegmentedDateTimeEditor))]
    public class WWDateEdit : WWBaseEdit
    {
        private const string PartEditor = "PART_Editor";

        private SegmentedDateTimeEditor _editor;

        static WWDateEdit()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWDateEdit),
                new FrameworkPropertyMetadata(typeof(WWDateEdit)));
        }

        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register(nameof(Mask), typeof(string), typeof(WWDateEdit), new PropertyMetadata("d"));

        public static readonly DependencyProperty MaskTypeProperty =
            DependencyProperty.Register(nameof(MaskType), typeof(MaskType), typeof(WWDateEdit), new PropertyMetadata(MaskType.DateTime));

        public static readonly DependencyProperty MinDateProperty =
            DependencyProperty.Register(nameof(MinDate), typeof(DateTime?), typeof(WWDateEdit), new PropertyMetadata(null));

        public static readonly DependencyProperty MaxDateProperty =
            DependencyProperty.Register(nameof(MaxDate), typeof(DateTime?), typeof(WWDateEdit), new PropertyMetadata(null));

        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(WWDateEdit), new PropertyMetadata(TextAlignment.Left));

        public string Mask
        {
            get => (string)GetValue(MaskProperty);
            set => SetValue(MaskProperty, value);
        }

        public MaskType MaskType
        {
            get => (MaskType)GetValue(MaskTypeProperty);
            set => SetValue(MaskTypeProperty, value);
        }

        public DateTime? MinDate
        {
            get => (DateTime?)GetValue(MinDateProperty);
            set => SetValue(MinDateProperty, value);
        }

        public DateTime? MaxDate
        {
            get => (DateTime?)GetValue(MaxDateProperty);
            set => SetValue(MaxDateProperty, value);
        }

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        /// <summary>The wrapped segmented date editor (null before the template is applied).</summary>
        public SegmentedDateTimeEditor Editor => _editor;

        /// <inheritdoc />
        protected override System.Windows.IInputElement FocusTarget => _editor;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _editor = GetTemplateChild(PartEditor) as SegmentedDateTimeEditor;
        }
    }
}
