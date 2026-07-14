using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using WWControls.Core.Display;
using WWControls.Wpf.Converters;

namespace WWControls.Wpf.Editors.Settings
{
    /// <summary>
    /// Date editor with full mask support.
    /// <list type="bullet">
    ///   <item>Edit mode is a <see cref="SegmentedDateTimeEditor"/> — a composite of a masked
    ///   <see cref="TextBox"/> for keyboard entry plus a dropdown <see cref="ToggleButton"/>
    ///   that opens a <see cref="Popup"/>-hosted <see cref="Calendar"/> for mouse picking. The
    ///   layout itself lives in the control's <see cref="ControlTemplate"/> in
    ///   <c>Themes/EditSettings.xaml</c>; this class just selects the mask configuration.</item>
    ///   <item>Display mode is a <see cref="TextBlock"/> formatted with the column's
    ///   <see cref="GridColumn.DisplayStringFormat"/> (default short date) — or, with
    ///   <see cref="UseMaskAsDisplayFormat"/> set to <c>true</c>, formatted through the same
    ///   mask used in edit so display and edit text agree.</item>
    /// </list>
    /// </summary>
    public class DatePickerSettings : BaseEditorSettings
    {
        public static readonly DependencyProperty MinDateProperty =
            DependencyProperty.Register(nameof(MinDate), typeof(DateTime?), typeof(DatePickerSettings), new PropertyMetadata(null));

        public static readonly DependencyProperty MaxDateProperty =
            DependencyProperty.Register(nameof(MaxDate), typeof(DateTime?), typeof(DatePickerSettings), new PropertyMetadata(null));

        /// <summary>
        /// Mask pattern applied to the editor TextBox. Default <c>"d"</c> resolves through
        /// <see cref="Core.Display.MaskType.DateTime"/> to the current culture's short-date
        /// pattern (e.g. <c>"MM/dd/yyyy"</c> in en-US), so consumers using
        /// <see cref="DatePickerSettings"/> with no per-instance configuration get a working
        /// editor for <see cref="DateTime"/> columns. Custom date / time patterns
        /// (<c>"MM/dd/yyyy HH:mm:ss"</c>, <c>"yyyy-MM-dd"</c>, etc.) work the same way. Set
        /// <see cref="MaskType"/> to <see cref="Core.Display.MaskType.Simple"/> if you need
        /// the legacy slot grammar (<c>"00/00/0000"</c>); note that Simple grammar can't carry
        /// specifier info, so the editor won't auto-populate from <see cref="Value"/>.
        /// </summary>
        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register(nameof(Mask), typeof(string), typeof(DatePickerSettings),
                new PropertyMetadata("d"));

        /// <summary>
        /// How <see cref="Mask"/> is interpreted. Defaults to
        /// <see cref="Core.Display.MaskType.DateTime"/> so the default <see cref="Mask"/>
        /// (<c>"d"</c>) resolves to the culture's short-date pattern.
        /// </summary>
        public static readonly DependencyProperty MaskTypeProperty =
            DependencyProperty.Register(nameof(MaskType), typeof(MaskType), typeof(DatePickerSettings),
                new PropertyMetadata(Core.Display.MaskType.DateTime));

        /// <summary>
        /// When <c>true</c>, the display TextBlock formats the bound value through the same
        /// mask pattern used in edit mode — display and edit text become identical. When
        /// <c>false</c> (default), the display path uses <see cref="GridColumn.DisplayStringFormat"/>
        /// (or short date if none is supplied) and the mask only kicks in during editing.
        /// </summary>
        public static readonly DependencyProperty UseMaskAsDisplayFormatProperty =
            DependencyProperty.Register(nameof(UseMaskAsDisplayFormat), typeof(bool), typeof(DatePickerSettings),
                new PropertyMetadata(false));

