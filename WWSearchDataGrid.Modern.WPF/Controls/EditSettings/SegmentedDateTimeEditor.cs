using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using WWSearchDataGrid.Modern.Core.Display;

namespace WWSearchDataGrid.Modern.WPF
{
    /// <summary>
    /// Segmented datetime editor used by <see cref="DateEditSettings"/>. Each format token in
    /// the mask (<c>MM</c>, <c>dd</c>, <c>yyyy</c>, <c>hh</c>, <c>mm</c>, <c>ss</c>, <c>tt</c>,
    /// <c>MMM</c>, <c>MMMM</c>, <c>ddd</c>, <c>dddd</c>, etc.) is a discrete section: tab/arrow
    /// navigates between sections, digits / letters overwrite within a section, Ctrl+Up/Down
    /// cycles, literals between sections are non-focusable. Matches the industry-standard
    /// datetime-editor model (WinUI, Telerik, DevExpress, <c>&lt;input type="datetime-local"&gt;</c>).
    /// The TextBox + calendar-dropdown layout (DockPanel + TextBox + Button + Popup + Calendar)
    /// lives in the default <see cref="ControlTemplate"/> in <c>Themes/EditSettings.xaml</c>;
    /// this class owns the <see cref="DependencyProperty"/> surface and the segmented-input
    /// model.
    /// </summary>
    /// <remarks>
    /// Segmented-input model — explicitly different from the slot-based mask used elsewhere:
    /// <list type="bullet">
    ///   <item>The mask pattern is parsed into a list of segments. Editable segments
    ///   correspond to date specifiers (M/d/y/H/h/m/s) and carry a max value. Literal
    ///   segments are the fixed separators between them.</item>
    ///   <item>No prompt-character placeholders. An empty region renders as nothing — the
    ///   display string is just the concatenation of (current digits per editable segment)
    ///   and (literal text). Empty MM/dd/yyyy → <c>"//"</c>; MM=5 → <c>"5//"</c>; full date
    ///   → <c>"12/15/2025"</c>.</item>
    ///   <item>No auto-advance. Each typed digit either appends to the current segment (if
    ///   the resulting <c>currentValue * 10 + digit</c> ≤ segment max) or overrides the
    ///   segment with just the new digit (if the candidate would be invalid). The caret
    ///   stays in the same segment until the user explicitly moves with arrow keys.</item>
    ///   <item>Arrow keys jump between editable segments. Right → next, Left → previous.
    ///   No within-segment caret nav (the model doesn't support inserting at an arbitrary
    ///   position; typing always appends or overrides relative to the segment's current
    ///   digits).</item>
    ///   <item>SelectAll on focus. First keystroke after SelectAll clears all editable
    ///   segments and applies to the first one — matches the "tab in, spam type, end up
    ///   with whatever digits fit" UX.</item>
    /// </list>
    /// On <see cref="LostFocus"/>, segment digits are concatenated and parsed via
    /// <see cref="DateTimeMaskFormatter"/> / <see cref="DateTime.TryParse(string, IFormatProvider, DateTimeStyles, out DateTime)"/>.
    /// Invalid composites (month=18, day=45, etc.) fail the parse and <see cref="Value"/> is
    /// left alone — the user sees their typed digits but the source isn't updated.
    /// </remarks>
    [TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PartCalendar, Type = typeof(System.Windows.Controls.Calendar))]
    [TemplatePart(Name = PartDropDownButton, Type = typeof(ToggleButton))]
    public class SegmentedDateTimeEditor : Control
    {
        private const string PartTextBox = "PART_TextBox";
        private const string PartCalendar = "PART_Calendar";
        private const string PartDropDownButton = "PART_DropDownButton";

        /// <summary>
        /// One element of the parsed Mask. The hierarchy mirrors the four ways the editor
        /// renders a position in the format string: fixed text, digit-driven value, choice
        /// from a finite enum, or value computed from <see cref="Value"/> with no user input.
        /// Tab / arrow nav and input handling branch on the runtime subtype rather than on a
        /// discriminator flag, so a future format token (e.g. fractional-second cycling) can
        /// be added by introducing a new subclass without re-checking flags scattered through
        /// the editor.
        /// </summary>
        private abstract class Segment
        {
            /// <summary>
            /// Rendered display text for this segment — what the inner TextBox shows for this
            /// position. For literal/computed segments this is fixed text; for digit/enum
            /// segments it's the user-driven content (digits typed, or the formatted enum
            /// value such as <c>"Mar"</c>/<c>"PM"</c>).
            /// </summary>
            public string Display = string.Empty;

            public int DisplayLength => Display.Length;

            /// <summary>Tab / arrow nav lands on this segment and the user can mutate it.</summary>
            public abstract bool IsEditable { get; }
        }

        /// <summary>Fixed separator text (e.g. <c>"/"</c>, <c>":"</c>, <c>", "</c>, quoted literals).</summary>
        private sealed class LiteralSegment : Segment
        {
            public override bool IsEditable => false;
        }

        /// <summary>
        /// Numeric date specifier — <c>M</c>/<c>d</c>/<c>y</c>/<c>H</c>/<c>h</c>/<c>m</c>/<c>s</c>/<c>f</c>/<c>F</c>
        /// (or <c>?</c> for the simple-grammar fallback). <see cref="Segment.Display"/> holds
        /// the digit string the user typed.
        /// </summary>
        private sealed class DigitSegment : Segment
        {
            public char Kind;          // 'M' / 'd' / 'y' / 'H' / 'h' / 'm' / 's' / 'f' / 'F' / '?' (Simple)
            public int MaxDigits;      // run length from format (e.g. 2 for "MM", 4 for "yyyy")
            public int Max;            // max integer value (e.g. 12 for month)
            public override bool IsEditable => true;
        }

        /// <summary>Text-form date specifier with a finite choice set. AM/PM, abbreviated/full month name.</summary>
        private enum EnumKind { AmPm, MonthAbbr, MonthFull }

        /// <summary>
        /// Editable text-form specifier — <c>tt</c>, <c>MMM</c>, <c>MMMM</c>. <see cref="Index"/>
        /// is the currently chosen value (<c>-1</c> = empty/sentinel before any input).
        /// <see cref="Segment.Display"/> holds the rendered string in the current culture.
        /// </summary>
        private sealed class EnumSegment : Segment
        {
            public EnumKind Kind;
            public int Index = -1;     // -1 = empty (no selection); 0..Count-1 = chosen value
            public int Count;
            public override bool IsEditable => true;
        }

        /// <summary>
        /// Display-only text-form specifier — <c>ddd</c>, <c>dddd</c>, <c>K</c>, <c>z</c>/<c>zz</c>/<c>zzz</c>,
        /// <c>g</c>/<c>gg</c>. Rendered from <see cref="Value"/> on each refresh; skipped by
        /// Tab/arrow nav. <see cref="FormatToken"/> is the original format-string token
        /// (e.g. <c>"ddd"</c>), so re-render is just <c>dt.ToString(FormatToken, culture)</c>.
        /// </summary>
        private sealed class ComputedSegment : Segment
        {
            public string FormatToken;
            public override bool IsEditable => false;
        }

        private TextBox _textBox;
        private System.Windows.Controls.Calendar _calendar;
        private ToggleButton _button;

        private readonly List<Segment> _segments = new List<Segment>();
        private int _activeSegmentIndex = -1;

        /// <summary>
        /// Set in <see cref="OnValueChanged"/> while we're pushing a new <see cref="Value"/>
        /// into the textbox; suppresses the <see cref="LostFocus"/> sync's circular write-back.
        /// </summary>
        private bool _suppressValueSync;

        /// <summary>
        /// Set after any user keystroke is applied; consumed by the deferred SelectAll
        /// on <see cref="OnTextBoxGotKeyboardFocus"/> so the seed-char path
        /// (<see cref="WWSearchDataGrid.Modern.WPF.SearchDataGrid"/> raises a synthetic
        /// PreviewTextInput when the user types into a non-editing cell) doesn't get
        /// clobbered by SelectAll re-running after the seed digit was already inserted.
        /// Reset on <see cref="OnTextBoxLostFocus"/>.
        /// </summary>
        private bool _seedInputApplied;

        /// <summary>
        /// Tracks whether the inner TextBox currently holds keyboard focus. Drives the
        /// placeholder-suppression rule in <see cref="RefreshTextBox"/>: when unfocused
        /// AND no editable segment has user input, the TextBox renders an empty string
        /// instead of the literals-only skeleton (e.g. <c>"//"</c> for a <c>MM/dd/yyyy</c>
        /// mask). Matters most for the auto-filter-row placement of this editor, where the
        /// control is always materialized but most cells start unfocused with a null value
        /// — without this, every date filter cell would show <c>"//"</c> as visual noise.
        /// </summary>
        private bool _textBoxIsFocused;

        /// <summary>
        /// Filter-row only. Set by <see cref="TryActivatePrefill"/> when the user focuses an
        /// empty editor under <see cref="IsFilterRowEditor"/>: each editable segment is
        /// populated from <see cref="DateTime.Today"/> so the focused editor renders a
        /// concrete date instead of bare separators (<c>"//"</c>). The pre-filled segments
        /// are <em>display only</em> — <see cref="Value"/> stays at its prior (null) state
        /// so no filter is silently applied just because the cell received focus. The flag
        /// is cleared (and segments are reset to empty) on the first user-driven mutation
        /// (<see cref="OnTextBoxPreviewTextInput"/>, Backspace, Delete) so subsequent input
        /// applies to clean segments. <see cref="IncrementActiveSegment"/> keeps the pre-fill
        /// values but flips the flag — spinning is an explicit edit, so the result should
        /// commit on LostFocus. <see cref="OnTextBoxLostFocus"/> with the flag still set
        /// reverts segments and skips <see cref="SyncValueFromSegments"/> so a focus-and-tab
        /// drive-by leaves the filter untouched.
        /// </summary>
        private bool _prefillActive;

        static SegmentedDateTimeEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SegmentedDateTimeEditor),
                new FrameworkPropertyMetadata(typeof(SegmentedDateTimeEditor)));
        }

        #region Dependency Properties

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(DateTime?), typeof(SegmentedDateTimeEditor),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.Register(nameof(Mask), typeof(string), typeof(SegmentedDateTimeEditor),
                new PropertyMetadata(null, OnMaskOrTypeChanged));

        public static readonly DependencyProperty MaskTypeProperty =
            DependencyProperty.Register(nameof(MaskType), typeof(MaskType), typeof(SegmentedDateTimeEditor),
                new PropertyMetadata(MaskType.Simple, OnMaskOrTypeChanged));

        public static readonly DependencyProperty MinDateProperty =
            DependencyProperty.Register(nameof(MinDate), typeof(DateTime?), typeof(SegmentedDateTimeEditor),
                new PropertyMetadata(null));

        public static readonly DependencyProperty MaxDateProperty =
            DependencyProperty.Register(nameof(MaxDate), typeof(DateTime?), typeof(SegmentedDateTimeEditor),
                new PropertyMetadata(null));

        /// <summary>
        /// Marks this editor as the filter-row instance produced by
        /// <see cref="DateEditSettings.CreateFilterEditor"/>. Two behaviors flip on:
        /// <list type="bullet">
        ///   <item><see cref="OnTextBoxLoaded"/> skips its <c>Keyboard.Focus</c> hop — the cell-edit
        ///   path needs it to bypass WPF's "focus stays on the cell" quirk, but the filter row
        ///   has no <see cref="DataGridCell"/> wrapper, so grabbing focus on load would steal
        ///   focus from whatever the user was doing and immediately render the empty-segment
        ///   literal skeleton (<c>//</c>) before any user intent to filter.</item>
        ///   <item><see cref="OnTextBoxGotKeyboardFocus"/> activates pre-fill mode: when the
        ///   editor receives focus with no value and no user input yet, segments are populated
        ///   from <see cref="DateTime.Today"/> so the user sees a concrete starting value rather
        ///   than literal separators. The pre-fill is non-binding (doesn't push to
        ///   <see cref="Value"/>) and clears on the first typed digit / Backspace / Delete.</item>
        /// </list>
        /// </summary>
        public static readonly DependencyProperty IsFilterRowEditorProperty =
            DependencyProperty.Register(nameof(IsFilterRowEditor), typeof(bool), typeof(SegmentedDateTimeEditor),
                new PropertyMetadata(false));

        /// <summary>
        /// Aligns the editor's typed text inside the inner <c>PART_TextBox</c>. Routed from
        /// <see cref="GridColumn.TextAlignment"/> at template-build time. Default
        /// <see cref="System.Windows.TextAlignment.Left"/>.
        /// </summary>
        public static readonly DependencyProperty TextAlignmentProperty =
            DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(SegmentedDateTimeEditor),
                new PropertyMetadata(TextAlignment.Left, OnTextAlignmentChanged));

        public TextAlignment TextAlignment
        {
            get => (TextAlignment)GetValue(TextAlignmentProperty);
            set => SetValue(TextAlignmentProperty, value);
        }

        private static void OnTextAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SegmentedDateTimeEditor editor && editor._textBox != null)
                editor._textBox.TextAlignment = (TextAlignment)e.NewValue;
        }

        public DateTime? Value
        {
            get => (DateTime?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

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

        /// <inheritdoc cref="IsFilterRowEditorProperty"/>
        public bool IsFilterRowEditor
        {
            get => (bool)GetValue(IsFilterRowEditorProperty);
            set => SetValue(IsFilterRowEditorProperty, value);
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_textBox != null) DetachTextBoxHandlers(_textBox);
            if (_calendar != null) _calendar.SelectedDatesChanged -= OnCalendarSelectionChanged;

            _textBox = GetTemplateChild(PartTextBox) as TextBox;
            _calendar = GetTemplateChild(PartCalendar) as System.Windows.Controls.Calendar;
            _button = GetTemplateChild(PartDropDownButton) as ToggleButton;

            if (_textBox != null)
            {
                AttachTextBoxHandlers(_textBox);
                _textBox.TextAlignment = TextAlignment;
            }
            if (_calendar != null) _calendar.SelectedDatesChanged += OnCalendarSelectionChanged;

            BuildSegments();
            PopulateSegmentsFromValue();
            RefreshTextBox();
        }

        private void AttachTextBoxHandlers(TextBox tb)
        {
            tb.PreviewTextInput += OnTextBoxPreviewTextInput;
            tb.PreviewKeyDown += OnTextBoxPreviewKeyDown;
            tb.GotKeyboardFocus += OnTextBoxGotKeyboardFocus;
            tb.LostFocus += OnTextBoxLostFocus;
            tb.PreviewMouseUp += OnTextBoxPreviewMouseUp;
            tb.Loaded += OnTextBoxLoaded;
        }

        private void DetachTextBoxHandlers(TextBox tb)
        {
            tb.PreviewTextInput -= OnTextBoxPreviewTextInput;
            tb.PreviewKeyDown -= OnTextBoxPreviewKeyDown;
            tb.GotKeyboardFocus -= OnTextBoxGotKeyboardFocus;
            tb.LostFocus -= OnTextBoxLostFocus;
            tb.PreviewMouseUp -= OnTextBoxPreviewMouseUp;
            tb.Loaded -= OnTextBoxLoaded;
        }

        /// <summary>
        /// WPF's <see cref="DataGridCell.BeginEdit"/> doesn't automatically push focus into the
        /// editing element for <see cref="DataGridTemplateColumn"/>: <c>cell.IsEditing</c> flips
        /// to true and the edit template is realized, but focus stays on the cell. Without
        /// this hop, the user has to press Tab a second time to reach the inner TextBox.
        /// Loaded fires on each new edit-mode entry (the template is rebuilt each time), so the
        /// dispatch reliably runs once per session — and the Input-priority defer lets WPF's
        /// own focus pipeline finish before we move focus, avoiding races with the cell's
        /// edit-mode setup.
        ///
        /// Skipped in <see cref="IsFilterRowEditor"/> mode: the filter cell isn't a
        /// <see cref="DataGridCell"/> entering edit mode — it's a persistent host materialized
        /// when the auto-filter row builds. Grabbing focus on load there would steal focus from
        /// wherever the user actually was and trigger <see cref="OnTextBoxGotKeyboardFocus"/>,
        /// which flips <c>_textBoxIsFocused</c> and unmasks the empty-segment literal skeleton
        /// (<c>//</c>) before any intent to filter.
        /// </summary>
        private void OnTextBoxLoaded(object sender, RoutedEventArgs e)
        {
            if (IsFilterRowEditor) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_textBox == null) return;
                if (_textBox.IsKeyboardFocused) return; // already there (mouse path, etc.)
                Keyboard.Focus(_textBox);
            }), DispatcherPriority.Input);
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            // Forward focus from the host control (which has IsTabStop=False) to the inner
            // TextBox when something programmatically focuses the host (e.g. cell.BeginEdit).
            if (e.NewFocus == this && _textBox != null)
            {
                Dispatcher.BeginInvoke(new Action(() => Keyboard.Focus(_textBox)),
                    DispatcherPriority.Input);
            }
        }

        private void OnCalendarSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Close the popup as soon as the user picks. Calendar.SelectedDate flows back to
            // Value via the TwoWay binding on the templated parent; OnValueChanged then
            // refreshes the textbox via PopulateSegmentsFromValue.
            if (_button != null && _calendar != null && _calendar.SelectedDate.HasValue)
                _button.IsChecked = false;
        }

        #region Segment building

        private void BuildSegments()
        {
            _segments.Clear();
            _activeSegmentIndex = -1;

            if (string.IsNullOrEmpty(Mask)) return;

            if (MaskType == MaskType.DateTime || MaskType == MaskType.DateOnly || MaskType == MaskType.TimeOnly)
                BuildSegmentsFromDateTimeFormat();
            else
                BuildSegmentsFromSimpleMask();

            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i].IsEditable) { _activeSegmentIndex = i; break; }
            }
        }

        /// <summary>
        /// Resolves the user's mask via <see cref="DateTimeMaskFormatter.ResolvePattern"/> (which
        /// does NOT throw on text-form specifiers, unlike the strict instance constructor) and
        /// walks the normalized pattern, emitting one segment per token. Digit runs become
        /// <see cref="DigitSegment"/>; text-form runs become <see cref="EnumSegment"/> (editable:
        /// <c>tt</c>, <c>MMM</c>, <c>MMMM</c>) or <see cref="ComputedSegment"/> (display-only:
        /// <c>ddd</c>, <c>dddd</c>, <c>K</c>, <c>z</c>/<c>zz</c>/<c>zzz</c>, <c>g</c>/<c>gg</c>).
        /// </summary>
        private void BuildSegmentsFromDateTimeFormat()
        {
            string format;
            try { format = DateTimeMaskFormatter.ResolvePattern(Mask); }
            catch { return; }

            var literalBuf = new StringBuilder();
            int i = 0;
            while (i < format.Length)
            {
                char c = format[i];

                if (c == '\'')
                {
                    int end = format.IndexOf('\'', i + 1);
                    if (end < 0) break;
                    for (int k = i + 1; k < end; k++) literalBuf.Append(format[k]);
                    i = end + 1;
                    continue;
                }
                if (c == '\\' && i + 1 < format.Length)
                {
                    literalBuf.Append(format[i + 1]);
                    i += 2;
                    continue;
                }

                // Month: 1-2 = digit, 3 = abbreviated name, 4+ = full name.
                if (c == 'M')
                {
                    int run = CountRun(format, i, c);
                    FlushLiteral(literalBuf);
                    if (run <= 2)
                    {
                        _segments.Add(new DigitSegment { Kind = 'M', MaxDigits = run, Max = 12 });
                    }
                    else
                    {
                        _segments.Add(new EnumSegment
                        {
                            Kind = run == 3 ? EnumKind.MonthAbbr : EnumKind.MonthFull,
                            Count = 12,
                            Index = -1,
                        });
                    }
                    i += run;
                    continue;
                }

                // Day: 1-2 = digit, 3+ = display-only day name (computed from the date).
                if (c == 'd')
                {
                    int run = CountRun(format, i, c);
                    FlushLiteral(literalBuf);
                    if (run <= 2)
                    {
                        _segments.Add(new DigitSegment { Kind = 'd', MaxDigits = run, Max = 31 });
                    }
                    else
                    {
                        _segments.Add(new ComputedSegment { FormatToken = new string('d', run) });
                    }
                    i += run;
                    continue;
                }

                // AM/PM designator — single 't' or 'tt'. Both editable; both render via culture.
                if (c == 't')
                {
                    int run = CountRun(format, i, c);
                    FlushLiteral(literalBuf);
                    _segments.Add(new EnumSegment
                    {
                        Kind = EnumKind.AmPm,
                        Count = 2,
                        Index = -1,
                    });
                    i += run;
                    continue;
                }

                // Timezone — K / z / zz / zzz — computed from Value, no input.
                if (c == 'K' || c == 'z')
                {
                    int run = CountRun(format, i, c);
                    FlushLiteral(literalBuf);
                    _segments.Add(new ComputedSegment { FormatToken = new string(c, run) });
                    i += run;
                    continue;
                }

                // Era — g / gg — computed, display-only.
                if (c == 'g')
                {
                    int run = CountRun(format, i, c);
                    FlushLiteral(literalBuf);
                    _segments.Add(new ComputedSegment { FormatToken = new string('g', run) });
                    i += run;
                    continue;
                }

                if ("yHhmsfF".IndexOf(c) >= 0)
                {
                    FlushLiteral(literalBuf);
                    int run = CountRun(format, i, c);
                    _segments.Add(new DigitSegment
                    {
                        Kind = c,
                        MaxDigits = run,
                        Max = MaxFor(c, run),
                    });
                    i += run;
                    continue;
                }

                literalBuf.Append(c);
                i++;
            }
            FlushLiteral(literalBuf);
        }

        /// <summary>
        /// Simple grammar fallback (mask like <c>"00/00/0000"</c>) — each <c>0</c> run becomes
        /// a generic-digit segment with no per-region max validation. Users wanting day/month
        /// caps should pass <see cref="MaskType.DateTime"/> with a real format string.
        /// </summary>
        private void BuildSegmentsFromSimpleMask()
        {
            string mask = Mask ?? string.Empty;
            var literalBuf = new StringBuilder();
            int i = 0;
            while (i < mask.Length)
            {
                char c = mask[i];

                if (c == '\\' && i + 1 < mask.Length)
                {
                    literalBuf.Append(mask[i + 1]);
                    i += 2;
                    continue;
                }

                if (c == '0')
                {
                    FlushLiteral(literalBuf);
                    int run = CountRun(mask, i, c);
                    _segments.Add(new DigitSegment
                    {
                        Kind = '?',
                        MaxDigits = run,
                        Max = (int)Math.Pow(10, run) - 1,
                    });
                    i += run;
                    continue;
                }

                literalBuf.Append(c);
                i++;
            }
            FlushLiteral(literalBuf);
        }

        private void FlushLiteral(StringBuilder buf)
        {
            if (buf.Length > 0)
            {
                _segments.Add(new LiteralSegment { Display = buf.ToString() });
                buf.Clear();
            }
        }

        private static int CountRun(string s, int start, char c)
        {
            int n = 0;
            while (start + n < s.Length && s[start + n] == c) n++;
            return n;
        }

        private static int MaxFor(char kind, int slotCount)
        {
            switch (kind)
            {
                case 'M': return 12;
                case 'd': return 31;
                case 'y': return slotCount == 2 ? 99 : 9999;
                case 'H': return 23;
                case 'h': return 12;
                case 'm': return 59;
                case 's': return 59;
                case 'f':
                case 'F': return (int)Math.Pow(10, slotCount) - 1;
                default: return (int)Math.Pow(10, slotCount) - 1;
            }
        }

        #endregion

        #region Display / segments helpers

        private string ComputeDisplayText()
        {
            var sb = new StringBuilder();
            foreach (var seg in _segments) sb.Append(seg.Display);
            return sb.ToString();
        }

        private int SegmentStartPosition(int segmentIndex)
        {
            int pos = 0;
            for (int i = 0; i < segmentIndex && i < _segments.Count; i++)
            {
                pos += _segments[i].DisplayLength;
            }
            return pos;
        }

        private int SegmentEndPosition(int segmentIndex)
        {
            if (segmentIndex < 0 || segmentIndex >= _segments.Count) return 0;
            return SegmentStartPosition(segmentIndex) + _segments[segmentIndex].DisplayLength;
        }

        /// <summary>
        /// Resolves a caret position (which can be ambiguous when multiple zero-length
        /// editable segments share the same boundary point) to an editable segment, biased
        /// toward the editable region the user most likely intended:
        /// 1. If the caret falls strictly inside an editable segment's digits, that wins.
        /// 2. Otherwise, prefer the nearest editable segment forward (matches click-to-edit
        ///    intent of "land in the next region"); if none, fall back to the previous one.
        /// </summary>
        private int FindEditableSegmentAt(int caretPosition)
        {
            int pos = 0;
            int candidate = -1;
            for (int i = 0; i < _segments.Count; i++)
            {
                var s = _segments[i];
                int len = s.DisplayLength;
                if (s.IsEditable)
                {
                    if (caretPosition > pos && caretPosition <= pos + len) return i;
                    if (candidate < 0 && caretPosition <= pos) candidate = i;
                }
                pos += len;
            }
            if (candidate >= 0) return candidate;
            for (int i = _segments.Count - 1; i >= 0; i--)
                if (_segments[i].IsEditable) return i;
            return -1;
        }

        private int FindNextEditableSegment(int from)
        {
            for (int i = from + 1; i < _segments.Count; i++)
                if (_segments[i].IsEditable) return i;
            return -1;
        }

        private int FindPrevEditableSegment(int from)
        {
            for (int i = from - 1; i >= 0; i--)
                if (_segments[i].IsEditable) return i;
            return -1;
        }

        private void RefreshTextBox()
        {
            if (_textBox == null) return;
            _suppressValueSync = true;
            try
            {
                // Placeholder suppression: when the user hasn't typed anything yet and
                // the textbox doesn't have focus, render an empty string instead of the
                // literals-only skeleton. Otherwise (focused, OR any segment has user
                // input) render the full segment composition. See _textBoxIsFocused docs
                // for the auto-filter-row motivation.
                string text = (!_textBoxIsFocused && AllEditableSegmentsEmpty())
                    ? string.Empty
                    : ComputeDisplayText();
                _textBox.Text = text;
                if (text.Length > 0
                    && _activeSegmentIndex >= 0
                    && _activeSegmentIndex < _segments.Count)
                {
                    _textBox.CaretIndex = SegmentEndPosition(_activeSegmentIndex);
                    _textBox.SelectionLength = 0;
                }
            }
            finally { _suppressValueSync = false; }
        }

        /// <summary>
        /// True when no editable segment carries user-supplied content — every
        /// <see cref="DigitSegment"/> has an empty <see cref="Segment.Display"/> and every
        /// <see cref="EnumSegment"/> still sits at <c>Index = -1</c>. Literal and computed
        /// segments are ignored: they're not user-driven, so them having "content" doesn't
        /// count as input.
        /// </summary>
        private bool AllEditableSegmentsEmpty()
        {
            foreach (var seg in _segments)
            {
                switch (seg)
                {
                    case DigitSegment d when d.Display.Length > 0: return false;
                    case EnumSegment en when en.Index >= 0: return false;
                }
            }
            return true;
        }

        #endregion

        #region Input handling

        private void OnTextBoxPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (_textBox == null || _segments.Count == 0) return;

            // We own the text — block all default insertion.
            e.Handled = true;

            if (string.IsNullOrEmpty(e.Text)) return;
            char c = e.Text[0];

            // Filter-row pre-fill: today's date is sitting in the segments as a hint, not as
            // user input. The first typed character should start a fresh value rather than
            // appending to / cycling within the hint — clear the segments before the input
            // handler below applies its digit/letter to the active segment.
            DeactivatePrefill(clearSegments: true);

            // _activeSegmentIndex is the source of truth — set explicitly by arrow nav,
            // mouse click, SelectAll, or initial template apply. Don't re-derive from caret
            // here: when the active region is empty, the caret can sit on a literal boundary
            // shared with adjacent zero-length editable segments, and naive caret-based
            // lookup walks back to the wrong region. The SelectAll case relies on the
            // append/override rule to cleanly replace existing digits in the active region
            // without clobbering any other region's content.
            if (_activeSegmentIndex < 0
                || _activeSegmentIndex >= _segments.Count
                || !_segments[_activeSegmentIndex].IsEditable)
            {
                for (int i = 0; i < _segments.Count; i++)
                {
                    if (_segments[i].IsEditable) { _activeSegmentIndex = i; break; }
                }
                if (_activeSegmentIndex < 0) return;
            }

            // Branch on segment kind. Digit segments only accept digits; AM/PM accepts A/P;
            // month-name accepts any letter (cycle-by-first-letter). Mismatched keys are
            // silently ignored — the user doesn't see a wrong char appear in the cell.
            bool applied;
            switch (_segments[_activeSegmentIndex])
            {
                case DigitSegment _:
                    if (!char.IsDigit(c)) return;
                    ApplyDigitToActiveSegment(c - '0');
                    applied = true;
                    break;
                case EnumSegment en when en.Kind == EnumKind.AmPm:
                    applied = ApplyAmPmKey(en, c);
                    break;
                case EnumSegment en when en.Kind == EnumKind.MonthAbbr || en.Kind == EnumKind.MonthFull:
                    applied = ApplyMonthLetterKey(en, c);
                    break;
                default:
                    return;
            }

            if (!applied) return;
            _seedInputApplied = true;
            TryRefreshComputedSegments();
            RefreshTextBox();
            SelectActiveRegion();
        }

        /// <summary>
        /// AM/PM keystroke handler: <c>A</c>/<c>a</c> → AM (Index 0), <c>P</c>/<c>p</c> → PM
        /// (Index 1). Other letters ignored. Returns whether the segment was mutated.
        /// </summary>
        private bool ApplyAmPmKey(EnumSegment seg, char c)
        {
            int newIndex;
            if (c == 'a' || c == 'A') newIndex = 0;
            else if (c == 'p' || c == 'P') newIndex = 1;
            else return false;

            if (seg.Index == newIndex) return false;
            seg.Index = newIndex;
            var dtfi = CultureInfo.CurrentCulture.DateTimeFormat;
            seg.Display = newIndex == 0 ? dtfi.AMDesignator : dtfi.PMDesignator;
            return true;
        }

        /// <summary>
        /// Month-name keystroke handler with stateless first-letter type-ahead. Looks up the
        /// next month whose name starts with the typed letter in the current culture's month
        /// list. If the current month already starts with that letter, advances to the *next*
        /// match (so repeated keystrokes cycle: J→Jan, J→Jun, J→Jul, J→Jan; M→Mar, M→May,
        /// M→Mar). Letters that match no month are silently ignored.
        /// </summary>
        private bool ApplyMonthLetterKey(EnumSegment seg, char c)
        {
            if (!char.IsLetter(c)) return false;

            var dtfi = CultureInfo.CurrentCulture.DateTimeFormat;
            string[] names = seg.Kind == EnumKind.MonthAbbr
                ? dtfi.AbbreviatedMonthNames
                : dtfi.MonthNames;

            // Cycle starting from (Index + 1), wrapping. If current is -1, start at 0.
            int start = seg.Index < 0 ? 0 : (seg.Index + 1) % seg.Count;
            for (int k = 0; k < seg.Count; k++)
            {
                int probe = (start + k) % seg.Count;
                string name = names[probe];
                if (!string.IsNullOrEmpty(name)
                    && char.ToUpperInvariant(name[0]) == char.ToUpperInvariant(c))
                {
                    seg.Index = probe;
                    seg.Display = name;
                    return true;
                }
            }
            return false;
        }

        private void OnTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_textBox == null || _segments.Count == 0) return;
            if (_activeSegmentIndex < 0) return;

            var modifiers = Keyboard.Modifiers;

            // Up / Down: Ctrl spins the active region's value, anything else exits the cell
            // (so plain Up/Down navigates rows like the rest of the DataGrid).
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                if ((modifiers & ModifierKeys.Control) != 0)
                {
                    // Spinning is an explicit edit on the pre-filled value — keep the segments
                    // populated so the spin starts from today's date (rather than empty / min)
                    // and just flip the flag so the result commits on LostFocus.
                    DeactivatePrefill(clearSegments: false);
                    IncrementActiveSegment(e.Key == Key.Up ? +1 : -1);
                }
                else
                {
                    BaseEditSettings.ExitCellViaArrow(_textBox, e);
                }
                e.Handled = true;
                return;
            }

            // Left / Right arrow rules:
            //   • Ctrl or Shift modifier → region nav, never exit the cell. Lets the user
            //     opt into region nav from any state without surprise cell exits.
            //   • Plain arrow + all-selected (e.g. just tab/click-focused) → exit the cell.
            //     The user hasn't drilled into a region yet, so arrow is column nav.
            //   • Plain arrow + single-region state (after region-nav, click into region,
            //     or post-typing caret) → region nav. When already at the first / last
            //     editable region in the arrow's direction, fall through to cell exit so
            //     the user can leave the cell without explicitly switching to plain Tab.
            if (e.Key == Key.Left || e.Key == Key.Right)
            {
                bool hasModifier = (modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) != 0;
                int textLen = _textBox.Text?.Length ?? 0;
                bool isAllSelected = textLen > 0 && _textBox.SelectionLength == textLen;
                bool wantNextRegion = e.Key == Key.Right;

                if (hasModifier)
                {
                    int target = wantNextRegion
                        ? FindNextEditableSegment(_activeSegmentIndex)
                        : FindPrevEditableSegment(_activeSegmentIndex);
                    if (target >= 0)
                    {
                        _activeSegmentIndex = target;
                        SelectActiveRegion();
                    }
                    // Modifier path stays in the editor at the edge — explicit opt-in to
                    // region nav shouldn't suddenly leave the cell.
                }
                else if (isAllSelected)
                {
                    BaseEditSettings.ExitCellViaArrow(_textBox, e);
                }
                else
                {
                    int target = wantNextRegion
                        ? FindNextEditableSegment(_activeSegmentIndex)
                        : FindPrevEditableSegment(_activeSegmentIndex);
                    if (target >= 0)
                    {
                        _activeSegmentIndex = target;
                        SelectActiveRegion();
                    }
                    else
                    {
                        // Edge region reached — exit the cell so the user can keep arrowing
                        // through the row.
                        BaseEditSettings.ExitCellViaArrow(_textBox, e);
                    }
                }
                e.Handled = true;
                return;
            }

            switch (e.Key)
            {
                case Key.Back:
                {
                    if (!_segments[_activeSegmentIndex].IsEditable) break;

                    // Filter-row pre-fill: Back means "drop the suggestion, I want to start
                    // clean". Wipe every segment in one shot and consume the key so the user
                    // doesn't see one digit pop off the active segment of today's date.
                    if (_prefillActive)
                    {
                        DeactivatePrefill(clearSegments: true);
                        RefreshTextBox();
                        e.Handled = true;
                        break;
                    }

                    var seg = _segments[_activeSegmentIndex];

                    // Enum segments clear whole — partial-substring on "Mar" → "Ma" isn't a
                    // valid month, so Backspace just resets Index to empty and Display to "".
                    // Digit segments pop the rightmost digit.
                    bool wasEmpty;
                    if (seg is EnumSegment en)
                    {
                        wasEmpty = en.Index < 0;
                        en.Index = -1;
                        en.Display = string.Empty;
                    }
                    else
                    {
                        wasEmpty = seg.Display.Length == 0;
                        if (!wasEmpty)
                            seg.Display = seg.Display.Substring(0, seg.Display.Length - 1);
                    }

                    if (wasEmpty)
                    {
                        // Already empty — hop to the previous editable segment so the next
                        // Backspace can shorten that one. Don't auto-pop digits from there;
                        // requires an explicit second keystroke (less destructive).
                        int prev = FindPrevEditableSegment(_activeSegmentIndex);
                        if (prev >= 0) _activeSegmentIndex = prev;
                    }
                    RefreshTextBox();
                    e.Handled = true;
                    break;
                }

                case Key.Delete:
                {
                    if (!_segments[_activeSegmentIndex].IsEditable) break;

                    // Filter-row pre-fill: same intent as Backspace — Delete means "drop the
                    // suggestion". Clear every segment rather than just the active one so the
                    // user doesn't end up looking at "12//2025" after one keystroke.
                    if (_prefillActive)
                    {
                        DeactivatePrefill(clearSegments: true);
                        RefreshTextBox();
                        e.Handled = true;
                        break;
                    }

                    var seg = _segments[_activeSegmentIndex];
                    if (seg is EnumSegment en) { en.Index = -1; en.Display = string.Empty; }
                    else seg.Display = string.Empty;
                    RefreshTextBox();
                    e.Handled = true;
                    break;
                }
            }
        }

        /// <summary>
        /// Mouse click in the textbox moves the caret to the click position. We re-derive
        /// the active segment from the new caret so subsequent typing applies to the region
        /// the user clicked into, then highlight that region's digits — matches the same
        /// "select-the-region" UX the arrow keys produce.
        /// </summary>
        private void OnTextBoxPreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_textBox == null || _segments.Count == 0) return;

            // The TextBox's CaretIndex isn't updated until after PreviewMouseUp's bubbling
            // pass completes — defer at Input priority to read the post-click position.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_textBox == null) return;
                int idx = FindEditableSegmentAt(_textBox.CaretIndex);
                if (idx < 0) return;
                _activeSegmentIndex = idx;
                SelectActiveRegion();
            }), DispatcherPriority.Input);
        }

        /// <summary>
        /// Selects the digit content of the currently active editable segment so the next
        /// keystroke either appends-or-overrides against it (the user sees what they're
        /// editing). Empty regions get a zero-length selection at their boundary position.
        /// </summary>
        private void SelectActiveRegion()
        {
            if (_textBox == null) return;
            if (_activeSegmentIndex < 0 || _activeSegmentIndex >= _segments.Count) return;
            var seg = _segments[_activeSegmentIndex];
            if (!seg.IsEditable) return;

            int start = SegmentStartPosition(_activeSegmentIndex);
            _textBox.SelectionStart = start;
            _textBox.SelectionLength = seg.Display.Length;
        }

        private void OnTextBoxGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // Flip the focus-tracking flag and re-render synchronously so the user sees the
            // mask skeleton (literals + any digits) the moment focus arrives — matters for
            // the filter-row case where the textbox was rendering empty while unfocused.
            _textBoxIsFocused = true;
            // Filter-row pre-fill: replace the bare "//" focused-empty state with today's
            // date so the user has a concrete, editable starting value. No-op when not in
            // filter-row mode or when segments already carry user input.
            TryActivatePrefill();
            RefreshTextBox();

            // SelectAll on focus, deferred so the seed-char path (from SearchDataGrid) — which
            // queues a Background dispatcher action to inject a typed digit — has already run
            // and set _seedInputApplied. Skipping SelectAll in that case preserves the seed
            // digit; otherwise the next keystroke would clobber it.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_textBox == null || !_textBox.IsKeyboardFocused) return;
                if (_seedInputApplied) return;
                _textBox.SelectAll();
            }), DispatcherPriority.Background);
        }

        private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            // Filter-row pre-fill still active means the user focused + tabbed away without
            // touching anything. Revert segments to empty and skip SyncValueFromSegments so
            // today's date doesn't quietly become an active filter just because the cell
            // received and lost focus.
            if (_prefillActive)
            {
                DeactivatePrefill(clearSegments: true);
            }
            else
            {
                SyncValueFromSegments();
            }
            _seedInputApplied = false;
            // Drop focus flag and re-render. If the user didn't type anything (and Value
            // is still null after SyncValueFromSegments), AllEditableSegmentsEmpty() is
            // true and the textbox collapses back to empty — the auto-filter-row "no
            // placeholder noise when idle" rule.
            _textBoxIsFocused = false;
            RefreshTextBox();
        }

        /// <summary>
        /// Append-or-override per the user's spec: candidate = current * 10 + newDigit. If
        /// the candidate is ≤ the segment's max <em>and</em> within the segment's digit
        /// capacity, append. Otherwise reset the segment to just the new digit. Caret stays
        /// in the segment; navigation between segments is on arrow keys.
        /// </summary>
        private void ApplyDigitToActiveSegment(int newDigit)
        {
            if (_activeSegmentIndex < 0 || _activeSegmentIndex >= _segments.Count) return;
            // Digit-typing only applies to digit segments; enum / computed segments handle
            // letter / no input in their own branches in OnTextBoxPreviewTextInput.
            if (_segments[_activeSegmentIndex] is not DigitSegment seg) return;

            int currentValue = seg.Display.Length > 0
                ? int.Parse(seg.Display, CultureInfo.InvariantCulture)
                : 0;
            int candidate = currentValue * 10 + newDigit;

            bool fitsLength = seg.Display.Length < seg.MaxDigits;
            if (fitsLength && candidate <= seg.Max)
                seg.Display = candidate.ToString(CultureInfo.InvariantCulture);
            else
                seg.Display = newDigit.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Spins the active editable segment's integer value by <paramref name="delta"/>.
        /// Cyclic specifiers (month, day, hour, minute, second) wrap at their min / max
        /// (e.g. month 12 + 1 → 1; hour 0 - 1 → 23). Year clamps to 1..9999 — wrapping a
        /// 9999-year range would surprise more than help. Empty regions on a first press
        /// initialize to <c>min</c> for ↑ and <c>max</c> for ↓ (year falls back to
        /// <see cref="DateTime.Today"/>'s year so the user sees a sensible starting value).
        /// The value is written zero-padded to <see cref="DigitSegment.MaxDigits"/> so the
        /// display stays in canonical form during spin.
        /// </summary>
        private void IncrementActiveSegment(int delta)
        {
            if (_activeSegmentIndex < 0 || _activeSegmentIndex >= _segments.Count) return;

            // Enum segments cycle their Index over [0, Count); empty (-1) initializes to the
            // first/last index depending on direction. Display is re-rendered from the current
            // culture immediately so the user sees the spin.
            if (_segments[_activeSegmentIndex] is EnumSegment en)
            {
                if (en.Count <= 0) return;
                int newIndex;
                if (en.Index < 0) newIndex = delta > 0 ? 0 : en.Count - 1;
                else newIndex = ((en.Index + delta) % en.Count + en.Count) % en.Count;
                en.Index = newIndex;

                var dtfi = CultureInfo.CurrentCulture.DateTimeFormat;
                switch (en.Kind)
                {
                    case EnumKind.AmPm:
                        en.Display = newIndex == 0 ? dtfi.AMDesignator : dtfi.PMDesignator;
                        break;
                    case EnumKind.MonthAbbr:
                        en.Display = dtfi.AbbreviatedMonthNames[newIndex];
                        break;
                    case EnumKind.MonthFull:
                        en.Display = dtfi.MonthNames[newIndex];
                        break;
                }
                TryRefreshComputedSegments();
                RefreshTextBox();
                SelectActiveRegion();
                return;
            }

            // Digit-segment spin (existing behavior).
            if (_segments[_activeSegmentIndex] is not DigitSegment seg) return;

            int min = MinFor(seg.Kind);
            int max = seg.Max;
            int newValue;

            if (seg.Display.Length == 0)
            {
                if (seg.Kind == 'y') newValue = DateTime.Today.Year;
                else newValue = delta > 0 ? min : max;
            }
            else
            {
                int current = int.Parse(seg.Display, CultureInfo.InvariantCulture);
                newValue = current + delta;
                if (seg.Kind == 'y')
                {
                    if (newValue < min) newValue = min;
                    if (newValue > max) newValue = max;
                }
                else
                {
                    int range = max - min + 1;
                    if (range <= 0) range = 1;
                    newValue = ((newValue - min) % range + range) % range + min;
                }
            }

            string digits = newValue.ToString(CultureInfo.InvariantCulture).PadLeft(seg.MaxDigits, '0');
            if (digits.Length > seg.MaxDigits)
                digits = digits.Substring(digits.Length - seg.MaxDigits);
            seg.Display = digits;

            TryRefreshComputedSegments();
            RefreshTextBox();
            // Highlight the spun region so a follow-up keystroke replaces it cleanly — same
            // selection feedback as arrow nav.
            SelectActiveRegion();
        }

        private static int MinFor(char kind)
        {
            switch (kind)
            {
                case 'M': return 1;
                case 'd': return 1;
                case 'h': return 1;
                case 'y': return 1;
                case 'H': return 0;
                case 'm': return 0;
                case 's': return 0;
                case 'f':
                case 'F': return 0;
                default: return 0;
            }
        }

        #endregion

        #region Value sync

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (SegmentedDateTimeEditor)d;
            if (self._suppressValueSync) return;
            // External Value write (calendar popup pick, programmatic set) supersedes any
            // pending filter-row pre-fill — the segments will be overwritten by the new
            // Value and shouldn't be treated as "untouched suggestion" on LostFocus.
            self._prefillActive = false;
            self.PopulateSegmentsFromValue();
            self.RefreshTextBox();
        }

        private static void OnMaskOrTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = (SegmentedDateTimeEditor)d;
            self.BuildSegments();
            self.PopulateSegmentsFromValue();
            self.RefreshTextBox();
        }

        /// <summary>
        /// Loads <see cref="Value"/> into each segment's <see cref="Segment.Display"/>:
        /// <list type="bullet">
        ///   <item><see cref="DigitSegment"/>: zero-padded to its <see cref="DigitSegment.MaxDigits"/>
        ///   (canonical <c>"01/15/2025"</c> form rather than <c>"1/15/2025"</c>; year over the cap
        ///   truncates to the rightmost digits, only meaningful for 2-digit year specifiers).</item>
        ///   <item><see cref="EnumSegment"/>: <see cref="EnumSegment.Index"/> set from the
        ///   current value (month-1 for month-name, 0/1 for AM/PM) and Display rendered from
        ///   the culture's <see cref="DateTimeFormatInfo"/> (month / AM-PM names).</item>
        ///   <item><see cref="ComputedSegment"/>: Display computed via
        ///   <c>dt.ToString(FormatToken, culture)</c> — day-name, timezone, era are all rendered
        ///   straight from the bound value.</item>
        /// </list>
        /// When <see cref="Value"/> is null, all editable segments clear and computed segments
        /// keep whatever stale display they had (rare path — typically Value is non-null in edit
        /// mode, and the cleared text is what the user expects to start typing into).
        /// </summary>
        private void PopulateSegmentsFromValue()
        {
            if (_segments.Count == 0) return;

            var culture = CultureInfo.CurrentCulture;

            if (Value.HasValue)
            {
                var dt = Value.Value;
                var dtfi = culture.DateTimeFormat;
                foreach (var seg in _segments)
                {
                    switch (seg)
                    {
                        case DigitSegment d:
                        {
                            int v = ValueForKind(d.Kind, dt);
                            string text = v.ToString(CultureInfo.InvariantCulture).PadLeft(d.MaxDigits, '0');
                            if (text.Length > d.MaxDigits) text = text.Substring(text.Length - d.MaxDigits);
                            d.Display = text;
                            break;
                        }
                        case EnumSegment e when e.Kind == EnumKind.AmPm:
                        {
                            e.Index = dt.Hour < 12 ? 0 : 1;
                            e.Display = e.Index == 0 ? dtfi.AMDesignator : dtfi.PMDesignator;
                            break;
                        }
                        case EnumSegment e when e.Kind == EnumKind.MonthAbbr:
                        {
                            e.Index = dt.Month - 1;
                            e.Display = dtfi.AbbreviatedMonthNames[e.Index];
                            break;
                        }
                        case EnumSegment e when e.Kind == EnumKind.MonthFull:
                        {
                            e.Index = dt.Month - 1;
                            e.Display = dtfi.MonthNames[e.Index];
                            break;
                        }
                        case ComputedSegment cs:
                        {
                            cs.Display = dt.ToString(cs.FormatToken, culture);
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (var seg in _segments)
                {
                    switch (seg)
                    {
                        case DigitSegment d: d.Display = string.Empty; break;
                        case EnumSegment e:  e.Index = -1; e.Display = string.Empty; break;
                        // ComputedSegment: leave stale display; will refresh when Value next changes.
                    }
                }
            }

            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i].IsEditable) { _activeSegmentIndex = i; break; }
            }
        }

        /// <summary>
        /// Filter-row pre-fill entry point. Runs from <see cref="OnTextBoxGotKeyboardFocus"/>
        /// when <see cref="IsFilterRowEditor"/> is true and every editable segment is empty:
        /// populates the segments from <see cref="DateTime.Today"/> using the same
        /// segment-by-kind mapping as <see cref="PopulateSegmentsFromValue"/>, but without
        /// touching <see cref="Value"/>. The result is a focused editor showing a concrete,
        /// editable date instead of bare separators — the user can spin / type to refine, or
        /// just tab away (which reverts to empty without committing a filter).
        /// </summary>
        private void TryActivatePrefill()
        {
            if (!IsFilterRowEditor) return;
            if (!_textBoxIsFocused) return;
            if (_prefillActive) return;
            if (!AllEditableSegmentsEmpty()) return;
            if (_segments.Count == 0) return;

            var today = DateTime.Today;
            var culture = CultureInfo.CurrentCulture;
            var dtfi = culture.DateTimeFormat;

            foreach (var seg in _segments)
            {
                switch (seg)
                {
                    case DigitSegment d:
                    {
                        int v = ValueForKind(d.Kind, today);
                        string text = v.ToString(CultureInfo.InvariantCulture).PadLeft(d.MaxDigits, '0');
                        if (text.Length > d.MaxDigits) text = text.Substring(text.Length - d.MaxDigits);
                        d.Display = text;
                        break;
                    }
                    case EnumSegment e when e.Kind == EnumKind.AmPm:
                        e.Index = today.Hour < 12 ? 0 : 1;
                        e.Display = e.Index == 0 ? dtfi.AMDesignator : dtfi.PMDesignator;
                        break;
                    case EnumSegment e when e.Kind == EnumKind.MonthAbbr:
                        e.Index = today.Month - 1;
                        e.Display = dtfi.AbbreviatedMonthNames[e.Index];
                        break;
                    case EnumSegment e when e.Kind == EnumKind.MonthFull:
                        e.Index = today.Month - 1;
                        e.Display = dtfi.MonthNames[e.Index];
                        break;
                    case ComputedSegment cs:
                        cs.Display = today.ToString(cs.FormatToken, culture);
                        break;
                }
            }

            _prefillActive = true;
        }

        /// <summary>
        /// Called from input handlers that should drop the pre-fill before applying the
        /// user's keystroke. When <paramref name="clearSegments"/> is true (typed digit,
        /// Backspace, Delete), every editable segment is reset to empty so the input lands
        /// on a clean slate rather than appending to / cycling within the pre-filled value.
        /// When false (spin via Ctrl+↑/↓), the segments are kept as-is and only the flag
        /// flips — spinning is an explicit edit on the pre-filled value, so its result
        /// should commit on LostFocus.
        /// </summary>
        private void DeactivatePrefill(bool clearSegments)
        {
            if (!_prefillActive) return;
            _prefillActive = false;

            if (!clearSegments) return;
            foreach (var seg in _segments)
            {
                switch (seg)
                {
                    case DigitSegment d: d.Display = string.Empty; break;
                    case EnumSegment e: e.Index = -1; e.Display = string.Empty; break;
                    // ComputedSegment intentionally left as-is — it gets refreshed by
                    // TryRefreshComputedSegments once the user-driven segments form a valid date.
                }
            }
        }

        private static int ValueForKind(char kind, DateTime dt)
        {
            switch (kind)
            {
                case 'M': return dt.Month;
                case 'd': return dt.Day;
                case 'y': return dt.Year;
                case 'H': return dt.Hour;
                case 'h': int h = dt.Hour % 12; return h == 0 ? 12 : h;
                case 'm': return dt.Minute;
                case 's': return dt.Second;
                default: return 0;
            }
        }

        /// <summary>
        /// Combines segment digits into a candidate string and tries to parse a
        /// <see cref="DateTime"/>. Tries the resolved format first (so culture-correct
        /// patterns like <c>"MM/dd/yyyy"</c> work even with single-digit user input),
        /// falls back to <see cref="DateTime.TryParse(string, IFormatProvider, DateTimeStyles, out DateTime)"/>.
        /// Updates <see cref="Value"/> on success; leaves it alone on failure so invalid
        /// composites don't clobber a previously-valid bound source value.
        /// </summary>
        private void SyncValueFromSegments()
        {
            if (_segments.Count == 0) return;

            // All editable segments empty → Value goes to null. Completeness rules per kind:
            //   • DigitSegment: at least one digit typed.
            //   • EnumSegment:  Index >= 0 (a value has been selected).
            //   • ComputedSegment / LiteralSegment: not editable, ignored.
            bool anyTyped = false;
            bool allComplete = true;
            foreach (var seg in _segments)
            {
                switch (seg)
                {
                    case DigitSegment d:
                        if (d.Display.Length > 0) anyTyped = true;
                        else allComplete = false;
                        break;
                    case EnumSegment en:
                        if (en.Index >= 0) anyTyped = true;
                        else allComplete = false;
                        break;
                }
            }

            if (!anyTyped)
            {
                if (Value != null)
                {
                    _suppressValueSync = true;
                    Value = null;
                    _suppressValueSync = false;
                }
                return;
            }

            if (!allComplete) return;

            string candidate = ComputeDisplayText();

            DateTime? parsed = null;
            if ((MaskType == MaskType.DateTime || MaskType == MaskType.DateOnly || MaskType == MaskType.TimeOnly)
                && !string.IsNullOrEmpty(Mask))
            {
                // Build the parse target from user-driven segments only — skip ComputedSegments
                // (dddd / ddd / K / z* / g*) since they're derived from Value and may be stale
                // mid-edit. Stale text in a computed segment (e.g. "Tuesday" when the spun date
                // is now a Saturday) would break TryParseExact because the day-name token can't
                // be reconciled with the rest of the date.
                var (parseText, parseFormat) = BuildParseInputs();
                if (!string.IsNullOrEmpty(parseFormat) &&
                    DateTime.TryParseExact(parseText, parseFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dtExact))
                {
                    parsed = dtExact;
                }
            }

            if (!parsed.HasValue && DateTime.TryParse(candidate, CultureInfo.CurrentCulture, DateTimeStyles.None, out var dtAny))
                parsed = dtAny;

            if (parsed.HasValue && parsed.Value != Value)
            {
                _suppressValueSync = true;
                Value = parsed.Value;
                _suppressValueSync = false;
                // Re-display the canonical zero-padded form (and refresh computed segments
                // like day-name that depend on the newly parsed date).
                PopulateSegmentsFromValue();
                RefreshTextBox();
            }
        }

        /// <summary>
        /// Builds matching (text, format) inputs for <see cref="DateTime.TryParseExact(string, string, IFormatProvider, DateTimeStyles, out DateTime)"/>
        /// from user-driven segments only. <see cref="ComputedSegment"/>s are skipped on both
        /// sides — their text is derived from <see cref="Value"/> and is stale mid-edit (e.g.
        /// the <c>dddd</c> day-name still shows "Tuesday" right after the user spins the month
        /// to one where the same day is a Saturday). Skipping them lets the parse succeed as
        /// long as the digit / enum segments form a valid date.
        ///
        /// Digits are padded to each segment's full width so a normalized format like
        /// <c>MM/dd/yyyy</c> matches single-digit user input like <c>"1/5/2025"</c>. Literal
        /// segments are wrapped in single quotes in the format string so reserved characters
        /// in the literal (e.g. an embedded letter) aren't interpreted as format specifiers.
        /// </summary>
        private (string text, string format) BuildParseInputs()
        {
            var textSb = new StringBuilder();
            var fmtSb = new StringBuilder();
            foreach (var seg in _segments)
            {
                switch (seg)
                {
                    case ComputedSegment _:
                        // Skip — its content is derived from Value and unreliable mid-edit.
                        break;
                    case LiteralSegment lit:
                        textSb.Append(lit.Display);
                        fmtSb.Append('\'').Append(lit.Display.Replace("'", "''")).Append('\'');
                        break;
                    case DigitSegment d:
                    {
                        string text = d.Display.PadLeft(d.MaxDigits, '0');
                        if (text.Length > d.MaxDigits) text = text.Substring(text.Length - d.MaxDigits);
                        textSb.Append(text);
                        fmtSb.Append(d.Kind, d.MaxDigits);
                        break;
                    }
                    case EnumSegment en:
                        textSb.Append(en.Display);
                        switch (en.Kind)
                        {
                            case EnumKind.AmPm:      fmtSb.Append("tt"); break;
                            case EnumKind.MonthAbbr: fmtSb.Append("MMM"); break;
                            case EnumKind.MonthFull: fmtSb.Append("MMMM"); break;
                        }
                        break;
                }
            }
            return (textSb.ToString(), fmtSb.ToString());
        }

        /// <summary>
        /// Recomputes every <see cref="ComputedSegment"/>'s display from a trial date built out
        /// of the current user-driven segments. Called after a successful spin or keystroke so
        /// the <c>dddd</c> day-name (and timezone, era) tracks the in-progress edit instead of
        /// going stale. If the user-driven segments don't form a valid date yet (mid-typing
        /// year, etc.), the trial parse fails and computed segments are left as-is — the user
        /// sees the previous day-name until enough is typed to resolve a real date.
        /// </summary>
        private bool TryRefreshComputedSegments()
        {
            bool hasComputed = false;
            foreach (var seg in _segments)
                if (seg is ComputedSegment) { hasComputed = true; break; }
            if (!hasComputed) return false;

            var (text, format) = BuildParseInputs();
            if (string.IsNullOrEmpty(format)) return false;
            if (!DateTime.TryParseExact(text, format, CultureInfo.CurrentCulture, DateTimeStyles.None, out var trial))
                return false;

            var culture = CultureInfo.CurrentCulture;
            foreach (var seg in _segments)
            {
                if (seg is ComputedSegment cs)
                    cs.Display = trial.ToString(cs.FormatToken, culture);
            }
            return true;
        }

        #endregion
    }
}
