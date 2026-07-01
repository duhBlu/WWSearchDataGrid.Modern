using System;
using System.Windows;
using System.Windows.Data;
using WWControls.Core.Display;

namespace WWControls.Wpf.Editors
{
    /// <summary>
    /// Date editor over <see cref="WWBaseEdit"/>. It wraps the existing
    /// <see cref="SegmentedDateTimeEditor"/> (masked segment entry + a calendar dropdown) in the
    /// content host, forwarding the date essentials, while the base owns the border — so the date
    /// editor reads as one bordered input in the edit form and flat in a grid cell, the same as every
    /// other editor.
    /// </summary>
    /// <remarks>
    /// Wrapping (rather than re-parenting the 71KB segmented control onto <see cref="WWBaseEdit"/>)
    /// keeps that control's <c>Value</c> / segment logic intact and sidesteps a DP collision. The
    /// inner editor renders flat so only the base's chrome draws; the segmented control still
    /// self-focuses its TextBox on edit-mode entry.
    /// </remarks>
    public class WWDateEdit : WWBaseEdit
    {
        private readonly SegmentedDateTimeEditor _editor;

        static WWDateEdit()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WWDateEdit),
                new FrameworkPropertyMetadata(typeof(WWBaseEdit)));
        }

        public WWDateEdit()
        {
            _editor = new SegmentedDateTimeEditor();
            // The segmented control renders flat (its template draws no border), so the only border
            // is the one WWBaseEdit's chrome draws — no double border.

            // object ↔ DateTime? coerces through WPF's standard binding type-converter (same as the
            // filter row's SearchValue binding), so the adapter can bind the column field to Value.
            BindingOperations.SetBinding(_editor, SegmentedDateTimeEditor.ValueProperty,
                new Binding(nameof(Value)) { Source = this, Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(_editor, SegmentedDateTimeEditor.MaskProperty,
                new Binding(nameof(Mask)) { Source = this, Mode = BindingMode.OneWay });
            BindingOperations.SetBinding(_editor, SegmentedDateTimeEditor.MaskTypeProperty,
                new Binding(nameof(MaskType)) { Source = this, Mode = BindingMode.OneWay });
            BindingOperations.SetBinding(_editor, SegmentedDateTimeEditor.MinDateProperty,
                new Binding(nameof(MinDate)) { Source = this, Mode = BindingMode.OneWay });
            BindingOperations.SetBinding(_editor, SegmentedDateTimeEditor.MaxDateProperty,
                new Binding(nameof(MaxDate)) { Source = this, Mode = BindingMode.OneWay });
            BindingOperations.SetBinding(_editor, SegmentedDateTimeEditor.TextAlignmentProperty,
                new Binding(nameof(TextAlignment)) { Source = this, Mode = BindingMode.OneWay });

            EditContent = _editor;
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

        /// <summary>The wrapped segmented date editor.</summary>
        public SegmentedDateTimeEditor Editor => _editor;

        /// <inheritdoc />
        protected override System.Windows.IInputElement FocusTarget => _editor;
    }
}