        /// <summary>
        /// <inheritdoc cref="SegmentedDateTimeEditor.CycleModifierProperty"/>
        /// Defaults to <see cref="ModifierKeys.Control"/> here — unlike the standalone control's
        /// <see cref="ModifierKeys.None"/> — so unmodified Up/Down keeps navigating grid rows
        /// from a date cell / filter cell, matching the rest of the DataGrid.
        /// </summary>
        public static readonly DependencyProperty CycleModifierProperty =
            DependencyProperty.Register(nameof(CycleModifier), typeof(ModifierKeys), typeof(DatePickerSettings),
                new PropertyMetadata(ModifierKeys.Control));

        /// <inheritdoc cref="SegmentedDateTimeEditor.AllowNullInputProperty"/>
        public static readonly DependencyProperty AllowNullInputProperty =
            DependencyProperty.Register(nameof(AllowNullInput), typeof(bool), typeof(DatePickerSettings),
                new PropertyMetadata(true));

        /// <inheritdoc cref="SegmentedDateTimeEditor.DefaultDateProperty"/>
        public static readonly DependencyProperty DefaultDateProperty =
            DependencyProperty.Register(nameof(DefaultDate), typeof(DateTime?), typeof(DatePickerSettings),
                new PropertyMetadata(null));

        /// <inheritdoc cref="SegmentedDateTimeEditor.PopupModeProperty"/>
        public static readonly DependencyProperty PopupModeProperty =
            DependencyProperty.Register(nameof(PopupMode), typeof(DatePickerPopupMode), typeof(DatePickerSettings),
                new PropertyMetadata(DatePickerPopupMode.Calendar));

        /// <inheritdoc cref="SegmentedDateTimeEditor.TimeInputProperty"/>
        public static readonly DependencyProperty TimeInputProperty =
            DependencyProperty.Register(nameof(TimeInput), typeof(TimeInputMode), typeof(DatePickerSettings),
                new PropertyMetadata(TimeInputMode.Auto));

        /// <inheritdoc cref="SegmentedDateTimeEditor.ShowClearButtonProperty"/>
        public static readonly DependencyProperty ShowClearButtonProperty =
            DependencyProperty.Register(nameof(ShowClearButton), typeof(bool), typeof(DatePickerSettings),
                new PropertyMetadata(false));

        /// <inheritdoc cref="SegmentedDateTimeEditor.ShowTodayButtonProperty"/>
        public static readonly DependencyProperty ShowTodayButtonProperty =
            DependencyProperty.Register(nameof(ShowTodayButton), typeof(bool), typeof(DatePickerSettings),
                new PropertyMetadata(false));

        /// <inheritdoc cref="SegmentedDateTimeEditor.ShowNowButtonProperty"/>
        public static readonly DependencyProperty ShowNowButtonProperty =
            DependencyProperty.Register(nameof(ShowNowButton), typeof(bool), typeof(DatePickerSettings),
                new PropertyMetadata(false));

        /// <inheritdoc cref="SegmentedDateTimeEditor.ShowWeekNumbersProperty"/>
        public static readonly DependencyProperty ShowWeekNumbersProperty =
            DependencyProperty.Register(nameof(ShowWeekNumbers), typeof(bool), typeof(DatePickerSettings),
                new PropertyMetadata(false));

        /// <inheritdoc cref="SegmentedDateTimeEditor.DisableWeekendsProperty"/>
        public static readonly DependencyProperty DisableWeekendsProperty =
            DependencyProperty.Register(nameof(DisableWeekends), typeof(bool), typeof(DatePickerSettings),
                new PropertyMetadata(false));

        /// <inheritdoc cref="SegmentedDateTimeEditor.HighlightHolidaysProperty"/>
        public static readonly DependencyProperty HighlightHolidaysProperty =
            DependencyProperty.Register(nameof(HighlightHolidays), typeof(bool), typeof(DatePickerSettings),
                new PropertyMetadata(false));

        /// <summary>Optional lower bound applied to the calendar popup.</summary>
        public DateTime? MinDate
        {
            get => (DateTime?)GetValue(MinDateProperty);
            set => SetValue(MinDateProperty, value);
        }

        /// <summary>Optional upper bound applied to the calendar popup.</summary>
        public DateTime? MaxDate
        {
            get => (DateTime?)GetValue(MaxDateProperty);
            set => SetValue(MaxDateProperty, value);
        }

