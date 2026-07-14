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
    /// Wrapping (rather than re-parenting the segmented control onto <see cref="WWEditorBase"/>) keeps
    /// that control's <c>Value</c> / segment logic intact and sidesteps a DP collision. The inner
    /// editor renders flat so only the chrome's border draws; the segmented control still
    /// self-focuses its TextBox on edit-mode entry.
    /// </remarks>
    [TemplatePart(Name = PartEditor, Type = typeof(SegmentedDateTimeEditor))]
    public class WWDatePicker : WWEditorBase
    {
        private const string PartEditor = "PART_Editor";

        private SegmentedDateTimeEditor _editor;

        static WWDatePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWDatePicker),
                new FrameworkPropertyMetadata(typeof(WWDatePicker)));
        }

        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register(nameof(Mask), typeof(string), typeof(WWDatePicker), new PropertyMetadata("d"));

        public static readonly DependencyProperty MaskTypeProperty =
            DependencyProperty.Register(nameof(MaskType), typeof(MaskType), typeof(WWDatePicker), new PropertyMetadata(MaskType.DateTime));

        public static readonly DependencyProperty MinDateProperty =
            DependencyProperty.Register(nameof(MinDate), typeof(DateTime?), typeof(WWDatePicker), new PropertyMetadata(null));

        public static readonly DependencyProperty MaxDateProperty =
            DependencyProperty.Register(nameof(MaxDate), typeof(DateTime?), typeof(WWDatePicker), new PropertyMetadata(null));

        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(WWDatePicker), new PropertyMetadata(TextAlignment.Left));

        /// <inheritdoc cref="SegmentedDateTimeEditor.CycleModifierProperty"/>
        public static readonly DependencyProperty CycleModifierProperty =
            DependencyProperty.Register(nameof(CycleModifier), typeof(System.Windows.Input.ModifierKeys), typeof(WWDatePicker), new PropertyMetadata(System.Windows.Input.ModifierKeys.None));

        /// <inheritdoc cref="SegmentedDateTimeEditor.AllowNullInputProperty"/>
        public static readonly DependencyProperty AllowNullInputProperty =
            DependencyProperty.Register(nameof(AllowNullInput), typeof(bool), typeof(WWDatePicker), new PropertyMetadata(true));

        /// <inheritdoc cref="SegmentedDateTimeEditor.DefaultDateProperty"/>
        public static readonly DependencyProperty DefaultDateProperty =
            DependencyProperty.Register(nameof(DefaultDate), typeof(DateTime?), typeof(WWDatePicker), new PropertyMetadata(null));

        /// <inheritdoc cref="SegmentedDateTimeEditor.PopupModeProperty"/>
        public static readonly DependencyProperty PopupModeProperty =
            DependencyProperty.Register(nameof(PopupMode), typeof(DatePickerPopupMode), typeof(WWDatePicker), new PropertyMetadata(DatePickerPopupMode.Calendar));

        /// <inheritdoc cref="SegmentedDateTimeEditor.TimeInputProperty"/>
        public static readonly DependencyProperty TimeInputProperty =
            DependencyProperty.Register(nameof(TimeInput), typeof(TimeInputMode), typeof(WWDatePicker), new PropertyMetadata(TimeInputMode.Auto));

        /// <inheritdoc cref="SegmentedDateTimeEditor.ShowClearButtonProperty"/>
        public static readonly DependencyProperty ShowClearButtonProperty =
            DependencyProperty.Register(nameof(ShowClearButton), typeof(bool), typeof(WWDatePicker), new PropertyMetadata(false));

        /// <inheritdoc cref="SegmentedDateTimeEditor.ShowTodayButtonProperty"/>
        public static readonly DependencyProperty ShowTodayButtonProperty =
            DependencyProperty.Register(nameof(ShowTodayButton), typeof(bool), typeof(WWDatePicker), new PropertyMetadata(false));

        /// <inheritdoc cref="SegmentedDateTimeEditor.ShowNowButtonProperty"/>
        public static readonly DependencyProperty ShowNowButtonProperty =
            DependencyProperty.Register(nameof(ShowNowButton), typeof(bool), typeof(WWDatePicker), new PropertyMetadata(false));

        /// <inheritdoc cref="SegmentedDateTimeEditor.ShowWeekNumbersProperty"/>
        public static readonly DependencyProperty ShowWeekNumbersProperty =
            DependencyProperty.Register(nameof(ShowWeekNumbers), typeof(bool), typeof(WWDatePicker), new PropertyMetadata(false));

        /// <inheritdoc cref="SegmentedDateTimeEditor.DisplayFormatProperty"/>
        public static readonly DependencyProperty DisplayFormatProperty =
            DependencyProperty.Register(nameof(DisplayFormat), typeof(string), typeof(WWDatePicker), new PropertyMetadata(null));

        /// <inheritdoc cref="SegmentedDateTimeEditor.UseMaskAsDisplayFormatProperty"/>
        public static readonly DependencyProperty UseMaskAsDisplayFormatProperty =
            DependencyProperty.Register(nameof(UseMaskAsDisplayFormat), typeof(bool), typeof(WWDatePicker), new PropertyMetadata(false));

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

        /// <inheritdoc cref="SegmentedDateTimeEditor.CycleModifier"/>
        public System.Windows.Input.ModifierKeys CycleModifier
        {
            get => (System.Windows.Input.ModifierKeys)GetValue(CycleModifierProperty);
            set => SetValue(CycleModifierProperty, value);
        }

        /// <inheritdoc cref="SegmentedDateTimeEditor.AllowNullInput"/>
        public bool AllowNullInput
        {
            get => (bool)GetValue(AllowNullInputProperty);
            set => SetValue(AllowNullInputProperty, value);
        }

        /// <inheritdoc cref="SegmentedDateTimeEditor.DefaultDate"/>
        public DateTime? DefaultDate
        {
            get => (DateTime?)GetValue(DefaultDateProperty);
            set => SetValue(DefaultDateProperty, value);
        }

        /// <inheritdoc cref="SegmentedDateTimeEditor.PopupMode"/>
        public DatePickerPopupMode PopupMode
        {
            get => (DatePickerPopupMode)GetValue(PopupModeProperty);
            set => SetValue(PopupModeProperty, value);
        }

        /// <inheritdoc cref="SegmentedDateTimeEditor.TimeInput"/>
        public TimeInputMode TimeInput
        {
            get => (TimeInputMode)GetValue(TimeInputProperty);
            set => SetValue(TimeInputProperty, value);
        }

        /// <inheritdoc cref="SegmentedDateTimeEditor.ShowClearButton"/>
        public bool ShowClearButton
        {
            get => (bool)GetValue(ShowClearButtonProperty);
            set => SetValue(ShowClearButtonProperty, value);
        }

        /// <inheritdoc cref="SegmentedDateTimeEditor.ShowTodayButton"/>
        public bool ShowTodayButton
        {
            get => (bool)GetValue(ShowTodayButtonProperty);
            set => SetValue(ShowTodayButtonProperty, value);
        }

        /// <inheritdoc cref="SegmentedDateTimeEditor.ShowNowButton"/>
        public bool ShowNowButton
        {
            get => (bool)GetValue(ShowNowButtonProperty);
            set => SetValue(ShowNowButtonProperty, value);
        }

        /// <inheritdoc cref="SegmentedDateTimeEditor.ShowWeekNumbers"/>
        public bool ShowWeekNumbers
        {
            get => (bool)GetValue(ShowWeekNumbersProperty);
            set => SetValue(ShowWeekNumbersProperty, value);
        }

        /// <inheritdoc cref="SegmentedDateTimeEditor.DisplayFormat"/>
        public string DisplayFormat
        {
            get => (string)GetValue(DisplayFormatProperty);
            set => SetValue(DisplayFormatProperty, value);
        }

        /// <inheritdoc cref="SegmentedDateTimeEditor.UseMaskAsDisplayFormat"/>
        public bool UseMaskAsDisplayFormat
        {
            get => (bool)GetValue(UseMaskAsDisplayFormatProperty);
            set => SetValue(UseMaskAsDisplayFormatProperty, value);
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