        /// <summary>Mask pattern applied to the editor TextBox.</summary>
        public string Mask
        {
            get => (string)GetValue(MaskProperty);
            set => SetValue(MaskProperty, value);
        }

        /// <summary>How <see cref="Mask"/> is interpreted by the input behavior.</summary>
        public MaskType MaskType
        {
            get => (MaskType)GetValue(MaskTypeProperty);
            set => SetValue(MaskTypeProperty, value);
        }

        /// <summary>Whether the mask is also used to format the display (non-editing) text.</summary>
        public bool UseMaskAsDisplayFormat
        {
            get => (bool)GetValue(UseMaskAsDisplayFormatProperty);
            set => SetValue(UseMaskAsDisplayFormatProperty, value);
        }

        /// <inheritdoc cref="SegmentedDateTimeEditor.CycleModifier"/>
        public ModifierKeys CycleModifier
        {
            get => (ModifierKeys)GetValue(CycleModifierProperty);
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

        /// <inheritdoc cref="SegmentedDateTimeEditor.DisableWeekends"/>
        public bool DisableWeekends
        {
            get => (bool)GetValue(DisableWeekendsProperty);
            set => SetValue(DisableWeekendsProperty, value);
        }

        /// <inheritdoc cref="SegmentedDateTimeEditor.HighlightHolidays"/>
        public bool HighlightHolidays
        {
            get => (bool)GetValue(HighlightHolidaysProperty);
            set => SetValue(HighlightHolidaysProperty, value);
        }

        public override DataTemplate CreateDisplayTemplate(IEditorColumn column)
        {
            // Two-column host: TextBlock fills column 0, calendar dropdown indicator sits in
            // column 1 with visibility controlled by EditorButtonShowMode.
            var grid = new FrameworkElementFactory(typeof(Grid));
            var col0 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col0.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            grid.AppendChild(col0);
            var col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            grid.AppendChild(col1);

            var factory = new FrameworkElementFactory(typeof(TextBlock));
            // Style FIRST — FrameworkElementFactory requires StyleProperty before other setters.
            ApplyDisplayStyle(factory, EditorThemeKeys.DisplayTextBlock);
            ApplyTextAlignment(factory, column);
            factory.SetValue(Grid.ColumnProperty, 0);

            var binding = column.CreateFieldBinding();
            binding.Mode = BindingMode.OneWay;

            string effectiveMask = !string.IsNullOrEmpty(Mask) ? Mask : column.DisplayMask;
            if (UseMaskAsDisplayFormat && !string.IsNullOrEmpty(effectiveMask))
            {
                MaskFormatterFactory.EnsureSupported(MaskType);
                binding.Converter = new MaskFormatConverter(MaskType);
                binding.ConverterParameter = effectiveMask;
            }
            else
            {
                // Default to short date when the consumer didn't supply one.
                binding.StringFormat = string.IsNullOrEmpty(column.DisplayStringFormat) ? "d" : column.DisplayStringFormat;
            }

            factory.SetBinding(TextBlock.TextProperty, binding);
            // Display element validates against the INotifyDataErrorInfo row by default; the badge
            // is the library's error surface, so strip WPF's red adorner. See TextBoxSettings.
            SuppressValidationErrorAdorner(factory);
            grid.AppendChild(factory);

            // Calendar glyph indicator — non-functional in display; click enters edit mode and
            // the user can then click the real calendar dropdown that appears in the editor.
            // Same style as the editor's actual dropdown button (SegmentedDateTimeEditor's
            // PART_DropDownButton), so retheming EditDateDropDownButton updates this indicator
            // and the editor's button together. IsHitTestVisible=false lets the click reach
            // the underlying cell, which the DataGrid promotes to edit mode.
            var glyph = new FrameworkElementFactory(typeof(ToggleButton));
            ApplyKeyedStyle(glyph, EditorThemeKeys.EditDateDropDownButton);
            glyph.SetValue(UIElement.IsHitTestVisibleProperty, false);
            glyph.SetValue(FrameworkElement.WidthProperty, 22.0);
            glyph.SetValue(Grid.ColumnProperty, 1);

            var visBinding = BuildEditorButtonVisibilityBinding(this, column);
            if (visBinding != null)
                glyph.SetBinding(UIElement.VisibilityProperty, visBinding);

            grid.AppendChild(glyph);
            return new DataTemplate { VisualTree = grid };
        }

        public override System.Collections.Generic.IEnumerable<Core.SearchType> GetSupportedFilterSearchTypes(Core.ColumnDataType columnDataType, bool isNullable)
            => WithNullability(new[]
            {
                Core.SearchType.Equals, Core.SearchType.NotEquals,
                Core.SearchType.GreaterThan, Core.SearchType.LessThan,
                Core.SearchType.GreaterThanOrEqualTo, Core.SearchType.LessThanOrEqualTo,
            }, isNullable);

        public override UIElement CreateFilterDisplay(IFilterEditorHost host)
        {
            // Read-only display: TextBlock with the same mask / format resolution rules the cell
            // display template uses. UseMaskAsDisplayFormat routes through MaskFormatConverter for
            // display==edit text parity; otherwise the column's DisplayStringFormat (or short date)
            // formats the value.
            var tb = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(4, 0, 4, 0),
            };
            var style = Application.Current?.TryFindResource(EditorThemeKeys.DisplayTextBlock) as Style;
            if (style != null) tb.Style = style;

            var binding = new Binding("SearchValue")
            {
                Source = host,
                Mode = BindingMode.OneWay,
            };

            string effectiveMask = !string.IsNullOrEmpty(Mask) ? Mask : host?.EditorColumn?.DisplayMask;
            string displayFormat = host?.EditorColumn?.DisplayStringFormat;
            if (UseMaskAsDisplayFormat && !string.IsNullOrEmpty(effectiveMask))
            {
                MaskFormatterFactory.EnsureSupported(MaskType);
                binding.Converter = new MaskFormatConverter(MaskType);
                binding.ConverterParameter = effectiveMask;
            }
            else
            {
                binding.StringFormat = string.IsNullOrEmpty(displayFormat) ? "d" : displayFormat;
            }

            BindingOperations.SetBinding(tb, TextBlock.TextProperty, binding);
            return tb;
        }

        public override UIElement CreateFilterEditor(IFilterEditorHost host)
        {
            // Reuse the same SegmentedDateTimeEditor the cell-edit template uses — same
            // masked TextBox + calendar popup, same theme styles. The only difference is
            // that Value is bound to the filter host's SearchValue (object-typed) instead
            // of a row-item field path. WPF coerces object↔DateTime? through the standard
            // binding type-converter, so picking a date in the popup pushes a DateTime
            // into SearchValue and clearing the editor pushes null.
            string effectiveMask = !string.IsNullOrEmpty(Mask) ? Mask : host?.EditorColumn?.DisplayMask;
            if (!string.IsNullOrEmpty(effectiveMask))
                MaskFormatterFactory.EnsureSupported(MaskType);

            var editor = new SegmentedDateTimeEditor
            {
                Mask = effectiveMask,
                MaskType = MaskType,
                // Filter-row marker: enables today's-date pre-fill on focus and suppresses the
                // cell-edit Loaded-time focus grab. See SegmentedDateTimeEditor.IsFilterRowEditor.
                IsFilterRowEditor = true,
                CycleModifier = CycleModifier,
                AllowNullInput = AllowNullInput,
                DefaultDate = DefaultDate,
                PopupMode = PopupMode,
                TimeInput = TimeInput,
                ShowClearButton = ShowClearButton,
                ShowTodayButton = ShowTodayButton,
                ShowNowButton = ShowNowButton,
                ShowWeekNumbers = ShowWeekNumbers,
                DisableWeekends = DisableWeekends,
                HighlightHolidays = HighlightHolidays,
            };
            if (MinDate.HasValue) editor.MinDate = MinDate.Value;
            if (MaxDate.HasValue) editor.MaxDate = MaxDate.Value;

            BindingOperations.SetBinding(editor, SegmentedDateTimeEditor.ValueProperty, new Binding("SearchValue")
            {
                Source = host,
                Mode = BindingMode.TwoWay,
            });

            // Cell-exit is the host's job: the control raises CellExitRequested, the adapter drives
            // the navigation (in the filter row, ExitCellViaArrow routes through FilterRowNavigator).
            editor.CellExitRequested += OnDateEditorCellExit;
            return editor;
        }

        public override DataTemplate CreateEditTemplate(IEditorColumn column)
        {
            string effectiveMask = !string.IsNullOrEmpty(Mask) ? Mask : column.DisplayMask;
            if (!string.IsNullOrEmpty(effectiveMask))
                MaskFormatterFactory.EnsureSupported(MaskType);

            // WWDatePicker wraps the SegmentedDateTimeEditor (masked segments + calendar popup) and owns
            // its own border, drawn by default and flattened when it detects a grid cell.
            var factory = new FrameworkElementFactory(typeof(WWDatePicker));
            factory.SetValue(WWDatePicker.TextAlignmentProperty, column.TextAlignment);
            factory.SetBinding(WWEditorBase.ValueProperty, CreateValueBinding(column));
            SuppressValidationErrorAdorner(factory);
            factory.SetValue(WWDatePicker.MaskProperty, effectiveMask);
            factory.SetValue(WWDatePicker.MaskTypeProperty, MaskType);
            if (MinDate.HasValue) factory.SetValue(WWDatePicker.MinDateProperty, MinDate.Value);
            if (MaxDate.HasValue) factory.SetValue(WWDatePicker.MaxDateProperty, MaxDate.Value);
            factory.SetValue(WWDatePicker.CycleModifierProperty, CycleModifier);
            factory.SetValue(WWDatePicker.AllowNullInputProperty, AllowNullInput);
            if (DefaultDate.HasValue) factory.SetValue(WWDatePicker.DefaultDateProperty, DefaultDate.Value);
            factory.SetValue(WWDatePicker.PopupModeProperty, PopupMode);
            factory.SetValue(WWDatePicker.TimeInputProperty, TimeInput);
            factory.SetValue(WWDatePicker.ShowClearButtonProperty, ShowClearButton);
            factory.SetValue(WWDatePicker.ShowTodayButtonProperty, ShowTodayButton);
            factory.SetValue(WWDatePicker.ShowNowButtonProperty, ShowNowButton);
            factory.SetValue(WWDatePicker.ShowWeekNumbersProperty, ShowWeekNumbers);
            factory.SetValue(WWDatePicker.DisableWeekendsProperty, DisableWeekends);
            factory.SetValue(WWDatePicker.HighlightHolidaysProperty, HighlightHolidays);

            // The control decides when an arrow should exit the cell and raises CellExitRequested;
            // the grid-side adapter drives the actual navigation, keeping WWDatePicker /
            // SegmentedDateTimeEditor grid-agnostic. Wire it once the editor materializes (re-arm on
            // recycle — the -= guards against stacking duplicate handlers).
            factory.AddHandler(FrameworkElement.LoadedEvent, new RoutedEventHandler((s, _) =>
            {
                if (s is WWDatePicker dateEdit && dateEdit.Editor != null)
                {
                    dateEdit.Editor.CellExitRequested -= OnDateEditorCellExit;
                    dateEdit.Editor.CellExitRequested += OnDateEditorCellExit;
                }
            }));

            return new DataTemplate { VisualTree = factory };
        }

        /// <summary>
        /// Grid-side handler for <see cref="SegmentedDateTimeEditor.CellExitRequested"/>: commits the
        /// edit and steps to the adjacent cell. The control raises this instead of calling the grid's
        /// navigation directly, so it stays grid-agnostic. The sender is the editor's inner TextBox
        /// (under the <see cref="System.Windows.Controls.DataGridCell"/> / filter
        /// <see cref="ColumnFilterControl"/>), which <see cref="BaseEditorSettings.ExitCellViaArrow"/>
        /// resolves to the owning cell.
        /// </summary>
        private static void OnDateEditorCellExit(object sender, KeyEventArgs e)
        {
            if (sender is DependencyObject source)
                ExitCellViaArrow(source, e);
        }
    }
}
