using System.Windows;
using System.Windows.Media;

namespace WWControls.Wpf.Icons
{
    /// <summary>
    /// Typed <see cref="ComponentResourceKey"/> keys for the stock Lucide <see cref="DrawingImage"/>
    /// resources defined in <c>Lucide/LucideIcons.xaml</c>. As with <c>IconKeys</c> and
    /// <c>SearchTypeIconKeys</c> in the Controls assembly, these are <see cref="ComponentResourceKey"/>
    /// instances rather than loose string keys so consumer resource scopes cannot collide with icon
    /// names and the icon surface is discoverable from one static class.
    /// </summary>
    /// <remarks>
    /// Member names are the PascalCase form of the Lucide SVG file name — <c>a-arrow-down.svg</c>
    /// becomes <see cref="AArrowDown"/>.
    /// <para>XAML usage:</para>
    /// <code>
    /// xmlns:icons="clr-namespace:WWControls.Wpf.Icons;assembly=WWControls.Wpf.Icons"
    /// ...
    /// &lt;Image Source="{StaticResource {x:Static icons:LucideIconKeys.AArrowDown}}" /&gt;
    /// </code>
    /// </remarks>
    public static class LucideIconKeys
    {
        // ─── A ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>a-arrow-down</c> icon.</summary>
        public static ComponentResourceKey AArrowDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AArrowDown));

        /// <summary>Lucide <c>a-arrow-up</c> icon.</summary>
        public static ComponentResourceKey AArrowUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AArrowUp));

        /// <summary>Lucide <c>a-large-small</c> icon.</summary>
        public static ComponentResourceKey ALargeSmall { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ALargeSmall));

        /// <summary>Lucide <c>accessibility</c> icon.</summary>
        public static ComponentResourceKey Accessibility { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Accessibility));

        /// <summary>Lucide <c>activity</c> icon.</summary>
        public static ComponentResourceKey Activity { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Activity));

        /// <summary>Lucide <c>ad</c> icon.</summary>
        public static ComponentResourceKey Ad { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ad));

        /// <summary>Lucide <c>air-vent</c> icon.</summary>
        public static ComponentResourceKey AirVent { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AirVent));

        /// <summary>Lucide <c>airplay</c> icon.</summary>
        public static ComponentResourceKey Airplay { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Airplay));

        /// <summary>Lucide <c>alarm-clock-check</c> icon.</summary>
        public static ComponentResourceKey AlarmClockCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlarmClockCheck));

        /// <summary>Lucide <c>alarm-clock-minus</c> icon.</summary>
        public static ComponentResourceKey AlarmClockMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlarmClockMinus));

        /// <summary>Lucide <c>alarm-clock-off</c> icon.</summary>
        public static ComponentResourceKey AlarmClockOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlarmClockOff));

        /// <summary>Lucide <c>alarm-clock-plus</c> icon.</summary>
        public static ComponentResourceKey AlarmClockPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlarmClockPlus));

        /// <summary>Lucide <c>alarm-clock</c> icon.</summary>
        public static ComponentResourceKey AlarmClock { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlarmClock));

        /// <summary>Lucide <c>alarm-smoke</c> icon.</summary>
        public static ComponentResourceKey AlarmSmoke { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlarmSmoke));

        /// <summary>Lucide <c>album</c> icon.</summary>
        public static ComponentResourceKey Album { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Album));

        /// <summary>Lucide <c>align-center-horizontal</c> icon.</summary>
        public static ComponentResourceKey AlignCenterHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignCenterHorizontal));

        /// <summary>Lucide <c>align-center-vertical</c> icon.</summary>
        public static ComponentResourceKey AlignCenterVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignCenterVertical));

        /// <summary>Lucide <c>align-end-horizontal</c> icon.</summary>
        public static ComponentResourceKey AlignEndHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignEndHorizontal));

        /// <summary>Lucide <c>align-end-vertical</c> icon.</summary>
        public static ComponentResourceKey AlignEndVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignEndVertical));

        /// <summary>Lucide <c>align-horizontal-distribute-center</c> icon.</summary>
        public static ComponentResourceKey AlignHorizontalDistributeCenter { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignHorizontalDistributeCenter));

        /// <summary>Lucide <c>align-horizontal-distribute-end</c> icon.</summary>
        public static ComponentResourceKey AlignHorizontalDistributeEnd { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignHorizontalDistributeEnd));

        /// <summary>Lucide <c>align-horizontal-distribute-start</c> icon.</summary>
        public static ComponentResourceKey AlignHorizontalDistributeStart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignHorizontalDistributeStart));

        /// <summary>Lucide <c>align-horizontal-justify-center</c> icon.</summary>
        public static ComponentResourceKey AlignHorizontalJustifyCenter { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignHorizontalJustifyCenter));

        /// <summary>Lucide <c>align-horizontal-justify-end</c> icon.</summary>
        public static ComponentResourceKey AlignHorizontalJustifyEnd { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignHorizontalJustifyEnd));

        /// <summary>Lucide <c>align-horizontal-justify-start</c> icon.</summary>
        public static ComponentResourceKey AlignHorizontalJustifyStart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignHorizontalJustifyStart));

        /// <summary>Lucide <c>align-horizontal-space-around</c> icon.</summary>
        public static ComponentResourceKey AlignHorizontalSpaceAround { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignHorizontalSpaceAround));

        /// <summary>Lucide <c>align-horizontal-space-between</c> icon.</summary>
        public static ComponentResourceKey AlignHorizontalSpaceBetween { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignHorizontalSpaceBetween));

        /// <summary>Lucide <c>align-start-horizontal</c> icon.</summary>
        public static ComponentResourceKey AlignStartHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignStartHorizontal));

        /// <summary>Lucide <c>align-start-vertical</c> icon.</summary>
        public static ComponentResourceKey AlignStartVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignStartVertical));

        /// <summary>Lucide <c>align-vertical-distribute-center</c> icon.</summary>
        public static ComponentResourceKey AlignVerticalDistributeCenter { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignVerticalDistributeCenter));

        /// <summary>Lucide <c>align-vertical-distribute-end</c> icon.</summary>
        public static ComponentResourceKey AlignVerticalDistributeEnd { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignVerticalDistributeEnd));

        /// <summary>Lucide <c>align-vertical-distribute-start</c> icon.</summary>
        public static ComponentResourceKey AlignVerticalDistributeStart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignVerticalDistributeStart));

        /// <summary>Lucide <c>align-vertical-justify-center</c> icon.</summary>
        public static ComponentResourceKey AlignVerticalJustifyCenter { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignVerticalJustifyCenter));

        /// <summary>Lucide <c>align-vertical-justify-end</c> icon.</summary>
        public static ComponentResourceKey AlignVerticalJustifyEnd { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignVerticalJustifyEnd));

        /// <summary>Lucide <c>align-vertical-justify-start</c> icon.</summary>
        public static ComponentResourceKey AlignVerticalJustifyStart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignVerticalJustifyStart));

        /// <summary>Lucide <c>align-vertical-space-around</c> icon.</summary>
        public static ComponentResourceKey AlignVerticalSpaceAround { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignVerticalSpaceAround));

        /// <summary>Lucide <c>align-vertical-space-between</c> icon.</summary>
        public static ComponentResourceKey AlignVerticalSpaceBetween { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AlignVerticalSpaceBetween));

        /// <summary>Lucide <c>ambulance</c> icon.</summary>
        public static ComponentResourceKey Ambulance { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ambulance));

        /// <summary>Lucide <c>ampersand</c> icon.</summary>
        public static ComponentResourceKey Ampersand { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ampersand));

        /// <summary>Lucide <c>ampersands</c> icon.</summary>
        public static ComponentResourceKey Ampersands { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ampersands));

        /// <summary>Lucide <c>amphora</c> icon.</summary>
        public static ComponentResourceKey Amphora { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Amphora));

        /// <summary>Lucide <c>anchor</c> icon.</summary>
        public static ComponentResourceKey Anchor { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Anchor));

        /// <summary>Lucide <c>angry</c> icon.</summary>
        public static ComponentResourceKey Angry { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Angry));

        /// <summary>Lucide <c>annoyed</c> icon.</summary>
        public static ComponentResourceKey Annoyed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Annoyed));

        /// <summary>Lucide <c>antenna</c> icon.</summary>
        public static ComponentResourceKey Antenna { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Antenna));

        /// <summary>Lucide <c>anvil</c> icon.</summary>
        public static ComponentResourceKey Anvil { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Anvil));

        /// <summary>Lucide <c>aperture</c> icon.</summary>
        public static ComponentResourceKey Aperture { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Aperture));

        /// <summary>Lucide <c>app-window-mac</c> icon.</summary>
        public static ComponentResourceKey AppWindowMac { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AppWindowMac));

        /// <summary>Lucide <c>app-window</c> icon.</summary>
        public static ComponentResourceKey AppWindow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AppWindow));

        /// <summary>Lucide <c>apple</c> icon.</summary>
        public static ComponentResourceKey Apple { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Apple));

        /// <summary>Lucide <c>archive-restore</c> icon.</summary>
        public static ComponentResourceKey ArchiveRestore { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArchiveRestore));

        /// <summary>Lucide <c>archive-x</c> icon.</summary>
        public static ComponentResourceKey ArchiveX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArchiveX));

        /// <summary>Lucide <c>archive</c> icon.</summary>
        public static ComponentResourceKey Archive { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Archive));

        /// <summary>Lucide <c>armchair</c> icon.</summary>
        public static ComponentResourceKey Armchair { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Armchair));

        /// <summary>Lucide <c>arrow-big-down-dash</c> icon.</summary>
        public static ComponentResourceKey ArrowBigDownDash { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowBigDownDash));

        /// <summary>Lucide <c>arrow-big-down</c> icon.</summary>
        public static ComponentResourceKey ArrowBigDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowBigDown));

        /// <summary>Lucide <c>arrow-big-left-dash</c> icon.</summary>
        public static ComponentResourceKey ArrowBigLeftDash { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowBigLeftDash));

        /// <summary>Lucide <c>arrow-big-left</c> icon.</summary>
        public static ComponentResourceKey ArrowBigLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowBigLeft));

        /// <summary>Lucide <c>arrow-big-right-dash</c> icon.</summary>
        public static ComponentResourceKey ArrowBigRightDash { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowBigRightDash));

        /// <summary>Lucide <c>arrow-big-right</c> icon.</summary>
        public static ComponentResourceKey ArrowBigRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowBigRight));

        /// <summary>Lucide <c>arrow-big-up-dash</c> icon.</summary>
        public static ComponentResourceKey ArrowBigUpDash { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowBigUpDash));

        /// <summary>Lucide <c>arrow-big-up</c> icon.</summary>
        public static ComponentResourceKey ArrowBigUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowBigUp));

        /// <summary>Lucide <c>arrow-down-0-1</c> icon.</summary>
        public static ComponentResourceKey ArrowDown01 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowDown01));

        /// <summary>Lucide <c>arrow-down-1-0</c> icon.</summary>
        public static ComponentResourceKey ArrowDown10 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowDown10));

        /// <summary>Lucide <c>arrow-down-a-z</c> icon.</summary>
        public static ComponentResourceKey ArrowDownAZ { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowDownAZ));

        /// <summary>Lucide <c>arrow-down-from-line</c> icon.</summary>
        public static ComponentResourceKey ArrowDownFromLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowDownFromLine));

        /// <summary>Lucide <c>arrow-down-left</c> icon.</summary>
        public static ComponentResourceKey ArrowDownLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowDownLeft));

        /// <summary>Lucide <c>arrow-down-narrow-wide</c> icon.</summary>
        public static ComponentResourceKey ArrowDownNarrowWide { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowDownNarrowWide));

        /// <summary>Lucide <c>arrow-down-right</c> icon.</summary>
        public static ComponentResourceKey ArrowDownRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowDownRight));

        /// <summary>Lucide <c>arrow-down-to-dot</c> icon.</summary>
        public static ComponentResourceKey ArrowDownToDot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowDownToDot));

        /// <summary>Lucide <c>arrow-down-to-line</c> icon.</summary>
        public static ComponentResourceKey ArrowDownToLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowDownToLine));

        /// <summary>Lucide <c>arrow-down-up</c> icon.</summary>
        public static ComponentResourceKey ArrowDownUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowDownUp));

        /// <summary>Lucide <c>arrow-down-wide-narrow</c> icon.</summary>
        public static ComponentResourceKey ArrowDownWideNarrow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowDownWideNarrow));

        /// <summary>Lucide <c>arrow-down-z-a</c> icon.</summary>
        public static ComponentResourceKey ArrowDownZA { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowDownZA));

        /// <summary>Lucide <c>arrow-down</c> icon.</summary>
        public static ComponentResourceKey ArrowDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowDown));

        /// <summary>Lucide <c>arrow-left-from-line</c> icon.</summary>
        public static ComponentResourceKey ArrowLeftFromLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowLeftFromLine));

        /// <summary>Lucide <c>arrow-left-right</c> icon.</summary>
        public static ComponentResourceKey ArrowLeftRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowLeftRight));

        /// <summary>Lucide <c>arrow-left-to-line</c> icon.</summary>
        public static ComponentResourceKey ArrowLeftToLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowLeftToLine));

        /// <summary>Lucide <c>arrow-left</c> icon.</summary>
        public static ComponentResourceKey ArrowLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowLeft));

        /// <summary>Lucide <c>arrow-right-from-line</c> icon.</summary>
        public static ComponentResourceKey ArrowRightFromLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowRightFromLine));

        /// <summary>Lucide <c>arrow-right-left</c> icon.</summary>
        public static ComponentResourceKey ArrowRightLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowRightLeft));

        /// <summary>Lucide <c>arrow-right-to-line</c> icon.</summary>
        public static ComponentResourceKey ArrowRightToLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowRightToLine));

        /// <summary>Lucide <c>arrow-right</c> icon.</summary>
        public static ComponentResourceKey ArrowRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowRight));

        /// <summary>Lucide <c>arrow-up-0-1</c> icon.</summary>
        public static ComponentResourceKey ArrowUp01 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowUp01));

        /// <summary>Lucide <c>arrow-up-1-0</c> icon.</summary>
        public static ComponentResourceKey ArrowUp10 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowUp10));

        /// <summary>Lucide <c>arrow-up-a-z</c> icon.</summary>
        public static ComponentResourceKey ArrowUpAZ { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowUpAZ));

        /// <summary>Lucide <c>arrow-up-down</c> icon.</summary>
        public static ComponentResourceKey ArrowUpDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowUpDown));

        /// <summary>Lucide <c>arrow-up-from-dot</c> icon.</summary>
        public static ComponentResourceKey ArrowUpFromDot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowUpFromDot));

        /// <summary>Lucide <c>arrow-up-from-line</c> icon.</summary>
        public static ComponentResourceKey ArrowUpFromLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowUpFromLine));

        /// <summary>Lucide <c>arrow-up-left</c> icon.</summary>
        public static ComponentResourceKey ArrowUpLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowUpLeft));

        /// <summary>Lucide <c>arrow-up-narrow-wide</c> icon.</summary>
        public static ComponentResourceKey ArrowUpNarrowWide { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowUpNarrowWide));

        /// <summary>Lucide <c>arrow-up-right</c> icon.</summary>
        public static ComponentResourceKey ArrowUpRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowUpRight));

        /// <summary>Lucide <c>arrow-up-to-line</c> icon.</summary>
        public static ComponentResourceKey ArrowUpToLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowUpToLine));

        /// <summary>Lucide <c>arrow-up-wide-narrow</c> icon.</summary>
        public static ComponentResourceKey ArrowUpWideNarrow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowUpWideNarrow));

        /// <summary>Lucide <c>arrow-up-z-a</c> icon.</summary>
        public static ComponentResourceKey ArrowUpZA { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowUpZA));

        /// <summary>Lucide <c>arrow-up</c> icon.</summary>
        public static ComponentResourceKey ArrowUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowUp));

        /// <summary>Lucide <c>arrows-up-from-line</c> icon.</summary>
        public static ComponentResourceKey ArrowsUpFromLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ArrowsUpFromLine));

        /// <summary>Lucide <c>asterisk</c> icon.</summary>
        public static ComponentResourceKey Asterisk { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Asterisk));

        /// <summary>Lucide <c>astroid</c> icon.</summary>
        public static ComponentResourceKey Astroid { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Astroid));

        /// <summary>Lucide <c>at-sign</c> icon.</summary>
        public static ComponentResourceKey AtSign { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AtSign));

        /// <summary>Lucide <c>atom</c> icon.</summary>
        public static ComponentResourceKey Atom { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Atom));

        /// <summary>Lucide <c>audio-lines</c> icon.</summary>
        public static ComponentResourceKey AudioLines { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AudioLines));

        /// <summary>Lucide <c>audio-waveform</c> icon.</summary>
        public static ComponentResourceKey AudioWaveform { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(AudioWaveform));

        /// <summary>Lucide <c>award</c> icon.</summary>
        public static ComponentResourceKey Award { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Award));

        /// <summary>Lucide <c>axe</c> icon.</summary>
        public static ComponentResourceKey Axe { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Axe));

        /// <summary>Lucide <c>axis-3d</c> icon.</summary>
        public static ComponentResourceKey Axis3d { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Axis3d));

        // ─── B ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>baby</c> icon.</summary>
        public static ComponentResourceKey Baby { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Baby));

        /// <summary>Lucide <c>backpack</c> icon.</summary>
        public static ComponentResourceKey Backpack { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Backpack));

        /// <summary>Lucide <c>badge-alert</c> icon.</summary>
        public static ComponentResourceKey BadgeAlert { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgeAlert));

        /// <summary>Lucide <c>badge-cent</c> icon.</summary>
        public static ComponentResourceKey BadgeCent { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgeCent));

        /// <summary>Lucide <c>badge-check</c> icon.</summary>
        public static ComponentResourceKey BadgeCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgeCheck));

        /// <summary>Lucide <c>badge-dollar-sign</c> icon.</summary>
        public static ComponentResourceKey BadgeDollarSign { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgeDollarSign));

        /// <summary>Lucide <c>badge-euro</c> icon.</summary>
        public static ComponentResourceKey BadgeEuro { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgeEuro));

        /// <summary>Lucide <c>badge-indian-rupee</c> icon.</summary>
        public static ComponentResourceKey BadgeIndianRupee { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgeIndianRupee));

        /// <summary>Lucide <c>badge-info</c> icon.</summary>
        public static ComponentResourceKey BadgeInfo { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgeInfo));

        /// <summary>Lucide <c>badge-japanese-yen</c> icon.</summary>
        public static ComponentResourceKey BadgeJapaneseYen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgeJapaneseYen));

        /// <summary>Lucide <c>badge-minus</c> icon.</summary>
        public static ComponentResourceKey BadgeMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgeMinus));

        /// <summary>Lucide <c>badge-percent</c> icon.</summary>
        public static ComponentResourceKey BadgePercent { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgePercent));

        /// <summary>Lucide <c>badge-plus</c> icon.</summary>
        public static ComponentResourceKey BadgePlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgePlus));

        /// <summary>Lucide <c>badge-pound-sterling</c> icon.</summary>
        public static ComponentResourceKey BadgePoundSterling { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgePoundSterling));

        /// <summary>Lucide <c>badge-question-mark</c> icon.</summary>
        public static ComponentResourceKey BadgeQuestionMark { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgeQuestionMark));

        /// <summary>Lucide <c>badge-russian-ruble</c> icon.</summary>
        public static ComponentResourceKey BadgeRussianRuble { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgeRussianRuble));

        /// <summary>Lucide <c>badge-swiss-franc</c> icon.</summary>
        public static ComponentResourceKey BadgeSwissFranc { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgeSwissFranc));

        /// <summary>Lucide <c>badge-turkish-lira</c> icon.</summary>
        public static ComponentResourceKey BadgeTurkishLira { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgeTurkishLira));

        /// <summary>Lucide <c>badge-x</c> icon.</summary>
        public static ComponentResourceKey BadgeX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BadgeX));

        /// <summary>Lucide <c>badge</c> icon.</summary>
        public static ComponentResourceKey Badge { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Badge));

        /// <summary>Lucide <c>baggage-claim</c> icon.</summary>
        public static ComponentResourceKey BaggageClaim { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BaggageClaim));

        /// <summary>Lucide <c>balloon</c> icon.</summary>
        public static ComponentResourceKey Balloon { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Balloon));

        /// <summary>Lucide <c>ban</c> icon.</summary>
        public static ComponentResourceKey Ban { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ban));

        /// <summary>Lucide <c>banana</c> icon.</summary>
        public static ComponentResourceKey Banana { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Banana));

        /// <summary>Lucide <c>bandage</c> icon.</summary>
        public static ComponentResourceKey Bandage { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bandage));

        /// <summary>Lucide <c>banknote-arrow-down</c> icon.</summary>
        public static ComponentResourceKey BanknoteArrowDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BanknoteArrowDown));

        /// <summary>Lucide <c>banknote-arrow-up</c> icon.</summary>
        public static ComponentResourceKey BanknoteArrowUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BanknoteArrowUp));

        /// <summary>Lucide <c>banknote-check</c> icon.</summary>
        public static ComponentResourceKey BanknoteCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BanknoteCheck));

        /// <summary>Lucide <c>banknote-x</c> icon.</summary>
        public static ComponentResourceKey BanknoteX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BanknoteX));

        /// <summary>Lucide <c>banknote</c> icon.</summary>
        public static ComponentResourceKey Banknote { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Banknote));

        /// <summary>Lucide <c>barcode</c> icon.</summary>
        public static ComponentResourceKey Barcode { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Barcode));

        /// <summary>Lucide <c>barrel</c> icon.</summary>
        public static ComponentResourceKey Barrel { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Barrel));

        /// <summary>Lucide <c>baseline</c> icon.</summary>
        public static ComponentResourceKey Baseline { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Baseline));

        /// <summary>Lucide <c>bath</c> icon.</summary>
        public static ComponentResourceKey Bath { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bath));

        /// <summary>Lucide <c>battery-charging</c> icon.</summary>
        public static ComponentResourceKey BatteryCharging { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BatteryCharging));

        /// <summary>Lucide <c>battery-full</c> icon.</summary>
        public static ComponentResourceKey BatteryFull { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BatteryFull));

        /// <summary>Lucide <c>battery-low</c> icon.</summary>
        public static ComponentResourceKey BatteryLow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BatteryLow));

        /// <summary>Lucide <c>battery-medium</c> icon.</summary>
        public static ComponentResourceKey BatteryMedium { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BatteryMedium));

        /// <summary>Lucide <c>battery-plus</c> icon.</summary>
        public static ComponentResourceKey BatteryPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BatteryPlus));

        /// <summary>Lucide <c>battery-warning</c> icon.</summary>
        public static ComponentResourceKey BatteryWarning { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BatteryWarning));

        /// <summary>Lucide <c>battery</c> icon.</summary>
        public static ComponentResourceKey Battery { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Battery));

        /// <summary>Lucide <c>beaker</c> icon.</summary>
        public static ComponentResourceKey Beaker { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Beaker));

        /// <summary>Lucide <c>bean-off</c> icon.</summary>
        public static ComponentResourceKey BeanOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BeanOff));

        /// <summary>Lucide <c>bean</c> icon.</summary>
        public static ComponentResourceKey Bean { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bean));

        /// <summary>Lucide <c>bed-double</c> icon.</summary>
        public static ComponentResourceKey BedDouble { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BedDouble));

        /// <summary>Lucide <c>bed-single</c> icon.</summary>
        public static ComponentResourceKey BedSingle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BedSingle));

        /// <summary>Lucide <c>bed</c> icon.</summary>
        public static ComponentResourceKey Bed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bed));

        /// <summary>Lucide <c>beef-off</c> icon.</summary>
        public static ComponentResourceKey BeefOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BeefOff));

        /// <summary>Lucide <c>beef</c> icon.</summary>
        public static ComponentResourceKey Beef { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Beef));

        /// <summary>Lucide <c>beer-off</c> icon.</summary>
        public static ComponentResourceKey BeerOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BeerOff));

        /// <summary>Lucide <c>beer</c> icon.</summary>
        public static ComponentResourceKey Beer { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Beer));

        /// <summary>Lucide <c>bell-check</c> icon.</summary>
        public static ComponentResourceKey BellCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BellCheck));

        /// <summary>Lucide <c>bell-dot</c> icon.</summary>
        public static ComponentResourceKey BellDot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BellDot));

        /// <summary>Lucide <c>bell-electric</c> icon.</summary>
        public static ComponentResourceKey BellElectric { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BellElectric));

        /// <summary>Lucide <c>bell-minus</c> icon.</summary>
        public static ComponentResourceKey BellMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BellMinus));

        /// <summary>Lucide <c>bell-off</c> icon.</summary>
        public static ComponentResourceKey BellOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BellOff));

        /// <summary>Lucide <c>bell-plus</c> icon.</summary>
        public static ComponentResourceKey BellPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BellPlus));

        /// <summary>Lucide <c>bell-ring</c> icon.</summary>
        public static ComponentResourceKey BellRing { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BellRing));

        /// <summary>Lucide <c>bell</c> icon.</summary>
        public static ComponentResourceKey Bell { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bell));

        /// <summary>Lucide <c>between-horizontal-end</c> icon.</summary>
        public static ComponentResourceKey BetweenHorizontalEnd { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BetweenHorizontalEnd));

        /// <summary>Lucide <c>between-horizontal-start</c> icon.</summary>
        public static ComponentResourceKey BetweenHorizontalStart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BetweenHorizontalStart));

        /// <summary>Lucide <c>between-vertical-end</c> icon.</summary>
        public static ComponentResourceKey BetweenVerticalEnd { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BetweenVerticalEnd));

        /// <summary>Lucide <c>between-vertical-start</c> icon.</summary>
        public static ComponentResourceKey BetweenVerticalStart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BetweenVerticalStart));

        /// <summary>Lucide <c>biceps-flexed</c> icon.</summary>
        public static ComponentResourceKey BicepsFlexed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BicepsFlexed));

        /// <summary>Lucide <c>bike</c> icon.</summary>
        public static ComponentResourceKey Bike { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bike));

        /// <summary>Lucide <c>binary</c> icon.</summary>
        public static ComponentResourceKey Binary { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Binary));

        /// <summary>Lucide <c>binoculars</c> icon.</summary>
        public static ComponentResourceKey Binoculars { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Binoculars));

        /// <summary>Lucide <c>biohazard</c> icon.</summary>
        public static ComponentResourceKey Biohazard { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Biohazard));

        /// <summary>Lucide <c>bird</c> icon.</summary>
        public static ComponentResourceKey Bird { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bird));

        /// <summary>Lucide <c>birdhouse</c> icon.</summary>
        public static ComponentResourceKey Birdhouse { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Birdhouse));

        /// <summary>Lucide <c>bitcoin</c> icon.</summary>
        public static ComponentResourceKey Bitcoin { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bitcoin));

        /// <summary>Lucide <c>blend</c> icon.</summary>
        public static ComponentResourceKey Blend { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Blend));

        /// <summary>Lucide <c>blender</c> icon.</summary>
        public static ComponentResourceKey Blender { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Blender));

        /// <summary>Lucide <c>blinds</c> icon.</summary>
        public static ComponentResourceKey Blinds { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Blinds));

        /// <summary>Lucide <c>blocks</c> icon.</summary>
        public static ComponentResourceKey Blocks { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Blocks));

        /// <summary>Lucide <c>bluetooth-connected</c> icon.</summary>
        public static ComponentResourceKey BluetoothConnected { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BluetoothConnected));

        /// <summary>Lucide <c>bluetooth-off</c> icon.</summary>
        public static ComponentResourceKey BluetoothOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BluetoothOff));

        /// <summary>Lucide <c>bluetooth-searching</c> icon.</summary>
        public static ComponentResourceKey BluetoothSearching { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BluetoothSearching));

        /// <summary>Lucide <c>bluetooth</c> icon.</summary>
        public static ComponentResourceKey Bluetooth { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bluetooth));

        /// <summary>Lucide <c>bold</c> icon.</summary>
        public static ComponentResourceKey Bold { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bold));

        /// <summary>Lucide <c>bolt</c> icon.</summary>
        public static ComponentResourceKey Bolt { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bolt));

        /// <summary>Lucide <c>bomb</c> icon.</summary>
        public static ComponentResourceKey Bomb { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bomb));

        /// <summary>Lucide <c>bone-fracture</c> icon.</summary>
        public static ComponentResourceKey BoneFracture { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BoneFracture));

        /// <summary>Lucide <c>bone</c> icon.</summary>
        public static ComponentResourceKey Bone { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bone));

        /// <summary>Lucide <c>book-a</c> icon.</summary>
        public static ComponentResourceKey BookA { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookA));

        /// <summary>Lucide <c>book-alert</c> icon.</summary>
        public static ComponentResourceKey BookAlert { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookAlert));

        /// <summary>Lucide <c>book-audio</c> icon.</summary>
        public static ComponentResourceKey BookAudio { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookAudio));

        /// <summary>Lucide <c>book-check</c> icon.</summary>
        public static ComponentResourceKey BookCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookCheck));

        /// <summary>Lucide <c>book-copy</c> icon.</summary>
        public static ComponentResourceKey BookCopy { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookCopy));

        /// <summary>Lucide <c>book-dashed</c> icon.</summary>
        public static ComponentResourceKey BookDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookDashed));

        /// <summary>Lucide <c>book-down</c> icon.</summary>
        public static ComponentResourceKey BookDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookDown));

        /// <summary>Lucide <c>book-headphones</c> icon.</summary>
        public static ComponentResourceKey BookHeadphones { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookHeadphones));

        /// <summary>Lucide <c>book-heart</c> icon.</summary>
        public static ComponentResourceKey BookHeart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookHeart));

        /// <summary>Lucide <c>book-image</c> icon.</summary>
        public static ComponentResourceKey BookImage { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookImage));

        /// <summary>Lucide <c>book-key</c> icon.</summary>
        public static ComponentResourceKey BookKey { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookKey));

        /// <summary>Lucide <c>book-lock</c> icon.</summary>
        public static ComponentResourceKey BookLock { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookLock));

        /// <summary>Lucide <c>book-marked</c> icon.</summary>
        public static ComponentResourceKey BookMarked { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookMarked));

        /// <summary>Lucide <c>book-minus</c> icon.</summary>
        public static ComponentResourceKey BookMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookMinus));

        /// <summary>Lucide <c>book-open-check</c> icon.</summary>
        public static ComponentResourceKey BookOpenCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookOpenCheck));

        /// <summary>Lucide <c>book-open-text</c> icon.</summary>
        public static ComponentResourceKey BookOpenText { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookOpenText));

        /// <summary>Lucide <c>book-open</c> icon.</summary>
        public static ComponentResourceKey BookOpen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookOpen));

        /// <summary>Lucide <c>book-plus</c> icon.</summary>
        public static ComponentResourceKey BookPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookPlus));

        /// <summary>Lucide <c>book-search</c> icon.</summary>
        public static ComponentResourceKey BookSearch { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookSearch));

        /// <summary>Lucide <c>book-text</c> icon.</summary>
        public static ComponentResourceKey BookText { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookText));

        /// <summary>Lucide <c>book-type</c> icon.</summary>
        public static ComponentResourceKey BookType { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookType));

        /// <summary>Lucide <c>book-up-2</c> icon.</summary>
        public static ComponentResourceKey BookUp2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookUp2));

        /// <summary>Lucide <c>book-up</c> icon.</summary>
        public static ComponentResourceKey BookUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookUp));

        /// <summary>Lucide <c>book-user</c> icon.</summary>
        public static ComponentResourceKey BookUser { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookUser));

        /// <summary>Lucide <c>book-x</c> icon.</summary>
        public static ComponentResourceKey BookX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookX));

        /// <summary>Lucide <c>book</c> icon.</summary>
        public static ComponentResourceKey Book { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Book));

        /// <summary>Lucide <c>bookmark-check</c> icon.</summary>
        public static ComponentResourceKey BookmarkCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookmarkCheck));

        /// <summary>Lucide <c>bookmark-minus</c> icon.</summary>
        public static ComponentResourceKey BookmarkMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookmarkMinus));

        /// <summary>Lucide <c>bookmark-off</c> icon.</summary>
        public static ComponentResourceKey BookmarkOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookmarkOff));

        /// <summary>Lucide <c>bookmark-plus</c> icon.</summary>
        public static ComponentResourceKey BookmarkPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookmarkPlus));

        /// <summary>Lucide <c>bookmark-x</c> icon.</summary>
        public static ComponentResourceKey BookmarkX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BookmarkX));

        /// <summary>Lucide <c>bookmark</c> icon.</summary>
        public static ComponentResourceKey Bookmark { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bookmark));

        /// <summary>Lucide <c>boom-box</c> icon.</summary>
        public static ComponentResourceKey BoomBox { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BoomBox));

        /// <summary>Lucide <c>bot-message-square</c> icon.</summary>
        public static ComponentResourceKey BotMessageSquare { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BotMessageSquare));

        /// <summary>Lucide <c>bot-off</c> icon.</summary>
        public static ComponentResourceKey BotOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BotOff));

        /// <summary>Lucide <c>bot</c> icon.</summary>
        public static ComponentResourceKey Bot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bot));

        /// <summary>Lucide <c>bottle-wine</c> icon.</summary>
        public static ComponentResourceKey BottleWine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BottleWine));

        /// <summary>Lucide <c>bow-arrow</c> icon.</summary>
        public static ComponentResourceKey BowArrow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BowArrow));

        /// <summary>Lucide <c>box</c> icon.</summary>
        public static ComponentResourceKey Box { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Box));

        /// <summary>Lucide <c>boxes</c> icon.</summary>
        public static ComponentResourceKey Boxes { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Boxes));

        /// <summary>Lucide <c>braces</c> icon.</summary>
        public static ComponentResourceKey Braces { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Braces));

        /// <summary>Lucide <c>brackets</c> icon.</summary>
        public static ComponentResourceKey Brackets { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Brackets));

        /// <summary>Lucide <c>brain-circuit</c> icon.</summary>
        public static ComponentResourceKey BrainCircuit { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BrainCircuit));

        /// <summary>Lucide <c>brain-cog</c> icon.</summary>
        public static ComponentResourceKey BrainCog { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BrainCog));

        /// <summary>Lucide <c>brain</c> icon.</summary>
        public static ComponentResourceKey Brain { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Brain));

        /// <summary>Lucide <c>brick-wall-fire</c> icon.</summary>
        public static ComponentResourceKey BrickWallFire { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BrickWallFire));

        /// <summary>Lucide <c>brick-wall-shield</c> icon.</summary>
        public static ComponentResourceKey BrickWallShield { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BrickWallShield));

        /// <summary>Lucide <c>brick-wall</c> icon.</summary>
        public static ComponentResourceKey BrickWall { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BrickWall));

        /// <summary>Lucide <c>briefcase-business</c> icon.</summary>
        public static ComponentResourceKey BriefcaseBusiness { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BriefcaseBusiness));

        /// <summary>Lucide <c>briefcase-conveyor-belt</c> icon.</summary>
        public static ComponentResourceKey BriefcaseConveyorBelt { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BriefcaseConveyorBelt));

        /// <summary>Lucide <c>briefcase-medical</c> icon.</summary>
        public static ComponentResourceKey BriefcaseMedical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BriefcaseMedical));

        /// <summary>Lucide <c>briefcase</c> icon.</summary>
        public static ComponentResourceKey Briefcase { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Briefcase));

        /// <summary>Lucide <c>bring-to-front</c> icon.</summary>
        public static ComponentResourceKey BringToFront { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BringToFront));

        /// <summary>Lucide <c>broccoli</c> icon.</summary>
        public static ComponentResourceKey Broccoli { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Broccoli));

        /// <summary>Lucide <c>brush-cleaning</c> icon.</summary>
        public static ComponentResourceKey BrushCleaning { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BrushCleaning));

        /// <summary>Lucide <c>brush</c> icon.</summary>
        public static ComponentResourceKey Brush { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Brush));

        /// <summary>Lucide <c>bubbles</c> icon.</summary>
        public static ComponentResourceKey Bubbles { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bubbles));

        /// <summary>Lucide <c>bug-off</c> icon.</summary>
        public static ComponentResourceKey BugOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BugOff));

        /// <summary>Lucide <c>bug-play</c> icon.</summary>
        public static ComponentResourceKey BugPlay { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BugPlay));

        /// <summary>Lucide <c>bug</c> icon.</summary>
        public static ComponentResourceKey Bug { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bug));

        /// <summary>Lucide <c>building-2</c> icon.</summary>
        public static ComponentResourceKey Building2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Building2));

        /// <summary>Lucide <c>building</c> icon.</summary>
        public static ComponentResourceKey Building { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Building));

        /// <summary>Lucide <c>bus-front</c> icon.</summary>
        public static ComponentResourceKey BusFront { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(BusFront));

        /// <summary>Lucide <c>bus</c> icon.</summary>
        public static ComponentResourceKey Bus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Bus));

        // ─── C ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>cable-car</c> icon.</summary>
        public static ComponentResourceKey CableCar { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CableCar));

        /// <summary>Lucide <c>cable</c> icon.</summary>
        public static ComponentResourceKey Cable { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cable));

        /// <summary>Lucide <c>cake-slice</c> icon.</summary>
        public static ComponentResourceKey CakeSlice { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CakeSlice));

        /// <summary>Lucide <c>cake</c> icon.</summary>
        public static ComponentResourceKey Cake { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cake));

        /// <summary>Lucide <c>calculator</c> icon.</summary>
        public static ComponentResourceKey Calculator { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Calculator));

        /// <summary>Lucide <c>calendar-1</c> icon.</summary>
        public static ComponentResourceKey Calendar1 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Calendar1));

        /// <summary>Lucide <c>calendar-arrow-down</c> icon.</summary>
        public static ComponentResourceKey CalendarArrowDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarArrowDown));

        /// <summary>Lucide <c>calendar-arrow-up</c> icon.</summary>
        public static ComponentResourceKey CalendarArrowUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarArrowUp));

        /// <summary>Lucide <c>calendar-check-2</c> icon.</summary>
        public static ComponentResourceKey CalendarCheck2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarCheck2));

        /// <summary>Lucide <c>calendar-check</c> icon.</summary>
        public static ComponentResourceKey CalendarCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarCheck));

        /// <summary>Lucide <c>calendar-clock</c> icon.</summary>
        public static ComponentResourceKey CalendarClock { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarClock));

        /// <summary>Lucide <c>calendar-cog</c> icon.</summary>
        public static ComponentResourceKey CalendarCog { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarCog));

        /// <summary>Lucide <c>calendar-days</c> icon.</summary>
        public static ComponentResourceKey CalendarDays { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarDays));

        /// <summary>Lucide <c>calendar-fold</c> icon.</summary>
        public static ComponentResourceKey CalendarFold { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarFold));

        /// <summary>Lucide <c>calendar-heart</c> icon.</summary>
        public static ComponentResourceKey CalendarHeart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarHeart));

        /// <summary>Lucide <c>calendar-minus-2</c> icon.</summary>
        public static ComponentResourceKey CalendarMinus2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarMinus2));

        /// <summary>Lucide <c>calendar-minus</c> icon.</summary>
        public static ComponentResourceKey CalendarMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarMinus));

        /// <summary>Lucide <c>calendar-off</c> icon.</summary>
        public static ComponentResourceKey CalendarOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarOff));

        /// <summary>Lucide <c>calendar-plus-2</c> icon.</summary>
        public static ComponentResourceKey CalendarPlus2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarPlus2));

        /// <summary>Lucide <c>calendar-plus</c> icon.</summary>
        public static ComponentResourceKey CalendarPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarPlus));

        /// <summary>Lucide <c>calendar-range</c> icon.</summary>
        public static ComponentResourceKey CalendarRange { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarRange));

        /// <summary>Lucide <c>calendar-search</c> icon.</summary>
        public static ComponentResourceKey CalendarSearch { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarSearch));

        /// <summary>Lucide <c>calendar-sync</c> icon.</summary>
        public static ComponentResourceKey CalendarSync { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarSync));

        /// <summary>Lucide <c>calendar-x-2</c> icon.</summary>
        public static ComponentResourceKey CalendarX2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarX2));

        /// <summary>Lucide <c>calendar-x</c> icon.</summary>
        public static ComponentResourceKey CalendarX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CalendarX));

        /// <summary>Lucide <c>calendar</c> icon.</summary>
        public static ComponentResourceKey Calendar { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Calendar));

        /// <summary>Lucide <c>calendars</c> icon.</summary>
        public static ComponentResourceKey Calendars { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Calendars));

        /// <summary>Lucide <c>camera-off</c> icon.</summary>
        public static ComponentResourceKey CameraOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CameraOff));

        /// <summary>Lucide <c>camera</c> icon.</summary>
        public static ComponentResourceKey Camera { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Camera));

        /// <summary>Lucide <c>candy-cane</c> icon.</summary>
        public static ComponentResourceKey CandyCane { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CandyCane));

        /// <summary>Lucide <c>candy-off</c> icon.</summary>
        public static ComponentResourceKey CandyOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CandyOff));

        /// <summary>Lucide <c>candy</c> icon.</summary>
        public static ComponentResourceKey Candy { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Candy));

        /// <summary>Lucide <c>cannabis-off</c> icon.</summary>
        public static ComponentResourceKey CannabisOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CannabisOff));

        /// <summary>Lucide <c>cannabis</c> icon.</summary>
        public static ComponentResourceKey Cannabis { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cannabis));

        /// <summary>Lucide <c>captions-off</c> icon.</summary>
        public static ComponentResourceKey CaptionsOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CaptionsOff));

        /// <summary>Lucide <c>captions</c> icon.</summary>
        public static ComponentResourceKey Captions { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Captions));

        /// <summary>Lucide <c>car-front</c> icon.</summary>
        public static ComponentResourceKey CarFront { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CarFront));

        /// <summary>Lucide <c>car-taxi-front</c> icon.</summary>
        public static ComponentResourceKey CarTaxiFront { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CarTaxiFront));

        /// <summary>Lucide <c>car</c> icon.</summary>
        public static ComponentResourceKey Car { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Car));

        /// <summary>Lucide <c>caravan</c> icon.</summary>
        public static ComponentResourceKey Caravan { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Caravan));

        /// <summary>Lucide <c>card-sim</c> icon.</summary>
        public static ComponentResourceKey CardSim { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CardSim));

        /// <summary>Lucide <c>carrot</c> icon.</summary>
        public static ComponentResourceKey Carrot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Carrot));

        /// <summary>Lucide <c>case-lower</c> icon.</summary>
        public static ComponentResourceKey CaseLower { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CaseLower));

        /// <summary>Lucide <c>case-sensitive</c> icon.</summary>
        public static ComponentResourceKey CaseSensitive { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CaseSensitive));

        /// <summary>Lucide <c>case-upper</c> icon.</summary>
        public static ComponentResourceKey CaseUpper { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CaseUpper));

        /// <summary>Lucide <c>cassette-tape</c> icon.</summary>
        public static ComponentResourceKey CassetteTape { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CassetteTape));

        /// <summary>Lucide <c>cast</c> icon.</summary>
        public static ComponentResourceKey Cast { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cast));

        /// <summary>Lucide <c>castle</c> icon.</summary>
        public static ComponentResourceKey Castle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Castle));

        /// <summary>Lucide <c>cat</c> icon.</summary>
        public static ComponentResourceKey Cat { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cat));

        /// <summary>Lucide <c>cctv-off</c> icon.</summary>
        public static ComponentResourceKey CctvOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CctvOff));

        /// <summary>Lucide <c>cctv</c> icon.</summary>
        public static ComponentResourceKey Cctv { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cctv));

        /// <summary>Lucide <c>chart-area</c> icon.</summary>
        public static ComponentResourceKey ChartArea { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartArea));

        /// <summary>Lucide <c>chart-bar-big</c> icon.</summary>
        public static ComponentResourceKey ChartBarBig { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartBarBig));

        /// <summary>Lucide <c>chart-bar-decreasing</c> icon.</summary>
        public static ComponentResourceKey ChartBarDecreasing { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartBarDecreasing));

        /// <summary>Lucide <c>chart-bar-increasing</c> icon.</summary>
        public static ComponentResourceKey ChartBarIncreasing { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartBarIncreasing));

        /// <summary>Lucide <c>chart-bar-stacked</c> icon.</summary>
        public static ComponentResourceKey ChartBarStacked { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartBarStacked));

        /// <summary>Lucide <c>chart-bar</c> icon.</summary>
        public static ComponentResourceKey ChartBar { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartBar));

        /// <summary>Lucide <c>chart-candlestick</c> icon.</summary>
        public static ComponentResourceKey ChartCandlestick { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartCandlestick));

        /// <summary>Lucide <c>chart-column-big</c> icon.</summary>
        public static ComponentResourceKey ChartColumnBig { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartColumnBig));

        /// <summary>Lucide <c>chart-column-decreasing</c> icon.</summary>
        public static ComponentResourceKey ChartColumnDecreasing { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartColumnDecreasing));

        /// <summary>Lucide <c>chart-column-increasing</c> icon.</summary>
        public static ComponentResourceKey ChartColumnIncreasing { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartColumnIncreasing));

        /// <summary>Lucide <c>chart-column-stacked</c> icon.</summary>
        public static ComponentResourceKey ChartColumnStacked { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartColumnStacked));

        /// <summary>Lucide <c>chart-column</c> icon.</summary>
        public static ComponentResourceKey ChartColumn { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartColumn));

        /// <summary>Lucide <c>chart-gantt</c> icon.</summary>
        public static ComponentResourceKey ChartGantt { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartGantt));

        /// <summary>Lucide <c>chart-line</c> icon.</summary>
        public static ComponentResourceKey ChartLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartLine));

        /// <summary>Lucide <c>chart-network</c> icon.</summary>
        public static ComponentResourceKey ChartNetwork { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartNetwork));

        /// <summary>Lucide <c>chart-no-axes-column-decreasing</c> icon.</summary>
        public static ComponentResourceKey ChartNoAxesColumnDecreasing { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartNoAxesColumnDecreasing));

        /// <summary>Lucide <c>chart-no-axes-column-increasing</c> icon.</summary>
        public static ComponentResourceKey ChartNoAxesColumnIncreasing { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartNoAxesColumnIncreasing));

        /// <summary>Lucide <c>chart-no-axes-column</c> icon.</summary>
        public static ComponentResourceKey ChartNoAxesColumn { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartNoAxesColumn));

        /// <summary>Lucide <c>chart-no-axes-combined</c> icon.</summary>
        public static ComponentResourceKey ChartNoAxesCombined { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartNoAxesCombined));

        /// <summary>Lucide <c>chart-no-axes-gantt</c> icon.</summary>
        public static ComponentResourceKey ChartNoAxesGantt { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartNoAxesGantt));

        /// <summary>Lucide <c>chart-pie</c> icon.</summary>
        public static ComponentResourceKey ChartPie { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartPie));

        /// <summary>Lucide <c>chart-scatter</c> icon.</summary>
        public static ComponentResourceKey ChartScatter { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartScatter));

        /// <summary>Lucide <c>chart-spline</c> icon.</summary>
        public static ComponentResourceKey ChartSpline { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChartSpline));

        /// <summary>Lucide <c>check-check</c> icon.</summary>
        public static ComponentResourceKey CheckCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CheckCheck));

        /// <summary>Lucide <c>check-line</c> icon.</summary>
        public static ComponentResourceKey CheckLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CheckLine));

        /// <summary>Lucide <c>check</c> icon.</summary>
        public static ComponentResourceKey Check { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Check));

        /// <summary>Lucide <c>chef-hat</c> icon.</summary>
        public static ComponentResourceKey ChefHat { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChefHat));

        /// <summary>Lucide <c>cherry</c> icon.</summary>
        public static ComponentResourceKey Cherry { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cherry));

        /// <summary>Lucide <c>chess-bishop</c> icon.</summary>
        public static ComponentResourceKey ChessBishop { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChessBishop));

        /// <summary>Lucide <c>chess-king</c> icon.</summary>
        public static ComponentResourceKey ChessKing { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChessKing));

        /// <summary>Lucide <c>chess-knight</c> icon.</summary>
        public static ComponentResourceKey ChessKnight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChessKnight));

        /// <summary>Lucide <c>chess-pawn</c> icon.</summary>
        public static ComponentResourceKey ChessPawn { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChessPawn));

        /// <summary>Lucide <c>chess-queen</c> icon.</summary>
        public static ComponentResourceKey ChessQueen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChessQueen));

        /// <summary>Lucide <c>chess-rook</c> icon.</summary>
        public static ComponentResourceKey ChessRook { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChessRook));

        /// <summary>Lucide <c>chevron-down</c> icon.</summary>
        public static ComponentResourceKey ChevronDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChevronDown));

        /// <summary>Lucide <c>chevron-first</c> icon.</summary>
        public static ComponentResourceKey ChevronFirst { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChevronFirst));

        /// <summary>Lucide <c>chevron-last</c> icon.</summary>
        public static ComponentResourceKey ChevronLast { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChevronLast));

        /// <summary>Lucide <c>chevron-left</c> icon.</summary>
        public static ComponentResourceKey ChevronLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChevronLeft));

        /// <summary>Lucide <c>chevron-right</c> icon.</summary>
        public static ComponentResourceKey ChevronRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChevronRight));

        /// <summary>Lucide <c>chevron-up</c> icon.</summary>
        public static ComponentResourceKey ChevronUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChevronUp));

        /// <summary>Lucide <c>chevrons-down-up</c> icon.</summary>
        public static ComponentResourceKey ChevronsDownUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChevronsDownUp));

        /// <summary>Lucide <c>chevrons-down</c> icon.</summary>
        public static ComponentResourceKey ChevronsDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChevronsDown));

        /// <summary>Lucide <c>chevrons-left-right-ellipsis</c> icon.</summary>
        public static ComponentResourceKey ChevronsLeftRightEllipsis { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChevronsLeftRightEllipsis));

        /// <summary>Lucide <c>chevrons-left-right</c> icon.</summary>
        public static ComponentResourceKey ChevronsLeftRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChevronsLeftRight));

        /// <summary>Lucide <c>chevrons-left</c> icon.</summary>
        public static ComponentResourceKey ChevronsLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChevronsLeft));

        /// <summary>Lucide <c>chevrons-right-left</c> icon.</summary>
        public static ComponentResourceKey ChevronsRightLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChevronsRightLeft));

        /// <summary>Lucide <c>chevrons-right</c> icon.</summary>
        public static ComponentResourceKey ChevronsRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChevronsRight));

        /// <summary>Lucide <c>chevrons-up-down</c> icon.</summary>
        public static ComponentResourceKey ChevronsUpDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChevronsUpDown));

        /// <summary>Lucide <c>chevrons-up</c> icon.</summary>
        public static ComponentResourceKey ChevronsUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ChevronsUp));

        /// <summary>Lucide <c>church</c> icon.</summary>
        public static ComponentResourceKey Church { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Church));

        /// <summary>Lucide <c>cigarette-off</c> icon.</summary>
        public static ComponentResourceKey CigaretteOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CigaretteOff));

        /// <summary>Lucide <c>cigarette</c> icon.</summary>
        public static ComponentResourceKey Cigarette { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cigarette));

        /// <summary>Lucide <c>circle-alert</c> icon.</summary>
        public static ComponentResourceKey CircleAlert { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleAlert));

        /// <summary>Lucide <c>circle-arrow-down</c> icon.</summary>
        public static ComponentResourceKey CircleArrowDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleArrowDown));

        /// <summary>Lucide <c>circle-arrow-left</c> icon.</summary>
        public static ComponentResourceKey CircleArrowLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleArrowLeft));

        /// <summary>Lucide <c>circle-arrow-out-down-left</c> icon.</summary>
        public static ComponentResourceKey CircleArrowOutDownLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleArrowOutDownLeft));

        /// <summary>Lucide <c>circle-arrow-out-down-right</c> icon.</summary>
        public static ComponentResourceKey CircleArrowOutDownRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleArrowOutDownRight));

        /// <summary>Lucide <c>circle-arrow-out-up-left</c> icon.</summary>
        public static ComponentResourceKey CircleArrowOutUpLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleArrowOutUpLeft));

        /// <summary>Lucide <c>circle-arrow-out-up-right</c> icon.</summary>
        public static ComponentResourceKey CircleArrowOutUpRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleArrowOutUpRight));

        /// <summary>Lucide <c>circle-arrow-right</c> icon.</summary>
        public static ComponentResourceKey CircleArrowRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleArrowRight));

        /// <summary>Lucide <c>circle-arrow-up</c> icon.</summary>
        public static ComponentResourceKey CircleArrowUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleArrowUp));

        /// <summary>Lucide <c>circle-check-big</c> icon.</summary>
        public static ComponentResourceKey CircleCheckBig { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleCheckBig));

        /// <summary>Lucide <c>circle-check</c> icon.</summary>
        public static ComponentResourceKey CircleCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleCheck));

        /// <summary>Lucide <c>circle-chevron-down</c> icon.</summary>
        public static ComponentResourceKey CircleChevronDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleChevronDown));

        /// <summary>Lucide <c>circle-chevron-left</c> icon.</summary>
        public static ComponentResourceKey CircleChevronLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleChevronLeft));

        /// <summary>Lucide <c>circle-chevron-right</c> icon.</summary>
        public static ComponentResourceKey CircleChevronRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleChevronRight));

        /// <summary>Lucide <c>circle-chevron-up</c> icon.</summary>
        public static ComponentResourceKey CircleChevronUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleChevronUp));

        /// <summary>Lucide <c>circle-dashed</c> icon.</summary>
        public static ComponentResourceKey CircleDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleDashed));

        /// <summary>Lucide <c>circle-divide</c> icon.</summary>
        public static ComponentResourceKey CircleDivide { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleDivide));

        /// <summary>Lucide <c>circle-dollar-sign</c> icon.</summary>
        public static ComponentResourceKey CircleDollarSign { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleDollarSign));

        /// <summary>Lucide <c>circle-dot-dashed</c> icon.</summary>
        public static ComponentResourceKey CircleDotDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleDotDashed));

        /// <summary>Lucide <c>circle-dot</c> icon.</summary>
        public static ComponentResourceKey CircleDot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleDot));

        /// <summary>Lucide <c>circle-ellipsis</c> icon.</summary>
        public static ComponentResourceKey CircleEllipsis { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleEllipsis));

        /// <summary>Lucide <c>circle-equal</c> icon.</summary>
        public static ComponentResourceKey CircleEqual { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleEqual));

        /// <summary>Lucide <c>circle-euro</c> icon.</summary>
        public static ComponentResourceKey CircleEuro { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleEuro));

        /// <summary>Lucide <c>circle-fading-arrow-up</c> icon.</summary>
        public static ComponentResourceKey CircleFadingArrowUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleFadingArrowUp));

        /// <summary>Lucide <c>circle-fading-plus</c> icon.</summary>
        public static ComponentResourceKey CircleFadingPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleFadingPlus));

        /// <summary>Lucide <c>circle-gauge</c> icon.</summary>
        public static ComponentResourceKey CircleGauge { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleGauge));

        /// <summary>Lucide <c>circle-minus</c> icon.</summary>
        public static ComponentResourceKey CircleMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleMinus));

        /// <summary>Lucide <c>circle-off</c> icon.</summary>
        public static ComponentResourceKey CircleOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleOff));

        /// <summary>Lucide <c>circle-parking-off</c> icon.</summary>
        public static ComponentResourceKey CircleParkingOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleParkingOff));

        /// <summary>Lucide <c>circle-parking</c> icon.</summary>
        public static ComponentResourceKey CircleParking { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleParking));

        /// <summary>Lucide <c>circle-pause</c> icon.</summary>
        public static ComponentResourceKey CirclePause { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CirclePause));

        /// <summary>Lucide <c>circle-percent</c> icon.</summary>
        public static ComponentResourceKey CirclePercent { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CirclePercent));

        /// <summary>Lucide <c>circle-pile</c> icon.</summary>
        public static ComponentResourceKey CirclePile { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CirclePile));

        /// <summary>Lucide <c>circle-play</c> icon.</summary>
        public static ComponentResourceKey CirclePlay { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CirclePlay));

        /// <summary>Lucide <c>circle-plus</c> icon.</summary>
        public static ComponentResourceKey CirclePlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CirclePlus));

        /// <summary>Lucide <c>circle-pound-sterling</c> icon.</summary>
        public static ComponentResourceKey CirclePoundSterling { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CirclePoundSterling));

        /// <summary>Lucide <c>circle-power</c> icon.</summary>
        public static ComponentResourceKey CirclePower { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CirclePower));

        /// <summary>Lucide <c>circle-question-mark</c> icon.</summary>
        public static ComponentResourceKey CircleQuestionMark { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleQuestionMark));

        /// <summary>Lucide <c>circle-slash-2</c> icon.</summary>
        public static ComponentResourceKey CircleSlash2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleSlash2));

        /// <summary>Lucide <c>circle-slash</c> icon.</summary>
        public static ComponentResourceKey CircleSlash { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleSlash));

        /// <summary>Lucide <c>circle-small</c> icon.</summary>
        public static ComponentResourceKey CircleSmall { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleSmall));

        /// <summary>Lucide <c>circle-star</c> icon.</summary>
        public static ComponentResourceKey CircleStar { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleStar));

        /// <summary>Lucide <c>circle-stop</c> icon.</summary>
        public static ComponentResourceKey CircleStop { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleStop));

        /// <summary>Lucide <c>circle-user-round</c> icon.</summary>
        public static ComponentResourceKey CircleUserRound { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleUserRound));

        /// <summary>Lucide <c>circle-user</c> icon.</summary>
        public static ComponentResourceKey CircleUser { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleUser));

        /// <summary>Lucide <c>circle-x</c> icon.</summary>
        public static ComponentResourceKey CircleX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircleX));

        /// <summary>Lucide <c>circle</c> icon.</summary>
        public static ComponentResourceKey Circle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Circle));

        /// <summary>Lucide <c>circuit-board</c> icon.</summary>
        public static ComponentResourceKey CircuitBoard { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CircuitBoard));

        /// <summary>Lucide <c>citrus</c> icon.</summary>
        public static ComponentResourceKey Citrus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Citrus));

        /// <summary>Lucide <c>clapperboard</c> icon.</summary>
        public static ComponentResourceKey Clapperboard { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clapperboard));

        /// <summary>Lucide <c>clipboard-check</c> icon.</summary>
        public static ComponentResourceKey ClipboardCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClipboardCheck));

        /// <summary>Lucide <c>clipboard-clock</c> icon.</summary>
        public static ComponentResourceKey ClipboardClock { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClipboardClock));

        /// <summary>Lucide <c>clipboard-copy</c> icon.</summary>
        public static ComponentResourceKey ClipboardCopy { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClipboardCopy));

        /// <summary>Lucide <c>clipboard-list</c> icon.</summary>
        public static ComponentResourceKey ClipboardList { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClipboardList));

        /// <summary>Lucide <c>clipboard-minus</c> icon.</summary>
        public static ComponentResourceKey ClipboardMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClipboardMinus));

        /// <summary>Lucide <c>clipboard-paste</c> icon.</summary>
        public static ComponentResourceKey ClipboardPaste { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClipboardPaste));

        /// <summary>Lucide <c>clipboard-pen-line</c> icon.</summary>
        public static ComponentResourceKey ClipboardPenLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClipboardPenLine));

        /// <summary>Lucide <c>clipboard-pen</c> icon.</summary>
        public static ComponentResourceKey ClipboardPen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClipboardPen));

        /// <summary>Lucide <c>clipboard-plus</c> icon.</summary>
        public static ComponentResourceKey ClipboardPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClipboardPlus));

        /// <summary>Lucide <c>clipboard-type</c> icon.</summary>
        public static ComponentResourceKey ClipboardType { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClipboardType));

        /// <summary>Lucide <c>clipboard-x</c> icon.</summary>
        public static ComponentResourceKey ClipboardX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClipboardX));

        /// <summary>Lucide <c>clipboard</c> icon.</summary>
        public static ComponentResourceKey Clipboard { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clipboard));

        /// <summary>Lucide <c>clock-1</c> icon.</summary>
        public static ComponentResourceKey Clock1 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clock1));

        /// <summary>Lucide <c>clock-10</c> icon.</summary>
        public static ComponentResourceKey Clock10 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clock10));

        /// <summary>Lucide <c>clock-11</c> icon.</summary>
        public static ComponentResourceKey Clock11 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clock11));

        /// <summary>Lucide <c>clock-12</c> icon.</summary>
        public static ComponentResourceKey Clock12 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clock12));

        /// <summary>Lucide <c>clock-2</c> icon.</summary>
        public static ComponentResourceKey Clock2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clock2));

        /// <summary>Lucide <c>clock-3</c> icon.</summary>
        public static ComponentResourceKey Clock3 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clock3));

        /// <summary>Lucide <c>clock-4</c> icon.</summary>
        public static ComponentResourceKey Clock4 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clock4));

        /// <summary>Lucide <c>clock-5</c> icon.</summary>
        public static ComponentResourceKey Clock5 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clock5));

        /// <summary>Lucide <c>clock-6</c> icon.</summary>
        public static ComponentResourceKey Clock6 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clock6));

        /// <summary>Lucide <c>clock-7</c> icon.</summary>
        public static ComponentResourceKey Clock7 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clock7));

        /// <summary>Lucide <c>clock-8</c> icon.</summary>
        public static ComponentResourceKey Clock8 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clock8));

        /// <summary>Lucide <c>clock-9</c> icon.</summary>
        public static ComponentResourceKey Clock9 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clock9));

        /// <summary>Lucide <c>clock-alert</c> icon.</summary>
        public static ComponentResourceKey ClockAlert { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClockAlert));

        /// <summary>Lucide <c>clock-arrow-down</c> icon.</summary>
        public static ComponentResourceKey ClockArrowDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClockArrowDown));

        /// <summary>Lucide <c>clock-arrow-left</c> icon.</summary>
        public static ComponentResourceKey ClockArrowLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClockArrowLeft));

        /// <summary>Lucide <c>clock-arrow-right</c> icon.</summary>
        public static ComponentResourceKey ClockArrowRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClockArrowRight));

        /// <summary>Lucide <c>clock-arrow-up</c> icon.</summary>
        public static ComponentResourceKey ClockArrowUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClockArrowUp));

        /// <summary>Lucide <c>clock-check</c> icon.</summary>
        public static ComponentResourceKey ClockCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClockCheck));

        /// <summary>Lucide <c>clock-fading</c> icon.</summary>
        public static ComponentResourceKey ClockFading { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClockFading));

        /// <summary>Lucide <c>clock-plus</c> icon.</summary>
        public static ComponentResourceKey ClockPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClockPlus));

        /// <summary>Lucide <c>clock</c> icon.</summary>
        public static ComponentResourceKey Clock { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clock));

        /// <summary>Lucide <c>closed-caption</c> icon.</summary>
        public static ComponentResourceKey ClosedCaption { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ClosedCaption));

        /// <summary>Lucide <c>cloud-alert</c> icon.</summary>
        public static ComponentResourceKey CloudAlert { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudAlert));

        /// <summary>Lucide <c>cloud-backup</c> icon.</summary>
        public static ComponentResourceKey CloudBackup { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudBackup));

        /// <summary>Lucide <c>cloud-check</c> icon.</summary>
        public static ComponentResourceKey CloudCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudCheck));

        /// <summary>Lucide <c>cloud-cog</c> icon.</summary>
        public static ComponentResourceKey CloudCog { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudCog));

        /// <summary>Lucide <c>cloud-download</c> icon.</summary>
        public static ComponentResourceKey CloudDownload { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudDownload));

        /// <summary>Lucide <c>cloud-drizzle</c> icon.</summary>
        public static ComponentResourceKey CloudDrizzle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudDrizzle));

        /// <summary>Lucide <c>cloud-fog</c> icon.</summary>
        public static ComponentResourceKey CloudFog { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudFog));

        /// <summary>Lucide <c>cloud-hail</c> icon.</summary>
        public static ComponentResourceKey CloudHail { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudHail));

        /// <summary>Lucide <c>cloud-lightning</c> icon.</summary>
        public static ComponentResourceKey CloudLightning { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudLightning));

        /// <summary>Lucide <c>cloud-moon-rain</c> icon.</summary>
        public static ComponentResourceKey CloudMoonRain { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudMoonRain));

        /// <summary>Lucide <c>cloud-moon</c> icon.</summary>
        public static ComponentResourceKey CloudMoon { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudMoon));

        /// <summary>Lucide <c>cloud-off</c> icon.</summary>
        public static ComponentResourceKey CloudOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudOff));

        /// <summary>Lucide <c>cloud-rain-wind</c> icon.</summary>
        public static ComponentResourceKey CloudRainWind { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudRainWind));

        /// <summary>Lucide <c>cloud-rain</c> icon.</summary>
        public static ComponentResourceKey CloudRain { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudRain));

        /// <summary>Lucide <c>cloud-snow</c> icon.</summary>
        public static ComponentResourceKey CloudSnow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudSnow));

        /// <summary>Lucide <c>cloud-sun-rain</c> icon.</summary>
        public static ComponentResourceKey CloudSunRain { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudSunRain));

        /// <summary>Lucide <c>cloud-sun</c> icon.</summary>
        public static ComponentResourceKey CloudSun { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudSun));

        /// <summary>Lucide <c>cloud-sync</c> icon.</summary>
        public static ComponentResourceKey CloudSync { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudSync));

        /// <summary>Lucide <c>cloud-upload</c> icon.</summary>
        public static ComponentResourceKey CloudUpload { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CloudUpload));

        /// <summary>Lucide <c>cloud</c> icon.</summary>
        public static ComponentResourceKey Cloud { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cloud));

        /// <summary>Lucide <c>cloudy</c> icon.</summary>
        public static ComponentResourceKey Cloudy { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cloudy));

        /// <summary>Lucide <c>clover</c> icon.</summary>
        public static ComponentResourceKey Clover { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Clover));

        /// <summary>Lucide <c>club</c> icon.</summary>
        public static ComponentResourceKey Club { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Club));

        /// <summary>Lucide <c>code-xml</c> icon.</summary>
        public static ComponentResourceKey CodeXml { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CodeXml));

        /// <summary>Lucide <c>code</c> icon.</summary>
        public static ComponentResourceKey Code { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Code));

        /// <summary>Lucide <c>coffee</c> icon.</summary>
        public static ComponentResourceKey Coffee { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Coffee));

        /// <summary>Lucide <c>cog</c> icon.</summary>
        public static ComponentResourceKey Cog { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cog));

        /// <summary>Lucide <c>coins</c> icon.</summary>
        public static ComponentResourceKey Coins { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Coins));

        /// <summary>Lucide <c>columns-2</c> icon.</summary>
        public static ComponentResourceKey Columns2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Columns2));

        /// <summary>Lucide <c>columns-3-cog</c> icon.</summary>
        public static ComponentResourceKey Columns3Cog { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Columns3Cog));

        /// <summary>Lucide <c>columns-3</c> icon.</summary>
        public static ComponentResourceKey Columns3 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Columns3));

        /// <summary>Lucide <c>columns-4</c> icon.</summary>
        public static ComponentResourceKey Columns4 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Columns4));

        /// <summary>Lucide <c>combine</c> icon.</summary>
        public static ComponentResourceKey Combine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Combine));

        /// <summary>Lucide <c>command</c> icon.</summary>
        public static ComponentResourceKey Command { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Command));

        /// <summary>Lucide <c>compass</c> icon.</summary>
        public static ComponentResourceKey Compass { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Compass));

        /// <summary>Lucide <c>component</c> icon.</summary>
        public static ComponentResourceKey Component { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Component));

        /// <summary>Lucide <c>computer</c> icon.</summary>
        public static ComponentResourceKey Computer { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Computer));

        /// <summary>Lucide <c>concierge-bell</c> icon.</summary>
        public static ComponentResourceKey ConciergeBell { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ConciergeBell));

        /// <summary>Lucide <c>cone</c> icon.</summary>
        public static ComponentResourceKey Cone { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cone));

        /// <summary>Lucide <c>construction</c> icon.</summary>
        public static ComponentResourceKey Construction { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Construction));

        /// <summary>Lucide <c>contact-round</c> icon.</summary>
        public static ComponentResourceKey ContactRound { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ContactRound));

        /// <summary>Lucide <c>contact</c> icon.</summary>
        public static ComponentResourceKey Contact { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Contact));

        /// <summary>Lucide <c>container</c> icon.</summary>
        public static ComponentResourceKey Container { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Container));

        /// <summary>Lucide <c>contrast</c> icon.</summary>
        public static ComponentResourceKey Contrast { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Contrast));

        /// <summary>Lucide <c>cookie</c> icon.</summary>
        public static ComponentResourceKey Cookie { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cookie));

        /// <summary>Lucide <c>cooking-pot</c> icon.</summary>
        public static ComponentResourceKey CookingPot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CookingPot));

        /// <summary>Lucide <c>copy-check</c> icon.</summary>
        public static ComponentResourceKey CopyCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CopyCheck));

        /// <summary>Lucide <c>copy-minus</c> icon.</summary>
        public static ComponentResourceKey CopyMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CopyMinus));

        /// <summary>Lucide <c>copy-plus</c> icon.</summary>
        public static ComponentResourceKey CopyPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CopyPlus));

        /// <summary>Lucide <c>copy-slash</c> icon.</summary>
        public static ComponentResourceKey CopySlash { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CopySlash));

        /// <summary>Lucide <c>copy-x</c> icon.</summary>
        public static ComponentResourceKey CopyX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CopyX));

        /// <summary>Lucide <c>copy</c> icon.</summary>
        public static ComponentResourceKey Copy { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Copy));

        /// <summary>Lucide <c>copyleft</c> icon.</summary>
        public static ComponentResourceKey Copyleft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Copyleft));

        /// <summary>Lucide <c>copyright</c> icon.</summary>
        public static ComponentResourceKey Copyright { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Copyright));

        /// <summary>Lucide <c>corner-down-left</c> icon.</summary>
        public static ComponentResourceKey CornerDownLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CornerDownLeft));

        /// <summary>Lucide <c>corner-down-right</c> icon.</summary>
        public static ComponentResourceKey CornerDownRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CornerDownRight));

        /// <summary>Lucide <c>corner-left-down</c> icon.</summary>
        public static ComponentResourceKey CornerLeftDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CornerLeftDown));

        /// <summary>Lucide <c>corner-left-up</c> icon.</summary>
        public static ComponentResourceKey CornerLeftUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CornerLeftUp));

        /// <summary>Lucide <c>corner-right-down</c> icon.</summary>
        public static ComponentResourceKey CornerRightDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CornerRightDown));

        /// <summary>Lucide <c>corner-right-up</c> icon.</summary>
        public static ComponentResourceKey CornerRightUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CornerRightUp));

        /// <summary>Lucide <c>corner-up-left</c> icon.</summary>
        public static ComponentResourceKey CornerUpLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CornerUpLeft));

        /// <summary>Lucide <c>corner-up-right</c> icon.</summary>
        public static ComponentResourceKey CornerUpRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CornerUpRight));

        /// <summary>Lucide <c>cpu</c> icon.</summary>
        public static ComponentResourceKey Cpu { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cpu));

        /// <summary>Lucide <c>creative-commons</c> icon.</summary>
        public static ComponentResourceKey CreativeCommons { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CreativeCommons));

        /// <summary>Lucide <c>credit-card</c> icon.</summary>
        public static ComponentResourceKey CreditCard { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CreditCard));

        /// <summary>Lucide <c>croissant</c> icon.</summary>
        public static ComponentResourceKey Croissant { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Croissant));

        /// <summary>Lucide <c>crop</c> icon.</summary>
        public static ComponentResourceKey Crop { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Crop));

        /// <summary>Lucide <c>cross</c> icon.</summary>
        public static ComponentResourceKey Cross { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cross));

        /// <summary>Lucide <c>crosshair</c> icon.</summary>
        public static ComponentResourceKey Crosshair { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Crosshair));

        /// <summary>Lucide <c>crown</c> icon.</summary>
        public static ComponentResourceKey Crown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Crown));

        /// <summary>Lucide <c>cuboid</c> icon.</summary>
        public static ComponentResourceKey Cuboid { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cuboid));

        /// <summary>Lucide <c>cup-soda</c> icon.</summary>
        public static ComponentResourceKey CupSoda { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(CupSoda));

        /// <summary>Lucide <c>currency</c> icon.</summary>
        public static ComponentResourceKey Currency { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Currency));

        /// <summary>Lucide <c>cylinder</c> icon.</summary>
        public static ComponentResourceKey Cylinder { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Cylinder));

        // ─── D ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>dam</c> icon.</summary>
        public static ComponentResourceKey Dam { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Dam));

        /// <summary>Lucide <c>database-arrow-down</c> icon.</summary>
        public static ComponentResourceKey DatabaseArrowDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DatabaseArrowDown));

        /// <summary>Lucide <c>database-arrow-up</c> icon.</summary>
        public static ComponentResourceKey DatabaseArrowUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DatabaseArrowUp));

        /// <summary>Lucide <c>database-backup</c> icon.</summary>
        public static ComponentResourceKey DatabaseBackup { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DatabaseBackup));

        /// <summary>Lucide <c>database-check</c> icon.</summary>
        public static ComponentResourceKey DatabaseCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DatabaseCheck));

        /// <summary>Lucide <c>database-minus</c> icon.</summary>
        public static ComponentResourceKey DatabaseMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DatabaseMinus));

        /// <summary>Lucide <c>database-plus</c> icon.</summary>
        public static ComponentResourceKey DatabasePlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DatabasePlus));

        /// <summary>Lucide <c>database-search</c> icon.</summary>
        public static ComponentResourceKey DatabaseSearch { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DatabaseSearch));

        /// <summary>Lucide <c>database-x</c> icon.</summary>
        public static ComponentResourceKey DatabaseX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DatabaseX));

        /// <summary>Lucide <c>database-zap</c> icon.</summary>
        public static ComponentResourceKey DatabaseZap { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DatabaseZap));

        /// <summary>Lucide <c>database</c> icon.</summary>
        public static ComponentResourceKey Database { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Database));

        /// <summary>Lucide <c>decimals-arrow-left</c> icon.</summary>
        public static ComponentResourceKey DecimalsArrowLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DecimalsArrowLeft));

        /// <summary>Lucide <c>decimals-arrow-right</c> icon.</summary>
        public static ComponentResourceKey DecimalsArrowRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DecimalsArrowRight));

        /// <summary>Lucide <c>delete</c> icon.</summary>
        public static ComponentResourceKey Delete { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Delete));

        /// <summary>Lucide <c>dessert</c> icon.</summary>
        public static ComponentResourceKey Dessert { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Dessert));

        /// <summary>Lucide <c>diameter</c> icon.</summary>
        public static ComponentResourceKey Diameter { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Diameter));

        /// <summary>Lucide <c>diamond-minus</c> icon.</summary>
        public static ComponentResourceKey DiamondMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DiamondMinus));

        /// <summary>Lucide <c>diamond-percent</c> icon.</summary>
        public static ComponentResourceKey DiamondPercent { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DiamondPercent));

        /// <summary>Lucide <c>diamond-plus</c> icon.</summary>
        public static ComponentResourceKey DiamondPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DiamondPlus));

        /// <summary>Lucide <c>diamond</c> icon.</summary>
        public static ComponentResourceKey Diamond { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Diamond));

        /// <summary>Lucide <c>dice-1</c> icon.</summary>
        public static ComponentResourceKey Dice1 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Dice1));

        /// <summary>Lucide <c>dice-2</c> icon.</summary>
        public static ComponentResourceKey Dice2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Dice2));

        /// <summary>Lucide <c>dice-3</c> icon.</summary>
        public static ComponentResourceKey Dice3 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Dice3));

        /// <summary>Lucide <c>dice-4</c> icon.</summary>
        public static ComponentResourceKey Dice4 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Dice4));

        /// <summary>Lucide <c>dice-5</c> icon.</summary>
        public static ComponentResourceKey Dice5 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Dice5));

        /// <summary>Lucide <c>dice-6</c> icon.</summary>
        public static ComponentResourceKey Dice6 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Dice6));

        /// <summary>Lucide <c>dices</c> icon.</summary>
        public static ComponentResourceKey Dices { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Dices));

        /// <summary>Lucide <c>diff</c> icon.</summary>
        public static ComponentResourceKey Diff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Diff));

        /// <summary>Lucide <c>disc-2</c> icon.</summary>
        public static ComponentResourceKey Disc2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Disc2));

        /// <summary>Lucide <c>disc-3</c> icon.</summary>
        public static ComponentResourceKey Disc3 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Disc3));

        /// <summary>Lucide <c>disc-album</c> icon.</summary>
        public static ComponentResourceKey DiscAlbum { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DiscAlbum));

        /// <summary>Lucide <c>disc</c> icon.</summary>
        public static ComponentResourceKey Disc { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Disc));

        /// <summary>Lucide <c>divide</c> icon.</summary>
        public static ComponentResourceKey Divide { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Divide));

        /// <summary>Lucide <c>dna-off</c> icon.</summary>
        public static ComponentResourceKey DnaOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DnaOff));

        /// <summary>Lucide <c>dna</c> icon.</summary>
        public static ComponentResourceKey Dna { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Dna));

        /// <summary>Lucide <c>dock</c> icon.</summary>
        public static ComponentResourceKey Dock { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Dock));

        /// <summary>Lucide <c>dog</c> icon.</summary>
        public static ComponentResourceKey Dog { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Dog));

        /// <summary>Lucide <c>dollar-sign</c> icon.</summary>
        public static ComponentResourceKey DollarSign { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DollarSign));

        /// <summary>Lucide <c>donut</c> icon.</summary>
        public static ComponentResourceKey Donut { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Donut));

        /// <summary>Lucide <c>door-closed-locked</c> icon.</summary>
        public static ComponentResourceKey DoorClosedLocked { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DoorClosedLocked));

        /// <summary>Lucide <c>door-closed</c> icon.</summary>
        public static ComponentResourceKey DoorClosed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DoorClosed));

        /// <summary>Lucide <c>door-open</c> icon.</summary>
        public static ComponentResourceKey DoorOpen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DoorOpen));

        /// <summary>Lucide <c>dot</c> icon.</summary>
        public static ComponentResourceKey Dot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Dot));

        /// <summary>Lucide <c>download</c> icon.</summary>
        public static ComponentResourceKey Download { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Download));

        /// <summary>Lucide <c>drafting-compass</c> icon.</summary>
        public static ComponentResourceKey DraftingCompass { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DraftingCompass));

        /// <summary>Lucide <c>drama</c> icon.</summary>
        public static ComponentResourceKey Drama { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Drama));

        /// <summary>Lucide <c>drill</c> icon.</summary>
        public static ComponentResourceKey Drill { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Drill));

        /// <summary>Lucide <c>drone</c> icon.</summary>
        public static ComponentResourceKey Drone { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Drone));

        /// <summary>Lucide <c>droplet-off</c> icon.</summary>
        public static ComponentResourceKey DropletOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(DropletOff));

        /// <summary>Lucide <c>droplet</c> icon.</summary>
        public static ComponentResourceKey Droplet { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Droplet));

        /// <summary>Lucide <c>droplets</c> icon.</summary>
        public static ComponentResourceKey Droplets { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Droplets));

        /// <summary>Lucide <c>drum</c> icon.</summary>
        public static ComponentResourceKey Drum { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Drum));

        /// <summary>Lucide <c>drumstick</c> icon.</summary>
        public static ComponentResourceKey Drumstick { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Drumstick));

        /// <summary>Lucide <c>dumbbell</c> icon.</summary>
        public static ComponentResourceKey Dumbbell { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Dumbbell));

        // ─── E ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>ear-off</c> icon.</summary>
        public static ComponentResourceKey EarOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(EarOff));

        /// <summary>Lucide <c>ear</c> icon.</summary>
        public static ComponentResourceKey Ear { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ear));

        /// <summary>Lucide <c>earth-lock</c> icon.</summary>
        public static ComponentResourceKey EarthLock { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(EarthLock));

        /// <summary>Lucide <c>earth</c> icon.</summary>
        public static ComponentResourceKey Earth { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Earth));

        /// <summary>Lucide <c>eclipse</c> icon.</summary>
        public static ComponentResourceKey Eclipse { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Eclipse));

        /// <summary>Lucide <c>egg-fried</c> icon.</summary>
        public static ComponentResourceKey EggFried { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(EggFried));

        /// <summary>Lucide <c>egg-off</c> icon.</summary>
        public static ComponentResourceKey EggOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(EggOff));

        /// <summary>Lucide <c>egg</c> icon.</summary>
        public static ComponentResourceKey Egg { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Egg));

        /// <summary>Lucide <c>ellipse</c> icon.</summary>
        public static ComponentResourceKey Ellipse { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ellipse));

        /// <summary>Lucide <c>ellipsis-vertical</c> icon.</summary>
        public static ComponentResourceKey EllipsisVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(EllipsisVertical));

        /// <summary>Lucide <c>ellipsis</c> icon.</summary>
        public static ComponentResourceKey Ellipsis { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ellipsis));

        /// <summary>Lucide <c>equal-approximately</c> icon.</summary>
        public static ComponentResourceKey EqualApproximately { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(EqualApproximately));

        /// <summary>Lucide <c>equal-not</c> icon.</summary>
        public static ComponentResourceKey EqualNot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(EqualNot));

        /// <summary>Lucide <c>equal</c> icon.</summary>
        public static ComponentResourceKey Equal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Equal));

        /// <summary>Lucide <c>eraser</c> icon.</summary>
        public static ComponentResourceKey Eraser { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Eraser));

        /// <summary>Lucide <c>ethernet-port</c> icon.</summary>
        public static ComponentResourceKey EthernetPort { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(EthernetPort));

        /// <summary>Lucide <c>euro</c> icon.</summary>
        public static ComponentResourceKey Euro { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Euro));

        /// <summary>Lucide <c>ev-charger</c> icon.</summary>
        public static ComponentResourceKey EvCharger { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(EvCharger));

        /// <summary>Lucide <c>expand</c> icon.</summary>
        public static ComponentResourceKey Expand { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Expand));

        /// <summary>Lucide <c>external-link</c> icon.</summary>
        public static ComponentResourceKey ExternalLink { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ExternalLink));

        /// <summary>Lucide <c>eye-closed</c> icon.</summary>
        public static ComponentResourceKey EyeClosed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(EyeClosed));

        /// <summary>Lucide <c>eye-dashed</c> icon.</summary>
        public static ComponentResourceKey EyeDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(EyeDashed));

        /// <summary>Lucide <c>eye-off</c> icon.</summary>
        public static ComponentResourceKey EyeOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(EyeOff));

        /// <summary>Lucide <c>eye</c> icon.</summary>
        public static ComponentResourceKey Eye { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Eye));

        // ─── F ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>factory</c> icon.</summary>
        public static ComponentResourceKey Factory { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Factory));

        /// <summary>Lucide <c>fan</c> icon.</summary>
        public static ComponentResourceKey Fan { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Fan));

        /// <summary>Lucide <c>fast-forward</c> icon.</summary>
        public static ComponentResourceKey FastForward { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FastForward));

        /// <summary>Lucide <c>feather</c> icon.</summary>
        public static ComponentResourceKey Feather { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Feather));

        /// <summary>Lucide <c>fence</c> icon.</summary>
        public static ComponentResourceKey Fence { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Fence));

        /// <summary>Lucide <c>ferris-wheel</c> icon.</summary>
        public static ComponentResourceKey FerrisWheel { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FerrisWheel));

        /// <summary>Lucide <c>file-archive</c> icon.</summary>
        public static ComponentResourceKey FileArchive { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileArchive));

        /// <summary>Lucide <c>file-axis-3d</c> icon.</summary>
        public static ComponentResourceKey FileAxis3d { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileAxis3d));

        /// <summary>Lucide <c>file-badge</c> icon.</summary>
        public static ComponentResourceKey FileBadge { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileBadge));

        /// <summary>Lucide <c>file-box</c> icon.</summary>
        public static ComponentResourceKey FileBox { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileBox));

        /// <summary>Lucide <c>file-braces-corner</c> icon.</summary>
        public static ComponentResourceKey FileBracesCorner { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileBracesCorner));

        /// <summary>Lucide <c>file-braces</c> icon.</summary>
        public static ComponentResourceKey FileBraces { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileBraces));

        /// <summary>Lucide <c>file-chart-column-increasing</c> icon.</summary>
        public static ComponentResourceKey FileChartColumnIncreasing { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileChartColumnIncreasing));

        /// <summary>Lucide <c>file-chart-column</c> icon.</summary>
        public static ComponentResourceKey FileChartColumn { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileChartColumn));

        /// <summary>Lucide <c>file-chart-line</c> icon.</summary>
        public static ComponentResourceKey FileChartLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileChartLine));

        /// <summary>Lucide <c>file-chart-pie</c> icon.</summary>
        public static ComponentResourceKey FileChartPie { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileChartPie));

        /// <summary>Lucide <c>file-check-corner</c> icon.</summary>
        public static ComponentResourceKey FileCheckCorner { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileCheckCorner));

        /// <summary>Lucide <c>file-check</c> icon.</summary>
        public static ComponentResourceKey FileCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileCheck));

        /// <summary>Lucide <c>file-clock</c> icon.</summary>
        public static ComponentResourceKey FileClock { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileClock));

        /// <summary>Lucide <c>file-code-corner</c> icon.</summary>
        public static ComponentResourceKey FileCodeCorner { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileCodeCorner));

        /// <summary>Lucide <c>file-code</c> icon.</summary>
        public static ComponentResourceKey FileCode { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileCode));

        /// <summary>Lucide <c>file-cog</c> icon.</summary>
        public static ComponentResourceKey FileCog { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileCog));

        /// <summary>Lucide <c>file-diff</c> icon.</summary>
        public static ComponentResourceKey FileDiff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileDiff));

        /// <summary>Lucide <c>file-digit</c> icon.</summary>
        public static ComponentResourceKey FileDigit { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileDigit));

        /// <summary>Lucide <c>file-down</c> icon.</summary>
        public static ComponentResourceKey FileDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileDown));

        /// <summary>Lucide <c>file-exclamation-point</c> icon.</summary>
        public static ComponentResourceKey FileExclamationPoint { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileExclamationPoint));

        /// <summary>Lucide <c>file-headphone</c> icon.</summary>
        public static ComponentResourceKey FileHeadphone { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileHeadphone));

        /// <summary>Lucide <c>file-heart</c> icon.</summary>
        public static ComponentResourceKey FileHeart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileHeart));

        /// <summary>Lucide <c>file-image</c> icon.</summary>
        public static ComponentResourceKey FileImage { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileImage));

        /// <summary>Lucide <c>file-input</c> icon.</summary>
        public static ComponentResourceKey FileInput { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileInput));

        /// <summary>Lucide <c>file-key</c> icon.</summary>
        public static ComponentResourceKey FileKey { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileKey));

        /// <summary>Lucide <c>file-lock</c> icon.</summary>
        public static ComponentResourceKey FileLock { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileLock));

        /// <summary>Lucide <c>file-minus-corner</c> icon.</summary>
        public static ComponentResourceKey FileMinusCorner { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileMinusCorner));

        /// <summary>Lucide <c>file-minus</c> icon.</summary>
        public static ComponentResourceKey FileMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileMinus));

        /// <summary>Lucide <c>file-music</c> icon.</summary>
        public static ComponentResourceKey FileMusic { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileMusic));

        /// <summary>Lucide <c>file-output</c> icon.</summary>
        public static ComponentResourceKey FileOutput { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileOutput));

        /// <summary>Lucide <c>file-pen-line</c> icon.</summary>
        public static ComponentResourceKey FilePenLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FilePenLine));

        /// <summary>Lucide <c>file-pen</c> icon.</summary>
        public static ComponentResourceKey FilePen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FilePen));

        /// <summary>Lucide <c>file-play</c> icon.</summary>
        public static ComponentResourceKey FilePlay { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FilePlay));

        /// <summary>Lucide <c>file-plus-corner</c> icon.</summary>
        public static ComponentResourceKey FilePlusCorner { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FilePlusCorner));

        /// <summary>Lucide <c>file-plus</c> icon.</summary>
        public static ComponentResourceKey FilePlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FilePlus));

        /// <summary>Lucide <c>file-question-mark</c> icon.</summary>
        public static ComponentResourceKey FileQuestionMark { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileQuestionMark));

        /// <summary>Lucide <c>file-scan</c> icon.</summary>
        public static ComponentResourceKey FileScan { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileScan));

        /// <summary>Lucide <c>file-search-corner</c> icon.</summary>
        public static ComponentResourceKey FileSearchCorner { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileSearchCorner));

        /// <summary>Lucide <c>file-search</c> icon.</summary>
        public static ComponentResourceKey FileSearch { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileSearch));

        /// <summary>Lucide <c>file-signal</c> icon.</summary>
        public static ComponentResourceKey FileSignal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileSignal));

        /// <summary>Lucide <c>file-sliders</c> icon.</summary>
        public static ComponentResourceKey FileSliders { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileSliders));

        /// <summary>Lucide <c>file-spreadsheet</c> icon.</summary>
        public static ComponentResourceKey FileSpreadsheet { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileSpreadsheet));

        /// <summary>Lucide <c>file-stack</c> icon.</summary>
        public static ComponentResourceKey FileStack { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileStack));

        /// <summary>Lucide <c>file-symlink</c> icon.</summary>
        public static ComponentResourceKey FileSymlink { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileSymlink));

        /// <summary>Lucide <c>file-terminal</c> icon.</summary>
        public static ComponentResourceKey FileTerminal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileTerminal));

        /// <summary>Lucide <c>file-text</c> icon.</summary>
        public static ComponentResourceKey FileText { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileText));

        /// <summary>Lucide <c>file-type-corner</c> icon.</summary>
        public static ComponentResourceKey FileTypeCorner { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileTypeCorner));

        /// <summary>Lucide <c>file-type</c> icon.</summary>
        public static ComponentResourceKey FileType { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileType));

        /// <summary>Lucide <c>file-up</c> icon.</summary>
        public static ComponentResourceKey FileUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileUp));

        /// <summary>Lucide <c>file-user</c> icon.</summary>
        public static ComponentResourceKey FileUser { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileUser));

        /// <summary>Lucide <c>file-video-camera</c> icon.</summary>
        public static ComponentResourceKey FileVideoCamera { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileVideoCamera));

        /// <summary>Lucide <c>file-volume</c> icon.</summary>
        public static ComponentResourceKey FileVolume { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileVolume));

        /// <summary>Lucide <c>file-x-corner</c> icon.</summary>
        public static ComponentResourceKey FileXCorner { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileXCorner));

        /// <summary>Lucide <c>file-x</c> icon.</summary>
        public static ComponentResourceKey FileX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FileX));

        /// <summary>Lucide <c>file</c> icon.</summary>
        public static ComponentResourceKey File { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(File));

        /// <summary>Lucide <c>files</c> icon.</summary>
        public static ComponentResourceKey Files { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Files));

        /// <summary>Lucide <c>film</c> icon.</summary>
        public static ComponentResourceKey Film { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Film));

        /// <summary>Lucide <c>fingerprint-pattern</c> icon.</summary>
        public static ComponentResourceKey FingerprintPattern { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FingerprintPattern));

        /// <summary>Lucide <c>fire-extinguisher</c> icon.</summary>
        public static ComponentResourceKey FireExtinguisher { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FireExtinguisher));

        /// <summary>Lucide <c>fish-off</c> icon.</summary>
        public static ComponentResourceKey FishOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FishOff));

        /// <summary>Lucide <c>fish-symbol</c> icon.</summary>
        public static ComponentResourceKey FishSymbol { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FishSymbol));

        /// <summary>Lucide <c>fish</c> icon.</summary>
        public static ComponentResourceKey Fish { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Fish));

        /// <summary>Lucide <c>fishing-hook</c> icon.</summary>
        public static ComponentResourceKey FishingHook { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FishingHook));

        /// <summary>Lucide <c>fishing-rod</c> icon.</summary>
        public static ComponentResourceKey FishingRod { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FishingRod));

        /// <summary>Lucide <c>flag-off</c> icon.</summary>
        public static ComponentResourceKey FlagOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FlagOff));

        /// <summary>Lucide <c>flag-triangle-left</c> icon.</summary>
        public static ComponentResourceKey FlagTriangleLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FlagTriangleLeft));

        /// <summary>Lucide <c>flag-triangle-right</c> icon.</summary>
        public static ComponentResourceKey FlagTriangleRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FlagTriangleRight));

        /// <summary>Lucide <c>flag</c> icon.</summary>
        public static ComponentResourceKey Flag { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Flag));

        /// <summary>Lucide <c>flame-kindling</c> icon.</summary>
        public static ComponentResourceKey FlameKindling { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FlameKindling));

        /// <summary>Lucide <c>flame</c> icon.</summary>
        public static ComponentResourceKey Flame { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Flame));

        /// <summary>Lucide <c>flashlight-off</c> icon.</summary>
        public static ComponentResourceKey FlashlightOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FlashlightOff));

        /// <summary>Lucide <c>flashlight</c> icon.</summary>
        public static ComponentResourceKey Flashlight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Flashlight));

        /// <summary>Lucide <c>flask-conical-off</c> icon.</summary>
        public static ComponentResourceKey FlaskConicalOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FlaskConicalOff));

        /// <summary>Lucide <c>flask-conical</c> icon.</summary>
        public static ComponentResourceKey FlaskConical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FlaskConical));

        /// <summary>Lucide <c>flask-round</c> icon.</summary>
        public static ComponentResourceKey FlaskRound { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FlaskRound));

        /// <summary>Lucide <c>flip-horizontal-2</c> icon.</summary>
        public static ComponentResourceKey FlipHorizontal2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FlipHorizontal2));

        /// <summary>Lucide <c>flip-vertical-2</c> icon.</summary>
        public static ComponentResourceKey FlipVertical2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FlipVertical2));

        /// <summary>Lucide <c>flower-2</c> icon.</summary>
        public static ComponentResourceKey Flower2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Flower2));

        /// <summary>Lucide <c>flower</c> icon.</summary>
        public static ComponentResourceKey Flower { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Flower));

        /// <summary>Lucide <c>focus</c> icon.</summary>
        public static ComponentResourceKey Focus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Focus));

        /// <summary>Lucide <c>fold-horizontal</c> icon.</summary>
        public static ComponentResourceKey FoldHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FoldHorizontal));

        /// <summary>Lucide <c>fold-vertical</c> icon.</summary>
        public static ComponentResourceKey FoldVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FoldVertical));

        /// <summary>Lucide <c>folder-archive</c> icon.</summary>
        public static ComponentResourceKey FolderArchive { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderArchive));

        /// <summary>Lucide <c>folder-bookmark</c> icon.</summary>
        public static ComponentResourceKey FolderBookmark { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderBookmark));

        /// <summary>Lucide <c>folder-check</c> icon.</summary>
        public static ComponentResourceKey FolderCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderCheck));

        /// <summary>Lucide <c>folder-clock</c> icon.</summary>
        public static ComponentResourceKey FolderClock { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderClock));

        /// <summary>Lucide <c>folder-closed</c> icon.</summary>
        public static ComponentResourceKey FolderClosed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderClosed));

        /// <summary>Lucide <c>folder-code</c> icon.</summary>
        public static ComponentResourceKey FolderCode { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderCode));

        /// <summary>Lucide <c>folder-cog</c> icon.</summary>
        public static ComponentResourceKey FolderCog { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderCog));

        /// <summary>Lucide <c>folder-dot</c> icon.</summary>
        public static ComponentResourceKey FolderDot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderDot));

        /// <summary>Lucide <c>folder-down</c> icon.</summary>
        public static ComponentResourceKey FolderDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderDown));

        /// <summary>Lucide <c>folder-git-2</c> icon.</summary>
        public static ComponentResourceKey FolderGit2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderGit2));

        /// <summary>Lucide <c>folder-git</c> icon.</summary>
        public static ComponentResourceKey FolderGit { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderGit));

        /// <summary>Lucide <c>folder-heart</c> icon.</summary>
        public static ComponentResourceKey FolderHeart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderHeart));

        /// <summary>Lucide <c>folder-input</c> icon.</summary>
        public static ComponentResourceKey FolderInput { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderInput));

        /// <summary>Lucide <c>folder-kanban</c> icon.</summary>
        public static ComponentResourceKey FolderKanban { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderKanban));

        /// <summary>Lucide <c>folder-key</c> icon.</summary>
        public static ComponentResourceKey FolderKey { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderKey));

        /// <summary>Lucide <c>folder-lock</c> icon.</summary>
        public static ComponentResourceKey FolderLock { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderLock));

        /// <summary>Lucide <c>folder-minus</c> icon.</summary>
        public static ComponentResourceKey FolderMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderMinus));

        /// <summary>Lucide <c>folder-open-dot</c> icon.</summary>
        public static ComponentResourceKey FolderOpenDot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderOpenDot));

        /// <summary>Lucide <c>folder-open</c> icon.</summary>
        public static ComponentResourceKey FolderOpen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderOpen));

        /// <summary>Lucide <c>folder-output</c> icon.</summary>
        public static ComponentResourceKey FolderOutput { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderOutput));

        /// <summary>Lucide <c>folder-pen</c> icon.</summary>
        public static ComponentResourceKey FolderPen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderPen));

        /// <summary>Lucide <c>folder-plus</c> icon.</summary>
        public static ComponentResourceKey FolderPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderPlus));

        /// <summary>Lucide <c>folder-root</c> icon.</summary>
        public static ComponentResourceKey FolderRoot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderRoot));

        /// <summary>Lucide <c>folder-search-2</c> icon.</summary>
        public static ComponentResourceKey FolderSearch2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderSearch2));

        /// <summary>Lucide <c>folder-search</c> icon.</summary>
        public static ComponentResourceKey FolderSearch { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderSearch));

        /// <summary>Lucide <c>folder-symlink</c> icon.</summary>
        public static ComponentResourceKey FolderSymlink { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderSymlink));

        /// <summary>Lucide <c>folder-sync</c> icon.</summary>
        public static ComponentResourceKey FolderSync { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderSync));

        /// <summary>Lucide <c>folder-tree</c> icon.</summary>
        public static ComponentResourceKey FolderTree { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderTree));

        /// <summary>Lucide <c>folder-up</c> icon.</summary>
        public static ComponentResourceKey FolderUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderUp));

        /// <summary>Lucide <c>folder-x</c> icon.</summary>
        public static ComponentResourceKey FolderX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FolderX));

        /// <summary>Lucide <c>folder</c> icon.</summary>
        public static ComponentResourceKey Folder { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Folder));

        /// <summary>Lucide <c>folders</c> icon.</summary>
        public static ComponentResourceKey Folders { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Folders));

        /// <summary>Lucide <c>footprints</c> icon.</summary>
        public static ComponentResourceKey Footprints { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Footprints));

        /// <summary>Lucide <c>forklift</c> icon.</summary>
        public static ComponentResourceKey Forklift { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Forklift));

        /// <summary>Lucide <c>form</c> icon.</summary>
        public static ComponentResourceKey Form { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Form));

        /// <summary>Lucide <c>forward</c> icon.</summary>
        public static ComponentResourceKey Forward { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Forward));

        /// <summary>Lucide <c>frame</c> icon.</summary>
        public static ComponentResourceKey Frame { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Frame));

        /// <summary>Lucide <c>frown</c> icon.</summary>
        public static ComponentResourceKey Frown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Frown));

        /// <summary>Lucide <c>fuel</c> icon.</summary>
        public static ComponentResourceKey Fuel { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Fuel));

        /// <summary>Lucide <c>fullscreen</c> icon.</summary>
        public static ComponentResourceKey Fullscreen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Fullscreen));

        /// <summary>Lucide <c>funnel-plus</c> icon.</summary>
        public static ComponentResourceKey FunnelPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FunnelPlus));

        /// <summary>Lucide <c>funnel-x</c> icon.</summary>
        public static ComponentResourceKey FunnelX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(FunnelX));

        /// <summary>Lucide <c>funnel</c> icon.</summary>
        public static ComponentResourceKey Funnel { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Funnel));

        // ─── G ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>gallery-horizontal-end</c> icon.</summary>
        public static ComponentResourceKey GalleryHorizontalEnd { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GalleryHorizontalEnd));

        /// <summary>Lucide <c>gallery-horizontal</c> icon.</summary>
        public static ComponentResourceKey GalleryHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GalleryHorizontal));

        /// <summary>Lucide <c>gallery-thumbnails</c> icon.</summary>
        public static ComponentResourceKey GalleryThumbnails { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GalleryThumbnails));

        /// <summary>Lucide <c>gallery-vertical-end</c> icon.</summary>
        public static ComponentResourceKey GalleryVerticalEnd { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GalleryVerticalEnd));

        /// <summary>Lucide <c>gallery-vertical</c> icon.</summary>
        public static ComponentResourceKey GalleryVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GalleryVertical));

        /// <summary>Lucide <c>gamepad-2</c> icon.</summary>
        public static ComponentResourceKey Gamepad2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Gamepad2));

        /// <summary>Lucide <c>gamepad-directional</c> icon.</summary>
        public static ComponentResourceKey GamepadDirectional { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GamepadDirectional));

        /// <summary>Lucide <c>gamepad</c> icon.</summary>
        public static ComponentResourceKey Gamepad { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Gamepad));

        /// <summary>Lucide <c>gauge</c> icon.</summary>
        public static ComponentResourceKey Gauge { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Gauge));

        /// <summary>Lucide <c>gavel</c> icon.</summary>
        public static ComponentResourceKey Gavel { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Gavel));

        /// <summary>Lucide <c>gem</c> icon.</summary>
        public static ComponentResourceKey Gem { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Gem));

        /// <summary>Lucide <c>georgian-lari</c> icon.</summary>
        public static ComponentResourceKey GeorgianLari { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GeorgianLari));

        /// <summary>Lucide <c>ghost</c> icon.</summary>
        public static ComponentResourceKey Ghost { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ghost));

        /// <summary>Lucide <c>gift</c> icon.</summary>
        public static ComponentResourceKey Gift { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Gift));

        /// <summary>Lucide <c>git-branch-minus</c> icon.</summary>
        public static ComponentResourceKey GitBranchMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitBranchMinus));

        /// <summary>Lucide <c>git-branch-plus</c> icon.</summary>
        public static ComponentResourceKey GitBranchPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitBranchPlus));

        /// <summary>Lucide <c>git-branch</c> icon.</summary>
        public static ComponentResourceKey GitBranch { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitBranch));

        /// <summary>Lucide <c>git-commit-horizontal</c> icon.</summary>
        public static ComponentResourceKey GitCommitHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitCommitHorizontal));

        /// <summary>Lucide <c>git-commit-vertical</c> icon.</summary>
        public static ComponentResourceKey GitCommitVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitCommitVertical));

        /// <summary>Lucide <c>git-compare-arrows</c> icon.</summary>
        public static ComponentResourceKey GitCompareArrows { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitCompareArrows));

        /// <summary>Lucide <c>git-compare</c> icon.</summary>
        public static ComponentResourceKey GitCompare { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitCompare));

        /// <summary>Lucide <c>git-fork</c> icon.</summary>
        public static ComponentResourceKey GitFork { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitFork));

        /// <summary>Lucide <c>git-graph</c> icon.</summary>
        public static ComponentResourceKey GitGraph { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitGraph));

        /// <summary>Lucide <c>git-merge-conflict</c> icon.</summary>
        public static ComponentResourceKey GitMergeConflict { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitMergeConflict));

        /// <summary>Lucide <c>git-merge</c> icon.</summary>
        public static ComponentResourceKey GitMerge { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitMerge));

        /// <summary>Lucide <c>git-pull-request-arrow</c> icon.</summary>
        public static ComponentResourceKey GitPullRequestArrow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitPullRequestArrow));

        /// <summary>Lucide <c>git-pull-request-closed</c> icon.</summary>
        public static ComponentResourceKey GitPullRequestClosed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitPullRequestClosed));

        /// <summary>Lucide <c>git-pull-request-create-arrow</c> icon.</summary>
        public static ComponentResourceKey GitPullRequestCreateArrow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitPullRequestCreateArrow));

        /// <summary>Lucide <c>git-pull-request-create</c> icon.</summary>
        public static ComponentResourceKey GitPullRequestCreate { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitPullRequestCreate));

        /// <summary>Lucide <c>git-pull-request-draft</c> icon.</summary>
        public static ComponentResourceKey GitPullRequestDraft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitPullRequestDraft));

        /// <summary>Lucide <c>git-pull-request</c> icon.</summary>
        public static ComponentResourceKey GitPullRequest { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GitPullRequest));

        /// <summary>Lucide <c>glass-water</c> icon.</summary>
        public static ComponentResourceKey GlassWater { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GlassWater));

        /// <summary>Lucide <c>glasses</c> icon.</summary>
        public static ComponentResourceKey Glasses { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Glasses));

        /// <summary>Lucide <c>globe-check</c> icon.</summary>
        public static ComponentResourceKey GlobeCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GlobeCheck));

        /// <summary>Lucide <c>globe-lock</c> icon.</summary>
        public static ComponentResourceKey GlobeLock { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GlobeLock));

        /// <summary>Lucide <c>globe-off</c> icon.</summary>
        public static ComponentResourceKey GlobeOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GlobeOff));

        /// <summary>Lucide <c>globe-x</c> icon.</summary>
        public static ComponentResourceKey GlobeX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GlobeX));

        /// <summary>Lucide <c>globe</c> icon.</summary>
        public static ComponentResourceKey Globe { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Globe));

        /// <summary>Lucide <c>goal</c> icon.</summary>
        public static ComponentResourceKey Goal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Goal));

        /// <summary>Lucide <c>gpu</c> icon.</summary>
        public static ComponentResourceKey Gpu { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Gpu));

        /// <summary>Lucide <c>graduation-cap</c> icon.</summary>
        public static ComponentResourceKey GraduationCap { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GraduationCap));

        /// <summary>Lucide <c>grape</c> icon.</summary>
        public static ComponentResourceKey Grape { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Grape));

        /// <summary>Lucide <c>grid-2x2-check</c> icon.</summary>
        public static ComponentResourceKey Grid2x2Check { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Grid2x2Check));

        /// <summary>Lucide <c>grid-2x2-plus</c> icon.</summary>
        public static ComponentResourceKey Grid2x2Plus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Grid2x2Plus));

        /// <summary>Lucide <c>grid-2x2-x</c> icon.</summary>
        public static ComponentResourceKey Grid2x2X { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Grid2x2X));

        /// <summary>Lucide <c>grid-2x2</c> icon.</summary>
        public static ComponentResourceKey Grid2x2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Grid2x2));

        /// <summary>Lucide <c>grid-3x2</c> icon.</summary>
        public static ComponentResourceKey Grid3x2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Grid3x2));

        /// <summary>Lucide <c>grid-3x3</c> icon.</summary>
        public static ComponentResourceKey Grid3x3 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Grid3x3));

        /// <summary>Lucide <c>grip-horizontal</c> icon.</summary>
        public static ComponentResourceKey GripHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GripHorizontal));

        /// <summary>Lucide <c>grip-vertical</c> icon.</summary>
        public static ComponentResourceKey GripVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(GripVertical));

        /// <summary>Lucide <c>grip</c> icon.</summary>
        public static ComponentResourceKey Grip { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Grip));

        /// <summary>Lucide <c>group</c> icon.</summary>
        public static ComponentResourceKey Group { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Group));

        /// <summary>Lucide <c>guitar</c> icon.</summary>
        public static ComponentResourceKey Guitar { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Guitar));

        // ─── H ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>ham</c> icon.</summary>
        public static ComponentResourceKey Ham { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ham));

        /// <summary>Lucide <c>hamburger</c> icon.</summary>
        public static ComponentResourceKey Hamburger { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Hamburger));

        /// <summary>Lucide <c>hammer</c> icon.</summary>
        public static ComponentResourceKey Hammer { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Hammer));

        /// <summary>Lucide <c>hand-coins</c> icon.</summary>
        public static ComponentResourceKey HandCoins { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HandCoins));

        /// <summary>Lucide <c>hand-fist</c> icon.</summary>
        public static ComponentResourceKey HandFist { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HandFist));

        /// <summary>Lucide <c>hand-grab</c> icon.</summary>
        public static ComponentResourceKey HandGrab { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HandGrab));

        /// <summary>Lucide <c>hand-heart</c> icon.</summary>
        public static ComponentResourceKey HandHeart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HandHeart));

        /// <summary>Lucide <c>hand-helping</c> icon.</summary>
        public static ComponentResourceKey HandHelping { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HandHelping));

        /// <summary>Lucide <c>hand-metal</c> icon.</summary>
        public static ComponentResourceKey HandMetal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HandMetal));

        /// <summary>Lucide <c>hand-platter</c> icon.</summary>
        public static ComponentResourceKey HandPlatter { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HandPlatter));

        /// <summary>Lucide <c>hand</c> icon.</summary>
        public static ComponentResourceKey Hand { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Hand));

        /// <summary>Lucide <c>handbag</c> icon.</summary>
        public static ComponentResourceKey Handbag { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Handbag));

        /// <summary>Lucide <c>handshake</c> icon.</summary>
        public static ComponentResourceKey Handshake { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Handshake));

        /// <summary>Lucide <c>hard-drive-download</c> icon.</summary>
        public static ComponentResourceKey HardDriveDownload { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HardDriveDownload));

        /// <summary>Lucide <c>hard-drive-upload</c> icon.</summary>
        public static ComponentResourceKey HardDriveUpload { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HardDriveUpload));

        /// <summary>Lucide <c>hard-drive</c> icon.</summary>
        public static ComponentResourceKey HardDrive { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HardDrive));

        /// <summary>Lucide <c>hard-hat</c> icon.</summary>
        public static ComponentResourceKey HardHat { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HardHat));

        /// <summary>Lucide <c>hash</c> icon.</summary>
        public static ComponentResourceKey Hash { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Hash));

        /// <summary>Lucide <c>hat-glasses</c> icon.</summary>
        public static ComponentResourceKey HatGlasses { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HatGlasses));

        /// <summary>Lucide <c>haze</c> icon.</summary>
        public static ComponentResourceKey Haze { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Haze));

        /// <summary>Lucide <c>hd</c> icon.</summary>
        public static ComponentResourceKey Hd { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Hd));

        /// <summary>Lucide <c>hdmi-port</c> icon.</summary>
        public static ComponentResourceKey HdmiPort { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HdmiPort));

        /// <summary>Lucide <c>heading-1</c> icon.</summary>
        public static ComponentResourceKey Heading1 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Heading1));

        /// <summary>Lucide <c>heading-2</c> icon.</summary>
        public static ComponentResourceKey Heading2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Heading2));

        /// <summary>Lucide <c>heading-3</c> icon.</summary>
        public static ComponentResourceKey Heading3 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Heading3));

        /// <summary>Lucide <c>heading-4</c> icon.</summary>
        public static ComponentResourceKey Heading4 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Heading4));

        /// <summary>Lucide <c>heading-5</c> icon.</summary>
        public static ComponentResourceKey Heading5 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Heading5));

        /// <summary>Lucide <c>heading-6</c> icon.</summary>
        public static ComponentResourceKey Heading6 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Heading6));

        /// <summary>Lucide <c>heading</c> icon.</summary>
        public static ComponentResourceKey Heading { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Heading));

        /// <summary>Lucide <c>headphone-off</c> icon.</summary>
        public static ComponentResourceKey HeadphoneOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HeadphoneOff));

        /// <summary>Lucide <c>headphones</c> icon.</summary>
        public static ComponentResourceKey Headphones { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Headphones));

        /// <summary>Lucide <c>headset</c> icon.</summary>
        public static ComponentResourceKey Headset { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Headset));

        /// <summary>Lucide <c>heart-crack</c> icon.</summary>
        public static ComponentResourceKey HeartCrack { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HeartCrack));

        /// <summary>Lucide <c>heart-handshake</c> icon.</summary>
        public static ComponentResourceKey HeartHandshake { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HeartHandshake));

        /// <summary>Lucide <c>heart-minus</c> icon.</summary>
        public static ComponentResourceKey HeartMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HeartMinus));

        /// <summary>Lucide <c>heart-off</c> icon.</summary>
        public static ComponentResourceKey HeartOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HeartOff));

        /// <summary>Lucide <c>heart-plus</c> icon.</summary>
        public static ComponentResourceKey HeartPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HeartPlus));

        /// <summary>Lucide <c>heart-pulse</c> icon.</summary>
        public static ComponentResourceKey HeartPulse { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HeartPulse));

        /// <summary>Lucide <c>heart-x</c> icon.</summary>
        public static ComponentResourceKey HeartX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HeartX));

        /// <summary>Lucide <c>heart</c> icon.</summary>
        public static ComponentResourceKey Heart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Heart));

        /// <summary>Lucide <c>heater</c> icon.</summary>
        public static ComponentResourceKey Heater { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Heater));

        /// <summary>Lucide <c>helicopter</c> icon.</summary>
        public static ComponentResourceKey Helicopter { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Helicopter));

        /// <summary>Lucide <c>hexagon</c> icon.</summary>
        public static ComponentResourceKey Hexagon { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Hexagon));

        /// <summary>Lucide <c>highlighter</c> icon.</summary>
        public static ComponentResourceKey Highlighter { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Highlighter));

        /// <summary>Lucide <c>history</c> icon.</summary>
        public static ComponentResourceKey History { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(History));

        /// <summary>Lucide <c>hop-off</c> icon.</summary>
        public static ComponentResourceKey HopOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HopOff));

        /// <summary>Lucide <c>hop</c> icon.</summary>
        public static ComponentResourceKey Hop { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Hop));

        /// <summary>Lucide <c>hospital</c> icon.</summary>
        public static ComponentResourceKey Hospital { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Hospital));

        /// <summary>Lucide <c>hotel</c> icon.</summary>
        public static ComponentResourceKey Hotel { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Hotel));

        /// <summary>Lucide <c>hourglass</c> icon.</summary>
        public static ComponentResourceKey Hourglass { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Hourglass));

        /// <summary>Lucide <c>house-heart</c> icon.</summary>
        public static ComponentResourceKey HouseHeart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HouseHeart));

        /// <summary>Lucide <c>house-plug</c> icon.</summary>
        public static ComponentResourceKey HousePlug { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HousePlug));

        /// <summary>Lucide <c>house-plus</c> icon.</summary>
        public static ComponentResourceKey HousePlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HousePlus));

        /// <summary>Lucide <c>house-wifi</c> icon.</summary>
        public static ComponentResourceKey HouseWifi { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(HouseWifi));

        /// <summary>Lucide <c>house</c> icon.</summary>
        public static ComponentResourceKey House { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(House));

        // ─── I ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>ice-cream-bowl</c> icon.</summary>
        public static ComponentResourceKey IceCreamBowl { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(IceCreamBowl));

        /// <summary>Lucide <c>ice-cream-cone</c> icon.</summary>
        public static ComponentResourceKey IceCreamCone { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(IceCreamCone));

        /// <summary>Lucide <c>id-card-lanyard</c> icon.</summary>
        public static ComponentResourceKey IdCardLanyard { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(IdCardLanyard));

        /// <summary>Lucide <c>id-card</c> icon.</summary>
        public static ComponentResourceKey IdCard { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(IdCard));

        /// <summary>Lucide <c>image-down</c> icon.</summary>
        public static ComponentResourceKey ImageDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ImageDown));

        /// <summary>Lucide <c>image-minus</c> icon.</summary>
        public static ComponentResourceKey ImageMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ImageMinus));

        /// <summary>Lucide <c>image-off</c> icon.</summary>
        public static ComponentResourceKey ImageOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ImageOff));

        /// <summary>Lucide <c>image-play</c> icon.</summary>
        public static ComponentResourceKey ImagePlay { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ImagePlay));

        /// <summary>Lucide <c>image-plus</c> icon.</summary>
        public static ComponentResourceKey ImagePlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ImagePlus));

        /// <summary>Lucide <c>image-up</c> icon.</summary>
        public static ComponentResourceKey ImageUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ImageUp));

        /// <summary>Lucide <c>image-upscale</c> icon.</summary>
        public static ComponentResourceKey ImageUpscale { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ImageUpscale));

        /// <summary>Lucide <c>image</c> icon.</summary>
        public static ComponentResourceKey Image { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Image));

        /// <summary>Lucide <c>images</c> icon.</summary>
        public static ComponentResourceKey Images { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Images));

        /// <summary>Lucide <c>import</c> icon.</summary>
        public static ComponentResourceKey Import { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Import));

        /// <summary>Lucide <c>inbox</c> icon.</summary>
        public static ComponentResourceKey Inbox { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Inbox));

        /// <summary>Lucide <c>indian-rupee</c> icon.</summary>
        public static ComponentResourceKey IndianRupee { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(IndianRupee));

        /// <summary>Lucide <c>infinity</c> icon.</summary>
        public static ComponentResourceKey Infinity { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Infinity));

        /// <summary>Lucide <c>info</c> icon.</summary>
        public static ComponentResourceKey Info { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Info));

        /// <summary>Lucide <c>inspection-panel</c> icon.</summary>
        public static ComponentResourceKey InspectionPanel { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(InspectionPanel));

        /// <summary>Lucide <c>italic</c> icon.</summary>
        public static ComponentResourceKey Italic { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Italic));

        /// <summary>Lucide <c>iteration-ccw</c> icon.</summary>
        public static ComponentResourceKey IterationCcw { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(IterationCcw));

        /// <summary>Lucide <c>iteration-cw</c> icon.</summary>
        public static ComponentResourceKey IterationCw { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(IterationCw));

        // ─── J ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>japanese-yen</c> icon.</summary>
        public static ComponentResourceKey JapaneseYen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(JapaneseYen));

        /// <summary>Lucide <c>joystick</c> icon.</summary>
        public static ComponentResourceKey Joystick { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Joystick));

        // ─── K ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>kanban</c> icon.</summary>
        public static ComponentResourceKey Kanban { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Kanban));

        /// <summary>Lucide <c>kayak</c> icon.</summary>
        public static ComponentResourceKey Kayak { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Kayak));

        /// <summary>Lucide <c>key-round</c> icon.</summary>
        public static ComponentResourceKey KeyRound { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(KeyRound));

        /// <summary>Lucide <c>key-square</c> icon.</summary>
        public static ComponentResourceKey KeySquare { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(KeySquare));

        /// <summary>Lucide <c>key</c> icon.</summary>
        public static ComponentResourceKey Key { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Key));

        /// <summary>Lucide <c>keyboard-music</c> icon.</summary>
        public static ComponentResourceKey KeyboardMusic { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(KeyboardMusic));

        /// <summary>Lucide <c>keyboard-off</c> icon.</summary>
        public static ComponentResourceKey KeyboardOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(KeyboardOff));

        /// <summary>Lucide <c>keyboard</c> icon.</summary>
        public static ComponentResourceKey Keyboard { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Keyboard));

        // ─── L ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>lamp-ceiling</c> icon.</summary>
        public static ComponentResourceKey LampCeiling { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LampCeiling));

        /// <summary>Lucide <c>lamp-desk</c> icon.</summary>
        public static ComponentResourceKey LampDesk { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LampDesk));

        /// <summary>Lucide <c>lamp-floor</c> icon.</summary>
        public static ComponentResourceKey LampFloor { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LampFloor));

        /// <summary>Lucide <c>lamp-wall-down</c> icon.</summary>
        public static ComponentResourceKey LampWallDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LampWallDown));

        /// <summary>Lucide <c>lamp-wall-up</c> icon.</summary>
        public static ComponentResourceKey LampWallUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LampWallUp));

        /// <summary>Lucide <c>lamp</c> icon.</summary>
        public static ComponentResourceKey Lamp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Lamp));

        /// <summary>Lucide <c>land-plot</c> icon.</summary>
        public static ComponentResourceKey LandPlot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LandPlot));

        /// <summary>Lucide <c>landmark</c> icon.</summary>
        public static ComponentResourceKey Landmark { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Landmark));

        /// <summary>Lucide <c>languages</c> icon.</summary>
        public static ComponentResourceKey Languages { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Languages));

        /// <summary>Lucide <c>laptop-minimal-check</c> icon.</summary>
        public static ComponentResourceKey LaptopMinimalCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LaptopMinimalCheck));

        /// <summary>Lucide <c>laptop-minimal</c> icon.</summary>
        public static ComponentResourceKey LaptopMinimal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LaptopMinimal));

        /// <summary>Lucide <c>laptop</c> icon.</summary>
        public static ComponentResourceKey Laptop { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Laptop));

        /// <summary>Lucide <c>lasso-select</c> icon.</summary>
        public static ComponentResourceKey LassoSelect { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LassoSelect));

        /// <summary>Lucide <c>lasso</c> icon.</summary>
        public static ComponentResourceKey Lasso { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Lasso));

        /// <summary>Lucide <c>laugh</c> icon.</summary>
        public static ComponentResourceKey Laugh { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Laugh));

        /// <summary>Lucide <c>layers-2</c> icon.</summary>
        public static ComponentResourceKey Layers2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Layers2));

        /// <summary>Lucide <c>layers-minus</c> icon.</summary>
        public static ComponentResourceKey LayersMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LayersMinus));

        /// <summary>Lucide <c>layers-plus</c> icon.</summary>
        public static ComponentResourceKey LayersPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LayersPlus));

        /// <summary>Lucide <c>layers</c> icon.</summary>
        public static ComponentResourceKey Layers { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Layers));

        /// <summary>Lucide <c>layout-dashboard</c> icon.</summary>
        public static ComponentResourceKey LayoutDashboard { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LayoutDashboard));

        /// <summary>Lucide <c>layout-grid</c> icon.</summary>
        public static ComponentResourceKey LayoutGrid { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LayoutGrid));

        /// <summary>Lucide <c>layout-list</c> icon.</summary>
        public static ComponentResourceKey LayoutList { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LayoutList));

        /// <summary>Lucide <c>layout-panel-left</c> icon.</summary>
        public static ComponentResourceKey LayoutPanelLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LayoutPanelLeft));

        /// <summary>Lucide <c>layout-panel-top</c> icon.</summary>
        public static ComponentResourceKey LayoutPanelTop { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LayoutPanelTop));

        /// <summary>Lucide <c>layout-template</c> icon.</summary>
        public static ComponentResourceKey LayoutTemplate { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LayoutTemplate));

        /// <summary>Lucide <c>leaf</c> icon.</summary>
        public static ComponentResourceKey Leaf { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Leaf));

        /// <summary>Lucide <c>leafy-green</c> icon.</summary>
        public static ComponentResourceKey LeafyGreen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LeafyGreen));

        /// <summary>Lucide <c>lectern</c> icon.</summary>
        public static ComponentResourceKey Lectern { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Lectern));

        /// <summary>Lucide <c>lens-concave</c> icon.</summary>
        public static ComponentResourceKey LensConcave { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LensConcave));

        /// <summary>Lucide <c>lens-convex</c> icon.</summary>
        public static ComponentResourceKey LensConvex { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LensConvex));

        /// <summary>Lucide <c>library-big</c> icon.</summary>
        public static ComponentResourceKey LibraryBig { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LibraryBig));

        /// <summary>Lucide <c>library</c> icon.</summary>
        public static ComponentResourceKey Library { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Library));

        /// <summary>Lucide <c>life-buoy</c> icon.</summary>
        public static ComponentResourceKey LifeBuoy { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LifeBuoy));

        /// <summary>Lucide <c>ligature</c> icon.</summary>
        public static ComponentResourceKey Ligature { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ligature));

        /// <summary>Lucide <c>lightbulb-off</c> icon.</summary>
        public static ComponentResourceKey LightbulbOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LightbulbOff));

        /// <summary>Lucide <c>lightbulb</c> icon.</summary>
        public static ComponentResourceKey Lightbulb { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Lightbulb));

        /// <summary>Lucide <c>line-dot-right-horizontal</c> icon.</summary>
        public static ComponentResourceKey LineDotRightHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LineDotRightHorizontal));

        /// <summary>Lucide <c>line-squiggle</c> icon.</summary>
        public static ComponentResourceKey LineSquiggle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LineSquiggle));

        /// <summary>Lucide <c>line-style</c> icon.</summary>
        public static ComponentResourceKey LineStyle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LineStyle));

        /// <summary>Lucide <c>link-2-off</c> icon.</summary>
        public static ComponentResourceKey Link2Off { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Link2Off));

        /// <summary>Lucide <c>link-2</c> icon.</summary>
        public static ComponentResourceKey Link2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Link2));

        /// <summary>Lucide <c>link</c> icon.</summary>
        public static ComponentResourceKey Link { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Link));

        /// <summary>Lucide <c>list-check</c> icon.</summary>
        public static ComponentResourceKey ListCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListCheck));

        /// <summary>Lucide <c>list-checks</c> icon.</summary>
        public static ComponentResourceKey ListChecks { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListChecks));

        /// <summary>Lucide <c>list-chevrons-down-up</c> icon.</summary>
        public static ComponentResourceKey ListChevronsDownUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListChevronsDownUp));

        /// <summary>Lucide <c>list-chevrons-up-down</c> icon.</summary>
        public static ComponentResourceKey ListChevronsUpDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListChevronsUpDown));

        /// <summary>Lucide <c>list-collapse</c> icon.</summary>
        public static ComponentResourceKey ListCollapse { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListCollapse));

        /// <summary>Lucide <c>list-end</c> icon.</summary>
        public static ComponentResourceKey ListEnd { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListEnd));

        /// <summary>Lucide <c>list-filter-plus</c> icon.</summary>
        public static ComponentResourceKey ListFilterPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListFilterPlus));

        /// <summary>Lucide <c>list-filter</c> icon.</summary>
        public static ComponentResourceKey ListFilter { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListFilter));

        /// <summary>Lucide <c>list-indent-decrease</c> icon.</summary>
        public static ComponentResourceKey ListIndentDecrease { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListIndentDecrease));

        /// <summary>Lucide <c>list-indent-increase</c> icon.</summary>
        public static ComponentResourceKey ListIndentIncrease { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListIndentIncrease));

        /// <summary>Lucide <c>list-minus</c> icon.</summary>
        public static ComponentResourceKey ListMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListMinus));

        /// <summary>Lucide <c>list-music</c> icon.</summary>
        public static ComponentResourceKey ListMusic { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListMusic));

        /// <summary>Lucide <c>list-ordered</c> icon.</summary>
        public static ComponentResourceKey ListOrdered { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListOrdered));

        /// <summary>Lucide <c>list-plus</c> icon.</summary>
        public static ComponentResourceKey ListPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListPlus));

        /// <summary>Lucide <c>list-restart</c> icon.</summary>
        public static ComponentResourceKey ListRestart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListRestart));

        /// <summary>Lucide <c>list-sort-ascending</c> icon.</summary>
        public static ComponentResourceKey ListSortAscending { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListSortAscending));

        /// <summary>Lucide <c>list-sort-descending</c> icon.</summary>
        public static ComponentResourceKey ListSortDescending { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListSortDescending));

        /// <summary>Lucide <c>list-start</c> icon.</summary>
        public static ComponentResourceKey ListStart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListStart));

        /// <summary>Lucide <c>list-todo</c> icon.</summary>
        public static ComponentResourceKey ListTodo { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListTodo));

        /// <summary>Lucide <c>list-tree</c> icon.</summary>
        public static ComponentResourceKey ListTree { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListTree));

        /// <summary>Lucide <c>list-video</c> icon.</summary>
        public static ComponentResourceKey ListVideo { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListVideo));

        /// <summary>Lucide <c>list-x</c> icon.</summary>
        public static ComponentResourceKey ListX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ListX));

        /// <summary>Lucide <c>list</c> icon.</summary>
        public static ComponentResourceKey List { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(List));

        /// <summary>Lucide <c>loader-circle</c> icon.</summary>
        public static ComponentResourceKey LoaderCircle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LoaderCircle));

        /// <summary>Lucide <c>loader-pinwheel</c> icon.</summary>
        public static ComponentResourceKey LoaderPinwheel { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LoaderPinwheel));

        /// <summary>Lucide <c>loader</c> icon.</summary>
        public static ComponentResourceKey Loader { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Loader));

        /// <summary>Lucide <c>locate-fixed</c> icon.</summary>
        public static ComponentResourceKey LocateFixed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LocateFixed));

        /// <summary>Lucide <c>locate-off</c> icon.</summary>
        public static ComponentResourceKey LocateOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LocateOff));

        /// <summary>Lucide <c>locate</c> icon.</summary>
        public static ComponentResourceKey Locate { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Locate));

        /// <summary>Lucide <c>lock-keyhole-open</c> icon.</summary>
        public static ComponentResourceKey LockKeyholeOpen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LockKeyholeOpen));

        /// <summary>Lucide <c>lock-keyhole</c> icon.</summary>
        public static ComponentResourceKey LockKeyhole { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LockKeyhole));

        /// <summary>Lucide <c>lock-open</c> icon.</summary>
        public static ComponentResourceKey LockOpen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LockOpen));

        /// <summary>Lucide <c>lock</c> icon.</summary>
        public static ComponentResourceKey Lock { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Lock));

        /// <summary>Lucide <c>log-in</c> icon.</summary>
        public static ComponentResourceKey LogIn { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LogIn));

        /// <summary>Lucide <c>log-out</c> icon.</summary>
        public static ComponentResourceKey LogOut { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(LogOut));

        /// <summary>Lucide <c>logs</c> icon.</summary>
        public static ComponentResourceKey Logs { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Logs));

        /// <summary>Lucide <c>lollipop</c> icon.</summary>
        public static ComponentResourceKey Lollipop { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Lollipop));

        /// <summary>Lucide <c>luggage</c> icon.</summary>
        public static ComponentResourceKey Luggage { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Luggage));

        // ─── M ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>magnet</c> icon.</summary>
        public static ComponentResourceKey Magnet { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Magnet));

        /// <summary>Lucide <c>mail-check</c> icon.</summary>
        public static ComponentResourceKey MailCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MailCheck));

        /// <summary>Lucide <c>mail-minus</c> icon.</summary>
        public static ComponentResourceKey MailMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MailMinus));

        /// <summary>Lucide <c>mail-open</c> icon.</summary>
        public static ComponentResourceKey MailOpen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MailOpen));

        /// <summary>Lucide <c>mail-plus</c> icon.</summary>
        public static ComponentResourceKey MailPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MailPlus));

        /// <summary>Lucide <c>mail-question-mark</c> icon.</summary>
        public static ComponentResourceKey MailQuestionMark { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MailQuestionMark));

        /// <summary>Lucide <c>mail-search</c> icon.</summary>
        public static ComponentResourceKey MailSearch { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MailSearch));

        /// <summary>Lucide <c>mail-warning</c> icon.</summary>
        public static ComponentResourceKey MailWarning { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MailWarning));

        /// <summary>Lucide <c>mail-x</c> icon.</summary>
        public static ComponentResourceKey MailX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MailX));

        /// <summary>Lucide <c>mail</c> icon.</summary>
        public static ComponentResourceKey Mail { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Mail));

        /// <summary>Lucide <c>mailbox</c> icon.</summary>
        public static ComponentResourceKey Mailbox { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Mailbox));

        /// <summary>Lucide <c>mails</c> icon.</summary>
        public static ComponentResourceKey Mails { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Mails));

        /// <summary>Lucide <c>map-minus</c> icon.</summary>
        public static ComponentResourceKey MapMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapMinus));

        /// <summary>Lucide <c>map-pin-check-inside</c> icon.</summary>
        public static ComponentResourceKey MapPinCheckInside { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapPinCheckInside));

        /// <summary>Lucide <c>map-pin-check</c> icon.</summary>
        public static ComponentResourceKey MapPinCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapPinCheck));

        /// <summary>Lucide <c>map-pin-house</c> icon.</summary>
        public static ComponentResourceKey MapPinHouse { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapPinHouse));

        /// <summary>Lucide <c>map-pin-minus-inside</c> icon.</summary>
        public static ComponentResourceKey MapPinMinusInside { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapPinMinusInside));

        /// <summary>Lucide <c>map-pin-minus</c> icon.</summary>
        public static ComponentResourceKey MapPinMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapPinMinus));

        /// <summary>Lucide <c>map-pin-off</c> icon.</summary>
        public static ComponentResourceKey MapPinOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapPinOff));

        /// <summary>Lucide <c>map-pin-pen</c> icon.</summary>
        public static ComponentResourceKey MapPinPen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapPinPen));

        /// <summary>Lucide <c>map-pin-plus-inside</c> icon.</summary>
        public static ComponentResourceKey MapPinPlusInside { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapPinPlusInside));

        /// <summary>Lucide <c>map-pin-plus</c> icon.</summary>
        public static ComponentResourceKey MapPinPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapPinPlus));

        /// <summary>Lucide <c>map-pin-search</c> icon.</summary>
        public static ComponentResourceKey MapPinSearch { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapPinSearch));

        /// <summary>Lucide <c>map-pin-x-inside</c> icon.</summary>
        public static ComponentResourceKey MapPinXInside { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapPinXInside));

        /// <summary>Lucide <c>map-pin-x</c> icon.</summary>
        public static ComponentResourceKey MapPinX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapPinX));

        /// <summary>Lucide <c>map-pin</c> icon.</summary>
        public static ComponentResourceKey MapPin { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapPin));

        /// <summary>Lucide <c>map-pinned</c> icon.</summary>
        public static ComponentResourceKey MapPinned { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapPinned));

        /// <summary>Lucide <c>map-plus</c> icon.</summary>
        public static ComponentResourceKey MapPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MapPlus));

        /// <summary>Lucide <c>map</c> icon.</summary>
        public static ComponentResourceKey Map { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Map));

        /// <summary>Lucide <c>mars-stroke</c> icon.</summary>
        public static ComponentResourceKey MarsStroke { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MarsStroke));

        /// <summary>Lucide <c>mars</c> icon.</summary>
        public static ComponentResourceKey Mars { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Mars));

        /// <summary>Lucide <c>martini</c> icon.</summary>
        public static ComponentResourceKey Martini { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Martini));

        /// <summary>Lucide <c>maximize-2</c> icon.</summary>
        public static ComponentResourceKey Maximize2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Maximize2));

        /// <summary>Lucide <c>maximize</c> icon.</summary>
        public static ComponentResourceKey Maximize { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Maximize));

        /// <summary>Lucide <c>medal</c> icon.</summary>
        public static ComponentResourceKey Medal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Medal));

        /// <summary>Lucide <c>megaphone-off</c> icon.</summary>
        public static ComponentResourceKey MegaphoneOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MegaphoneOff));

        /// <summary>Lucide <c>megaphone</c> icon.</summary>
        public static ComponentResourceKey Megaphone { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Megaphone));

        /// <summary>Lucide <c>meh</c> icon.</summary>
        public static ComponentResourceKey Meh { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Meh));

        /// <summary>Lucide <c>memory-stick</c> icon.</summary>
        public static ComponentResourceKey MemoryStick { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MemoryStick));

        /// <summary>Lucide <c>menu</c> icon.</summary>
        public static ComponentResourceKey Menu { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Menu));

        /// <summary>Lucide <c>merge</c> icon.</summary>
        public static ComponentResourceKey Merge { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Merge));

        /// <summary>Lucide <c>message-circle-check</c> icon.</summary>
        public static ComponentResourceKey MessageCircleCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageCircleCheck));

        /// <summary>Lucide <c>message-circle-code</c> icon.</summary>
        public static ComponentResourceKey MessageCircleCode { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageCircleCode));

        /// <summary>Lucide <c>message-circle-dashed</c> icon.</summary>
        public static ComponentResourceKey MessageCircleDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageCircleDashed));

        /// <summary>Lucide <c>message-circle-heart</c> icon.</summary>
        public static ComponentResourceKey MessageCircleHeart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageCircleHeart));

        /// <summary>Lucide <c>message-circle-more</c> icon.</summary>
        public static ComponentResourceKey MessageCircleMore { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageCircleMore));

        /// <summary>Lucide <c>message-circle-off</c> icon.</summary>
        public static ComponentResourceKey MessageCircleOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageCircleOff));

        /// <summary>Lucide <c>message-circle-plus</c> icon.</summary>
        public static ComponentResourceKey MessageCirclePlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageCirclePlus));

        /// <summary>Lucide <c>message-circle-question-mark</c> icon.</summary>
        public static ComponentResourceKey MessageCircleQuestionMark { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageCircleQuestionMark));

        /// <summary>Lucide <c>message-circle-reply</c> icon.</summary>
        public static ComponentResourceKey MessageCircleReply { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageCircleReply));

        /// <summary>Lucide <c>message-circle-warning</c> icon.</summary>
        public static ComponentResourceKey MessageCircleWarning { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageCircleWarning));

        /// <summary>Lucide <c>message-circle-x</c> icon.</summary>
        public static ComponentResourceKey MessageCircleX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageCircleX));

        /// <summary>Lucide <c>message-circle</c> icon.</summary>
        public static ComponentResourceKey MessageCircle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageCircle));

        /// <summary>Lucide <c>message-square-check</c> icon.</summary>
        public static ComponentResourceKey MessageSquareCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquareCheck));

        /// <summary>Lucide <c>message-square-code</c> icon.</summary>
        public static ComponentResourceKey MessageSquareCode { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquareCode));

        /// <summary>Lucide <c>message-square-dashed</c> icon.</summary>
        public static ComponentResourceKey MessageSquareDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquareDashed));

        /// <summary>Lucide <c>message-square-diff</c> icon.</summary>
        public static ComponentResourceKey MessageSquareDiff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquareDiff));

        /// <summary>Lucide <c>message-square-dot</c> icon.</summary>
        public static ComponentResourceKey MessageSquareDot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquareDot));

        /// <summary>Lucide <c>message-square-heart</c> icon.</summary>
        public static ComponentResourceKey MessageSquareHeart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquareHeart));

        /// <summary>Lucide <c>message-square-lock</c> icon.</summary>
        public static ComponentResourceKey MessageSquareLock { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquareLock));

        /// <summary>Lucide <c>message-square-more</c> icon.</summary>
        public static ComponentResourceKey MessageSquareMore { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquareMore));

        /// <summary>Lucide <c>message-square-off</c> icon.</summary>
        public static ComponentResourceKey MessageSquareOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquareOff));

        /// <summary>Lucide <c>message-square-plus</c> icon.</summary>
        public static ComponentResourceKey MessageSquarePlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquarePlus));

        /// <summary>Lucide <c>message-square-quote</c> icon.</summary>
        public static ComponentResourceKey MessageSquareQuote { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquareQuote));

        /// <summary>Lucide <c>message-square-reply</c> icon.</summary>
        public static ComponentResourceKey MessageSquareReply { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquareReply));

        /// <summary>Lucide <c>message-square-share</c> icon.</summary>
        public static ComponentResourceKey MessageSquareShare { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquareShare));

        /// <summary>Lucide <c>message-square-text</c> icon.</summary>
        public static ComponentResourceKey MessageSquareText { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquareText));

        /// <summary>Lucide <c>message-square-warning</c> icon.</summary>
        public static ComponentResourceKey MessageSquareWarning { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquareWarning));

        /// <summary>Lucide <c>message-square-x</c> icon.</summary>
        public static ComponentResourceKey MessageSquareX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquareX));

        /// <summary>Lucide <c>message-square</c> icon.</summary>
        public static ComponentResourceKey MessageSquare { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessageSquare));

        /// <summary>Lucide <c>messages-square</c> icon.</summary>
        public static ComponentResourceKey MessagesSquare { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MessagesSquare));

        /// <summary>Lucide <c>metronome</c> icon.</summary>
        public static ComponentResourceKey Metronome { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Metronome));

        /// <summary>Lucide <c>mic-off</c> icon.</summary>
        public static ComponentResourceKey MicOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MicOff));

        /// <summary>Lucide <c>mic-vocal</c> icon.</summary>
        public static ComponentResourceKey MicVocal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MicVocal));

        /// <summary>Lucide <c>mic</c> icon.</summary>
        public static ComponentResourceKey Mic { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Mic));

        /// <summary>Lucide <c>microchip</c> icon.</summary>
        public static ComponentResourceKey Microchip { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Microchip));

        /// <summary>Lucide <c>microscope</c> icon.</summary>
        public static ComponentResourceKey Microscope { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Microscope));

        /// <summary>Lucide <c>microwave</c> icon.</summary>
        public static ComponentResourceKey Microwave { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Microwave));

        /// <summary>Lucide <c>milestone</c> icon.</summary>
        public static ComponentResourceKey Milestone { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Milestone));

        /// <summary>Lucide <c>milk-off</c> icon.</summary>
        public static ComponentResourceKey MilkOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MilkOff));

        /// <summary>Lucide <c>milk</c> icon.</summary>
        public static ComponentResourceKey Milk { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Milk));

        /// <summary>Lucide <c>minimize-2</c> icon.</summary>
        public static ComponentResourceKey Minimize2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Minimize2));

        /// <summary>Lucide <c>minimize</c> icon.</summary>
        public static ComponentResourceKey Minimize { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Minimize));

        /// <summary>Lucide <c>minus</c> icon.</summary>
        public static ComponentResourceKey Minus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Minus));

        /// <summary>Lucide <c>mirror-rectangular</c> icon.</summary>
        public static ComponentResourceKey MirrorRectangular { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MirrorRectangular));

        /// <summary>Lucide <c>mirror-round</c> icon.</summary>
        public static ComponentResourceKey MirrorRound { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MirrorRound));

        /// <summary>Lucide <c>monitor-check</c> icon.</summary>
        public static ComponentResourceKey MonitorCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MonitorCheck));

        /// <summary>Lucide <c>monitor-cloud</c> icon.</summary>
        public static ComponentResourceKey MonitorCloud { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MonitorCloud));

        /// <summary>Lucide <c>monitor-cog</c> icon.</summary>
        public static ComponentResourceKey MonitorCog { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MonitorCog));

        /// <summary>Lucide <c>monitor-dot</c> icon.</summary>
        public static ComponentResourceKey MonitorDot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MonitorDot));

        /// <summary>Lucide <c>monitor-down</c> icon.</summary>
        public static ComponentResourceKey MonitorDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MonitorDown));

        /// <summary>Lucide <c>monitor-off</c> icon.</summary>
        public static ComponentResourceKey MonitorOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MonitorOff));

        /// <summary>Lucide <c>monitor-pause</c> icon.</summary>
        public static ComponentResourceKey MonitorPause { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MonitorPause));

        /// <summary>Lucide <c>monitor-play</c> icon.</summary>
        public static ComponentResourceKey MonitorPlay { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MonitorPlay));

        /// <summary>Lucide <c>monitor-smartphone</c> icon.</summary>
        public static ComponentResourceKey MonitorSmartphone { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MonitorSmartphone));

        /// <summary>Lucide <c>monitor-speaker</c> icon.</summary>
        public static ComponentResourceKey MonitorSpeaker { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MonitorSpeaker));

        /// <summary>Lucide <c>monitor-stop</c> icon.</summary>
        public static ComponentResourceKey MonitorStop { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MonitorStop));

        /// <summary>Lucide <c>monitor-up</c> icon.</summary>
        public static ComponentResourceKey MonitorUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MonitorUp));

        /// <summary>Lucide <c>monitor-x</c> icon.</summary>
        public static ComponentResourceKey MonitorX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MonitorX));

        /// <summary>Lucide <c>monitor</c> icon.</summary>
        public static ComponentResourceKey Monitor { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Monitor));

        /// <summary>Lucide <c>moon-star</c> icon.</summary>
        public static ComponentResourceKey MoonStar { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MoonStar));

        /// <summary>Lucide <c>moon</c> icon.</summary>
        public static ComponentResourceKey Moon { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Moon));

        /// <summary>Lucide <c>motorbike</c> icon.</summary>
        public static ComponentResourceKey Motorbike { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Motorbike));

        /// <summary>Lucide <c>mountain-snow</c> icon.</summary>
        public static ComponentResourceKey MountainSnow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MountainSnow));

        /// <summary>Lucide <c>mountain</c> icon.</summary>
        public static ComponentResourceKey Mountain { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Mountain));

        /// <summary>Lucide <c>mouse-left</c> icon.</summary>
        public static ComponentResourceKey MouseLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MouseLeft));

        /// <summary>Lucide <c>mouse-off</c> icon.</summary>
        public static ComponentResourceKey MouseOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MouseOff));

        /// <summary>Lucide <c>mouse-pointer-2-off</c> icon.</summary>
        public static ComponentResourceKey MousePointer2Off { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MousePointer2Off));

        /// <summary>Lucide <c>mouse-pointer-2</c> icon.</summary>
        public static ComponentResourceKey MousePointer2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MousePointer2));

        /// <summary>Lucide <c>mouse-pointer-ban</c> icon.</summary>
        public static ComponentResourceKey MousePointerBan { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MousePointerBan));

        /// <summary>Lucide <c>mouse-pointer-click</c> icon.</summary>
        public static ComponentResourceKey MousePointerClick { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MousePointerClick));

        /// <summary>Lucide <c>mouse-pointer</c> icon.</summary>
        public static ComponentResourceKey MousePointer { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MousePointer));

        /// <summary>Lucide <c>mouse-right</c> icon.</summary>
        public static ComponentResourceKey MouseRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MouseRight));

        /// <summary>Lucide <c>mouse</c> icon.</summary>
        public static ComponentResourceKey Mouse { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Mouse));

        /// <summary>Lucide <c>move-3d</c> icon.</summary>
        public static ComponentResourceKey Move3d { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Move3d));

        /// <summary>Lucide <c>move-diagonal-2</c> icon.</summary>
        public static ComponentResourceKey MoveDiagonal2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MoveDiagonal2));

        /// <summary>Lucide <c>move-diagonal</c> icon.</summary>
        public static ComponentResourceKey MoveDiagonal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MoveDiagonal));

        /// <summary>Lucide <c>move-down-left</c> icon.</summary>
        public static ComponentResourceKey MoveDownLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MoveDownLeft));

        /// <summary>Lucide <c>move-down-right</c> icon.</summary>
        public static ComponentResourceKey MoveDownRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MoveDownRight));

        /// <summary>Lucide <c>move-down</c> icon.</summary>
        public static ComponentResourceKey MoveDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MoveDown));

        /// <summary>Lucide <c>move-horizontal</c> icon.</summary>
        public static ComponentResourceKey MoveHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MoveHorizontal));

        /// <summary>Lucide <c>move-left</c> icon.</summary>
        public static ComponentResourceKey MoveLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MoveLeft));

        /// <summary>Lucide <c>move-right</c> icon.</summary>
        public static ComponentResourceKey MoveRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MoveRight));

        /// <summary>Lucide <c>move-up-left</c> icon.</summary>
        public static ComponentResourceKey MoveUpLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MoveUpLeft));

        /// <summary>Lucide <c>move-up-right</c> icon.</summary>
        public static ComponentResourceKey MoveUpRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MoveUpRight));

        /// <summary>Lucide <c>move-up</c> icon.</summary>
        public static ComponentResourceKey MoveUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MoveUp));

        /// <summary>Lucide <c>move-vertical</c> icon.</summary>
        public static ComponentResourceKey MoveVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(MoveVertical));

        /// <summary>Lucide <c>move</c> icon.</summary>
        public static ComponentResourceKey Move { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Move));

        /// <summary>Lucide <c>music-2</c> icon.</summary>
        public static ComponentResourceKey Music2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Music2));

        /// <summary>Lucide <c>music-3</c> icon.</summary>
        public static ComponentResourceKey Music3 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Music3));

        /// <summary>Lucide <c>music-4</c> icon.</summary>
        public static ComponentResourceKey Music4 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Music4));

        /// <summary>Lucide <c>music</c> icon.</summary>
        public static ComponentResourceKey Music { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Music));

        // ─── N ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>navigation-2-off</c> icon.</summary>
        public static ComponentResourceKey Navigation2Off { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Navigation2Off));

        /// <summary>Lucide <c>navigation-2</c> icon.</summary>
        public static ComponentResourceKey Navigation2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Navigation2));

        /// <summary>Lucide <c>navigation-off</c> icon.</summary>
        public static ComponentResourceKey NavigationOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(NavigationOff));

        /// <summary>Lucide <c>navigation</c> icon.</summary>
        public static ComponentResourceKey Navigation { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Navigation));

        /// <summary>Lucide <c>network</c> icon.</summary>
        public static ComponentResourceKey Network { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Network));

        /// <summary>Lucide <c>newspaper</c> icon.</summary>
        public static ComponentResourceKey Newspaper { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Newspaper));

        /// <summary>Lucide <c>nfc</c> icon.</summary>
        public static ComponentResourceKey Nfc { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Nfc));

        /// <summary>Lucide <c>non-binary</c> icon.</summary>
        public static ComponentResourceKey NonBinary { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(NonBinary));

        /// <summary>Lucide <c>notebook-pen</c> icon.</summary>
        public static ComponentResourceKey NotebookPen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(NotebookPen));

        /// <summary>Lucide <c>notebook-tabs</c> icon.</summary>
        public static ComponentResourceKey NotebookTabs { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(NotebookTabs));

        /// <summary>Lucide <c>notebook-text</c> icon.</summary>
        public static ComponentResourceKey NotebookText { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(NotebookText));

        /// <summary>Lucide <c>notebook</c> icon.</summary>
        public static ComponentResourceKey Notebook { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Notebook));

        /// <summary>Lucide <c>notepad-text-dashed</c> icon.</summary>
        public static ComponentResourceKey NotepadTextDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(NotepadTextDashed));

        /// <summary>Lucide <c>notepad-text</c> icon.</summary>
        public static ComponentResourceKey NotepadText { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(NotepadText));

        /// <summary>Lucide <c>nut-off</c> icon.</summary>
        public static ComponentResourceKey NutOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(NutOff));

        /// <summary>Lucide <c>nut</c> icon.</summary>
        public static ComponentResourceKey Nut { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Nut));

        // ─── O ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>octagon-alert</c> icon.</summary>
        public static ComponentResourceKey OctagonAlert { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(OctagonAlert));

        /// <summary>Lucide <c>octagon-minus</c> icon.</summary>
        public static ComponentResourceKey OctagonMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(OctagonMinus));

        /// <summary>Lucide <c>octagon-pause</c> icon.</summary>
        public static ComponentResourceKey OctagonPause { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(OctagonPause));

        /// <summary>Lucide <c>octagon-x</c> icon.</summary>
        public static ComponentResourceKey OctagonX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(OctagonX));

        /// <summary>Lucide <c>octagon</c> icon.</summary>
        public static ComponentResourceKey Octagon { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Octagon));

        /// <summary>Lucide <c>omega</c> icon.</summary>
        public static ComponentResourceKey Omega { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Omega));

        /// <summary>Lucide <c>option</c> icon.</summary>
        public static ComponentResourceKey Option { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Option));

        /// <summary>Lucide <c>orbit</c> icon.</summary>
        public static ComponentResourceKey Orbit { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Orbit));

        /// <summary>Lucide <c>origami</c> icon.</summary>
        public static ComponentResourceKey Origami { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Origami));

        // ─── P ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>package-2</c> icon.</summary>
        public static ComponentResourceKey Package2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Package2));

        /// <summary>Lucide <c>package-check</c> icon.</summary>
        public static ComponentResourceKey PackageCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PackageCheck));

        /// <summary>Lucide <c>package-minus</c> icon.</summary>
        public static ComponentResourceKey PackageMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PackageMinus));

        /// <summary>Lucide <c>package-open</c> icon.</summary>
        public static ComponentResourceKey PackageOpen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PackageOpen));

        /// <summary>Lucide <c>package-plus</c> icon.</summary>
        public static ComponentResourceKey PackagePlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PackagePlus));

        /// <summary>Lucide <c>package-search</c> icon.</summary>
        public static ComponentResourceKey PackageSearch { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PackageSearch));

        /// <summary>Lucide <c>package-x</c> icon.</summary>
        public static ComponentResourceKey PackageX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PackageX));

        /// <summary>Lucide <c>package</c> icon.</summary>
        public static ComponentResourceKey Package { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Package));

        /// <summary>Lucide <c>paint-bucket</c> icon.</summary>
        public static ComponentResourceKey PaintBucket { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PaintBucket));

        /// <summary>Lucide <c>paint-roller</c> icon.</summary>
        public static ComponentResourceKey PaintRoller { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PaintRoller));

        /// <summary>Lucide <c>paintbrush-vertical</c> icon.</summary>
        public static ComponentResourceKey PaintbrushVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PaintbrushVertical));

        /// <summary>Lucide <c>paintbrush</c> icon.</summary>
        public static ComponentResourceKey Paintbrush { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Paintbrush));

        /// <summary>Lucide <c>palette</c> icon.</summary>
        public static ComponentResourceKey Palette { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Palette));

        /// <summary>Lucide <c>panda</c> icon.</summary>
        public static ComponentResourceKey Panda { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Panda));

        /// <summary>Lucide <c>panel-bottom-close</c> icon.</summary>
        public static ComponentResourceKey PanelBottomClose { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelBottomClose));

        /// <summary>Lucide <c>panel-bottom-dashed</c> icon.</summary>
        public static ComponentResourceKey PanelBottomDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelBottomDashed));

        /// <summary>Lucide <c>panel-bottom-open</c> icon.</summary>
        public static ComponentResourceKey PanelBottomOpen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelBottomOpen));

        /// <summary>Lucide <c>panel-bottom</c> icon.</summary>
        public static ComponentResourceKey PanelBottom { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelBottom));

        /// <summary>Lucide <c>panel-left-close</c> icon.</summary>
        public static ComponentResourceKey PanelLeftClose { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelLeftClose));

        /// <summary>Lucide <c>panel-left-dashed</c> icon.</summary>
        public static ComponentResourceKey PanelLeftDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelLeftDashed));

        /// <summary>Lucide <c>panel-left-open</c> icon.</summary>
        public static ComponentResourceKey PanelLeftOpen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelLeftOpen));

        /// <summary>Lucide <c>panel-left-right-dashed</c> icon.</summary>
        public static ComponentResourceKey PanelLeftRightDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelLeftRightDashed));

        /// <summary>Lucide <c>panel-left</c> icon.</summary>
        public static ComponentResourceKey PanelLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelLeft));

        /// <summary>Lucide <c>panel-right-close</c> icon.</summary>
        public static ComponentResourceKey PanelRightClose { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelRightClose));

        /// <summary>Lucide <c>panel-right-dashed</c> icon.</summary>
        public static ComponentResourceKey PanelRightDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelRightDashed));

        /// <summary>Lucide <c>panel-right-open</c> icon.</summary>
        public static ComponentResourceKey PanelRightOpen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelRightOpen));

        /// <summary>Lucide <c>panel-right</c> icon.</summary>
        public static ComponentResourceKey PanelRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelRight));

        /// <summary>Lucide <c>panel-top-bottom-dashed</c> icon.</summary>
        public static ComponentResourceKey PanelTopBottomDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelTopBottomDashed));

        /// <summary>Lucide <c>panel-top-close</c> icon.</summary>
        public static ComponentResourceKey PanelTopClose { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelTopClose));

        /// <summary>Lucide <c>panel-top-dashed</c> icon.</summary>
        public static ComponentResourceKey PanelTopDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelTopDashed));

        /// <summary>Lucide <c>panel-top-open</c> icon.</summary>
        public static ComponentResourceKey PanelTopOpen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelTopOpen));

        /// <summary>Lucide <c>panel-top</c> icon.</summary>
        public static ComponentResourceKey PanelTop { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelTop));

        /// <summary>Lucide <c>panels-left-bottom</c> icon.</summary>
        public static ComponentResourceKey PanelsLeftBottom { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelsLeftBottom));

        /// <summary>Lucide <c>panels-right-bottom</c> icon.</summary>
        public static ComponentResourceKey PanelsRightBottom { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelsRightBottom));

        /// <summary>Lucide <c>panels-top-left</c> icon.</summary>
        public static ComponentResourceKey PanelsTopLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PanelsTopLeft));

        /// <summary>Lucide <c>paper-bag</c> icon.</summary>
        public static ComponentResourceKey PaperBag { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PaperBag));

        /// <summary>Lucide <c>paperclip</c> icon.</summary>
        public static ComponentResourceKey Paperclip { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Paperclip));

        /// <summary>Lucide <c>parasol</c> icon.</summary>
        public static ComponentResourceKey Parasol { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Parasol));

        /// <summary>Lucide <c>parentheses</c> icon.</summary>
        public static ComponentResourceKey Parentheses { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Parentheses));

        /// <summary>Lucide <c>parking-meter</c> icon.</summary>
        public static ComponentResourceKey ParkingMeter { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ParkingMeter));

        /// <summary>Lucide <c>party-popper</c> icon.</summary>
        public static ComponentResourceKey PartyPopper { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PartyPopper));

        /// <summary>Lucide <c>pause</c> icon.</summary>
        public static ComponentResourceKey Pause { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Pause));

        /// <summary>Lucide <c>paw-print</c> icon.</summary>
        public static ComponentResourceKey PawPrint { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PawPrint));

        /// <summary>Lucide <c>pc-case</c> icon.</summary>
        public static ComponentResourceKey PcCase { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PcCase));

        /// <summary>Lucide <c>pen-line</c> icon.</summary>
        public static ComponentResourceKey PenLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PenLine));

        /// <summary>Lucide <c>pen-off</c> icon.</summary>
        public static ComponentResourceKey PenOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PenOff));

        /// <summary>Lucide <c>pen-tool</c> icon.</summary>
        public static ComponentResourceKey PenTool { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PenTool));

        /// <summary>Lucide <c>pen</c> icon.</summary>
        public static ComponentResourceKey Pen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Pen));

        /// <summary>Lucide <c>pencil-line</c> icon.</summary>
        public static ComponentResourceKey PencilLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PencilLine));

        /// <summary>Lucide <c>pencil-off</c> icon.</summary>
        public static ComponentResourceKey PencilOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PencilOff));

        /// <summary>Lucide <c>pencil-ruler</c> icon.</summary>
        public static ComponentResourceKey PencilRuler { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PencilRuler));

        /// <summary>Lucide <c>pencil-sparkles</c> icon.</summary>
        public static ComponentResourceKey PencilSparkles { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PencilSparkles));

        /// <summary>Lucide <c>pencil</c> icon.</summary>
        public static ComponentResourceKey Pencil { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Pencil));

        /// <summary>Lucide <c>pentagon</c> icon.</summary>
        public static ComponentResourceKey Pentagon { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Pentagon));

        /// <summary>Lucide <c>percent</c> icon.</summary>
        public static ComponentResourceKey Percent { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Percent));

        /// <summary>Lucide <c>person-standing</c> icon.</summary>
        public static ComponentResourceKey PersonStanding { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PersonStanding));

        /// <summary>Lucide <c>phi</c> icon.</summary>
        public static ComponentResourceKey Phi { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Phi));

        /// <summary>Lucide <c>philippine-peso</c> icon.</summary>
        public static ComponentResourceKey PhilippinePeso { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PhilippinePeso));

        /// <summary>Lucide <c>phone-call</c> icon.</summary>
        public static ComponentResourceKey PhoneCall { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PhoneCall));

        /// <summary>Lucide <c>phone-forwarded</c> icon.</summary>
        public static ComponentResourceKey PhoneForwarded { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PhoneForwarded));

        /// <summary>Lucide <c>phone-incoming</c> icon.</summary>
        public static ComponentResourceKey PhoneIncoming { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PhoneIncoming));

        /// <summary>Lucide <c>phone-missed</c> icon.</summary>
        public static ComponentResourceKey PhoneMissed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PhoneMissed));

        /// <summary>Lucide <c>phone-off</c> icon.</summary>
        public static ComponentResourceKey PhoneOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PhoneOff));

        /// <summary>Lucide <c>phone-outgoing</c> icon.</summary>
        public static ComponentResourceKey PhoneOutgoing { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PhoneOutgoing));

        /// <summary>Lucide <c>phone</c> icon.</summary>
        public static ComponentResourceKey Phone { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Phone));

        /// <summary>Lucide <c>pi</c> icon.</summary>
        public static ComponentResourceKey Pi { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Pi));

        /// <summary>Lucide <c>piano</c> icon.</summary>
        public static ComponentResourceKey Piano { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Piano));

        /// <summary>Lucide <c>pickaxe</c> icon.</summary>
        public static ComponentResourceKey Pickaxe { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Pickaxe));

        /// <summary>Lucide <c>picture-in-picture-2</c> icon.</summary>
        public static ComponentResourceKey PictureInPicture2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PictureInPicture2));

        /// <summary>Lucide <c>picture-in-picture</c> icon.</summary>
        public static ComponentResourceKey PictureInPicture { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PictureInPicture));

        /// <summary>Lucide <c>piggy-bank</c> icon.</summary>
        public static ComponentResourceKey PiggyBank { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PiggyBank));

        /// <summary>Lucide <c>pilcrow-left</c> icon.</summary>
        public static ComponentResourceKey PilcrowLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PilcrowLeft));

        /// <summary>Lucide <c>pilcrow-right</c> icon.</summary>
        public static ComponentResourceKey PilcrowRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PilcrowRight));

        /// <summary>Lucide <c>pilcrow</c> icon.</summary>
        public static ComponentResourceKey Pilcrow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Pilcrow));

        /// <summary>Lucide <c>pill-bottle</c> icon.</summary>
        public static ComponentResourceKey PillBottle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PillBottle));

        /// <summary>Lucide <c>pill</c> icon.</summary>
        public static ComponentResourceKey Pill { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Pill));

        /// <summary>Lucide <c>pin-off</c> icon.</summary>
        public static ComponentResourceKey PinOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PinOff));

        /// <summary>Lucide <c>pin</c> icon.</summary>
        public static ComponentResourceKey Pin { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Pin));

        /// <summary>Lucide <c>pipette</c> icon.</summary>
        public static ComponentResourceKey Pipette { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Pipette));

        /// <summary>Lucide <c>pizza</c> icon.</summary>
        public static ComponentResourceKey Pizza { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Pizza));

        /// <summary>Lucide <c>plane-landing</c> icon.</summary>
        public static ComponentResourceKey PlaneLanding { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PlaneLanding));

        /// <summary>Lucide <c>plane-takeoff</c> icon.</summary>
        public static ComponentResourceKey PlaneTakeoff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PlaneTakeoff));

        /// <summary>Lucide <c>plane</c> icon.</summary>
        public static ComponentResourceKey Plane { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Plane));

        /// <summary>Lucide <c>play-off</c> icon.</summary>
        public static ComponentResourceKey PlayOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PlayOff));

        /// <summary>Lucide <c>play</c> icon.</summary>
        public static ComponentResourceKey Play { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Play));

        /// <summary>Lucide <c>plug-2</c> icon.</summary>
        public static ComponentResourceKey Plug2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Plug2));

        /// <summary>Lucide <c>plug-zap</c> icon.</summary>
        public static ComponentResourceKey PlugZap { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PlugZap));

        /// <summary>Lucide <c>plug</c> icon.</summary>
        public static ComponentResourceKey Plug { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Plug));

        /// <summary>Lucide <c>plus</c> icon.</summary>
        public static ComponentResourceKey Plus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Plus));

        /// <summary>Lucide <c>pocket-knife</c> icon.</summary>
        public static ComponentResourceKey PocketKnife { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PocketKnife));

        /// <summary>Lucide <c>podcast</c> icon.</summary>
        public static ComponentResourceKey Podcast { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Podcast));

        /// <summary>Lucide <c>podium</c> icon.</summary>
        public static ComponentResourceKey Podium { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Podium));

        /// <summary>Lucide <c>pointer-off</c> icon.</summary>
        public static ComponentResourceKey PointerOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PointerOff));

        /// <summary>Lucide <c>pointer</c> icon.</summary>
        public static ComponentResourceKey Pointer { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Pointer));

        /// <summary>Lucide <c>popcorn</c> icon.</summary>
        public static ComponentResourceKey Popcorn { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Popcorn));

        /// <summary>Lucide <c>popsicle</c> icon.</summary>
        public static ComponentResourceKey Popsicle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Popsicle));

        /// <summary>Lucide <c>pound-sterling</c> icon.</summary>
        public static ComponentResourceKey PoundSterling { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PoundSterling));

        /// <summary>Lucide <c>power-off</c> icon.</summary>
        public static ComponentResourceKey PowerOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PowerOff));

        /// <summary>Lucide <c>power</c> icon.</summary>
        public static ComponentResourceKey Power { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Power));

        /// <summary>Lucide <c>presentation</c> icon.</summary>
        public static ComponentResourceKey Presentation { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Presentation));

        /// <summary>Lucide <c>printer-check</c> icon.</summary>
        public static ComponentResourceKey PrinterCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PrinterCheck));

        /// <summary>Lucide <c>printer-x</c> icon.</summary>
        public static ComponentResourceKey PrinterX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(PrinterX));

        /// <summary>Lucide <c>printer</c> icon.</summary>
        public static ComponentResourceKey Printer { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Printer));

        /// <summary>Lucide <c>projector</c> icon.</summary>
        public static ComponentResourceKey Projector { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Projector));

        /// <summary>Lucide <c>proportions</c> icon.</summary>
        public static ComponentResourceKey Proportions { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Proportions));

        /// <summary>Lucide <c>puzzle</c> icon.</summary>
        public static ComponentResourceKey Puzzle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Puzzle));

        /// <summary>Lucide <c>pyramid</c> icon.</summary>
        public static ComponentResourceKey Pyramid { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Pyramid));

        // ─── Q ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>qr-code</c> icon.</summary>
        public static ComponentResourceKey QrCode { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(QrCode));

        /// <summary>Lucide <c>quote</c> icon.</summary>
        public static ComponentResourceKey Quote { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Quote));

        // ─── R ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>rabbit</c> icon.</summary>
        public static ComponentResourceKey Rabbit { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Rabbit));

        /// <summary>Lucide <c>radar</c> icon.</summary>
        public static ComponentResourceKey Radar { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Radar));

        /// <summary>Lucide <c>radiation</c> icon.</summary>
        public static ComponentResourceKey Radiation { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Radiation));

        /// <summary>Lucide <c>radical</c> icon.</summary>
        public static ComponentResourceKey Radical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Radical));

        /// <summary>Lucide <c>radio-off</c> icon.</summary>
        public static ComponentResourceKey RadioOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RadioOff));

        /// <summary>Lucide <c>radio-receiver</c> icon.</summary>
        public static ComponentResourceKey RadioReceiver { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RadioReceiver));

        /// <summary>Lucide <c>radio-tower</c> icon.</summary>
        public static ComponentResourceKey RadioTower { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RadioTower));

        /// <summary>Lucide <c>radio</c> icon.</summary>
        public static ComponentResourceKey Radio { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Radio));

        /// <summary>Lucide <c>radius</c> icon.</summary>
        public static ComponentResourceKey Radius { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Radius));

        /// <summary>Lucide <c>rainbow</c> icon.</summary>
        public static ComponentResourceKey Rainbow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Rainbow));

        /// <summary>Lucide <c>rat</c> icon.</summary>
        public static ComponentResourceKey Rat { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Rat));

        /// <summary>Lucide <c>ratio</c> icon.</summary>
        public static ComponentResourceKey Ratio { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ratio));

        /// <summary>Lucide <c>receipt-cent</c> icon.</summary>
        public static ComponentResourceKey ReceiptCent { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ReceiptCent));

        /// <summary>Lucide <c>receipt-euro</c> icon.</summary>
        public static ComponentResourceKey ReceiptEuro { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ReceiptEuro));

        /// <summary>Lucide <c>receipt-indian-rupee</c> icon.</summary>
        public static ComponentResourceKey ReceiptIndianRupee { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ReceiptIndianRupee));

        /// <summary>Lucide <c>receipt-japanese-yen</c> icon.</summary>
        public static ComponentResourceKey ReceiptJapaneseYen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ReceiptJapaneseYen));

        /// <summary>Lucide <c>receipt-pound-sterling</c> icon.</summary>
        public static ComponentResourceKey ReceiptPoundSterling { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ReceiptPoundSterling));

        /// <summary>Lucide <c>receipt-russian-ruble</c> icon.</summary>
        public static ComponentResourceKey ReceiptRussianRuble { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ReceiptRussianRuble));

        /// <summary>Lucide <c>receipt-swiss-franc</c> icon.</summary>
        public static ComponentResourceKey ReceiptSwissFranc { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ReceiptSwissFranc));

        /// <summary>Lucide <c>receipt-text</c> icon.</summary>
        public static ComponentResourceKey ReceiptText { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ReceiptText));

        /// <summary>Lucide <c>receipt-turkish-lira</c> icon.</summary>
        public static ComponentResourceKey ReceiptTurkishLira { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ReceiptTurkishLira));

        /// <summary>Lucide <c>receipt</c> icon.</summary>
        public static ComponentResourceKey Receipt { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Receipt));

        /// <summary>Lucide <c>rectangle-circle</c> icon.</summary>
        public static ComponentResourceKey RectangleCircle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RectangleCircle));

        /// <summary>Lucide <c>rectangle-ellipsis</c> icon.</summary>
        public static ComponentResourceKey RectangleEllipsis { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RectangleEllipsis));

        /// <summary>Lucide <c>rectangle-goggles</c> icon.</summary>
        public static ComponentResourceKey RectangleGoggles { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RectangleGoggles));

        /// <summary>Lucide <c>rectangle-horizontal</c> icon.</summary>
        public static ComponentResourceKey RectangleHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RectangleHorizontal));

        /// <summary>Lucide <c>rectangle-vertical</c> icon.</summary>
        public static ComponentResourceKey RectangleVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RectangleVertical));

        /// <summary>Lucide <c>recycle</c> icon.</summary>
        public static ComponentResourceKey Recycle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Recycle));

        /// <summary>Lucide <c>redo-2</c> icon.</summary>
        public static ComponentResourceKey Redo2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Redo2));

        /// <summary>Lucide <c>redo-dot</c> icon.</summary>
        public static ComponentResourceKey RedoDot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RedoDot));

        /// <summary>Lucide <c>redo</c> icon.</summary>
        public static ComponentResourceKey Redo { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Redo));

        /// <summary>Lucide <c>refresh-ccw-dot</c> icon.</summary>
        public static ComponentResourceKey RefreshCcwDot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RefreshCcwDot));

        /// <summary>Lucide <c>refresh-ccw</c> icon.</summary>
        public static ComponentResourceKey RefreshCcw { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RefreshCcw));

        /// <summary>Lucide <c>refresh-cw-off</c> icon.</summary>
        public static ComponentResourceKey RefreshCwOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RefreshCwOff));

        /// <summary>Lucide <c>refresh-cw</c> icon.</summary>
        public static ComponentResourceKey RefreshCw { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RefreshCw));

        /// <summary>Lucide <c>refrigerator</c> icon.</summary>
        public static ComponentResourceKey Refrigerator { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Refrigerator));

        /// <summary>Lucide <c>regex</c> icon.</summary>
        public static ComponentResourceKey Regex { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Regex));

        /// <summary>Lucide <c>remove-formatting</c> icon.</summary>
        public static ComponentResourceKey RemoveFormatting { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RemoveFormatting));

        /// <summary>Lucide <c>repeat-1</c> icon.</summary>
        public static ComponentResourceKey Repeat1 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Repeat1));

        /// <summary>Lucide <c>repeat-2</c> icon.</summary>
        public static ComponentResourceKey Repeat2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Repeat2));

        /// <summary>Lucide <c>repeat-off</c> icon.</summary>
        public static ComponentResourceKey RepeatOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RepeatOff));

        /// <summary>Lucide <c>repeat</c> icon.</summary>
        public static ComponentResourceKey Repeat { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Repeat));

        /// <summary>Lucide <c>replace-all</c> icon.</summary>
        public static ComponentResourceKey ReplaceAll { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ReplaceAll));

        /// <summary>Lucide <c>replace</c> icon.</summary>
        public static ComponentResourceKey Replace { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Replace));

        /// <summary>Lucide <c>reply-all</c> icon.</summary>
        public static ComponentResourceKey ReplyAll { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ReplyAll));

        /// <summary>Lucide <c>reply</c> icon.</summary>
        public static ComponentResourceKey Reply { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Reply));

        /// <summary>Lucide <c>rewind</c> icon.</summary>
        public static ComponentResourceKey Rewind { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Rewind));

        /// <summary>Lucide <c>ribbon</c> icon.</summary>
        public static ComponentResourceKey Ribbon { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ribbon));

        /// <summary>Lucide <c>road</c> icon.</summary>
        public static ComponentResourceKey Road { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Road));

        /// <summary>Lucide <c>rocket</c> icon.</summary>
        public static ComponentResourceKey Rocket { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Rocket));

        /// <summary>Lucide <c>rocking-chair</c> icon.</summary>
        public static ComponentResourceKey RockingChair { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RockingChair));

        /// <summary>Lucide <c>roller-coaster</c> icon.</summary>
        public static ComponentResourceKey RollerCoaster { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RollerCoaster));

        /// <summary>Lucide <c>rose</c> icon.</summary>
        public static ComponentResourceKey Rose { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Rose));

        /// <summary>Lucide <c>rotate-3d</c> icon.</summary>
        public static ComponentResourceKey Rotate3d { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Rotate3d));

        /// <summary>Lucide <c>rotate-ccw-key</c> icon.</summary>
        public static ComponentResourceKey RotateCcwKey { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RotateCcwKey));

        /// <summary>Lucide <c>rotate-ccw-square</c> icon.</summary>
        public static ComponentResourceKey RotateCcwSquare { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RotateCcwSquare));

        /// <summary>Lucide <c>rotate-ccw</c> icon.</summary>
        public static ComponentResourceKey RotateCcw { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RotateCcw));

        /// <summary>Lucide <c>rotate-cw-square</c> icon.</summary>
        public static ComponentResourceKey RotateCwSquare { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RotateCwSquare));

        /// <summary>Lucide <c>rotate-cw</c> icon.</summary>
        public static ComponentResourceKey RotateCw { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RotateCw));

        /// <summary>Lucide <c>route-off</c> icon.</summary>
        public static ComponentResourceKey RouteOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RouteOff));

        /// <summary>Lucide <c>route</c> icon.</summary>
        public static ComponentResourceKey Route { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Route));

        /// <summary>Lucide <c>router</c> icon.</summary>
        public static ComponentResourceKey Router { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Router));

        /// <summary>Lucide <c>rows-2</c> icon.</summary>
        public static ComponentResourceKey Rows2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Rows2));

        /// <summary>Lucide <c>rows-3</c> icon.</summary>
        public static ComponentResourceKey Rows3 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Rows3));

        /// <summary>Lucide <c>rows-4</c> icon.</summary>
        public static ComponentResourceKey Rows4 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Rows4));

        /// <summary>Lucide <c>rss</c> icon.</summary>
        public static ComponentResourceKey Rss { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Rss));

        /// <summary>Lucide <c>ruler-dimension-line</c> icon.</summary>
        public static ComponentResourceKey RulerDimensionLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RulerDimensionLine));

        /// <summary>Lucide <c>ruler</c> icon.</summary>
        public static ComponentResourceKey Ruler { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ruler));

        /// <summary>Lucide <c>russian-ruble</c> icon.</summary>
        public static ComponentResourceKey RussianRuble { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(RussianRuble));

        // ─── S ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>sailboat</c> icon.</summary>
        public static ComponentResourceKey Sailboat { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Sailboat));

        /// <summary>Lucide <c>salad</c> icon.</summary>
        public static ComponentResourceKey Salad { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Salad));

        /// <summary>Lucide <c>sandwich</c> icon.</summary>
        public static ComponentResourceKey Sandwich { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Sandwich));

        /// <summary>Lucide <c>satellite-dish</c> icon.</summary>
        public static ComponentResourceKey SatelliteDish { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SatelliteDish));

        /// <summary>Lucide <c>satellite</c> icon.</summary>
        public static ComponentResourceKey Satellite { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Satellite));

        /// <summary>Lucide <c>saudi-riyal</c> icon.</summary>
        public static ComponentResourceKey SaudiRiyal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SaudiRiyal));

        /// <summary>Lucide <c>save-all</c> icon.</summary>
        public static ComponentResourceKey SaveAll { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SaveAll));

        /// <summary>Lucide <c>save-check</c> icon.</summary>
        public static ComponentResourceKey SaveCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SaveCheck));

        /// <summary>Lucide <c>save-off</c> icon.</summary>
        public static ComponentResourceKey SaveOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SaveOff));

        /// <summary>Lucide <c>save-pen</c> icon.</summary>
        public static ComponentResourceKey SavePen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SavePen));

        /// <summary>Lucide <c>save-plus</c> icon.</summary>
        public static ComponentResourceKey SavePlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SavePlus));

        /// <summary>Lucide <c>save</c> icon.</summary>
        public static ComponentResourceKey Save { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Save));

        /// <summary>Lucide <c>scale-3d</c> icon.</summary>
        public static ComponentResourceKey Scale3d { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Scale3d));

        /// <summary>Lucide <c>scale</c> icon.</summary>
        public static ComponentResourceKey Scale { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Scale));

        /// <summary>Lucide <c>scaling</c> icon.</summary>
        public static ComponentResourceKey Scaling { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Scaling));

        /// <summary>Lucide <c>scan-barcode</c> icon.</summary>
        public static ComponentResourceKey ScanBarcode { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ScanBarcode));

        /// <summary>Lucide <c>scan-box</c> icon.</summary>
        public static ComponentResourceKey ScanBox { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ScanBox));

        /// <summary>Lucide <c>scan-eye</c> icon.</summary>
        public static ComponentResourceKey ScanEye { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ScanEye));

        /// <summary>Lucide <c>scan-face</c> icon.</summary>
        public static ComponentResourceKey ScanFace { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ScanFace));

        /// <summary>Lucide <c>scan-heart</c> icon.</summary>
        public static ComponentResourceKey ScanHeart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ScanHeart));

        /// <summary>Lucide <c>scan-line</c> icon.</summary>
        public static ComponentResourceKey ScanLine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ScanLine));

        /// <summary>Lucide <c>scan-qr-code</c> icon.</summary>
        public static ComponentResourceKey ScanQrCode { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ScanQrCode));

        /// <summary>Lucide <c>scan-search</c> icon.</summary>
        public static ComponentResourceKey ScanSearch { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ScanSearch));

        /// <summary>Lucide <c>scan-text</c> icon.</summary>
        public static ComponentResourceKey ScanText { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ScanText));

        /// <summary>Lucide <c>scan</c> icon.</summary>
        public static ComponentResourceKey Scan { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Scan));

        /// <summary>Lucide <c>school</c> icon.</summary>
        public static ComponentResourceKey School { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(School));

        /// <summary>Lucide <c>scissors-line-dashed</c> icon.</summary>
        public static ComponentResourceKey ScissorsLineDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ScissorsLineDashed));

        /// <summary>Lucide <c>scissors</c> icon.</summary>
        public static ComponentResourceKey Scissors { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Scissors));

        /// <summary>Lucide <c>scooter</c> icon.</summary>
        public static ComponentResourceKey Scooter { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Scooter));

        /// <summary>Lucide <c>screen-share-off</c> icon.</summary>
        public static ComponentResourceKey ScreenShareOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ScreenShareOff));

        /// <summary>Lucide <c>screen-share</c> icon.</summary>
        public static ComponentResourceKey ScreenShare { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ScreenShare));

        /// <summary>Lucide <c>scroll-text</c> icon.</summary>
        public static ComponentResourceKey ScrollText { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ScrollText));

        /// <summary>Lucide <c>scroll</c> icon.</summary>
        public static ComponentResourceKey Scroll { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Scroll));

        /// <summary>Lucide <c>search-alert</c> icon.</summary>
        public static ComponentResourceKey SearchAlert { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SearchAlert));

        /// <summary>Lucide <c>search-check</c> icon.</summary>
        public static ComponentResourceKey SearchCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SearchCheck));

        /// <summary>Lucide <c>search-code</c> icon.</summary>
        public static ComponentResourceKey SearchCode { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SearchCode));

        /// <summary>Lucide <c>search-slash</c> icon.</summary>
        public static ComponentResourceKey SearchSlash { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SearchSlash));

        /// <summary>Lucide <c>search-x</c> icon.</summary>
        public static ComponentResourceKey SearchX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SearchX));

        /// <summary>Lucide <c>search</c> icon.</summary>
        public static ComponentResourceKey Search { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Search));

        /// <summary>Lucide <c>section</c> icon.</summary>
        public static ComponentResourceKey Section { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Section));

        /// <summary>Lucide <c>send-horizontal</c> icon.</summary>
        public static ComponentResourceKey SendHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SendHorizontal));

        /// <summary>Lucide <c>send-to-back</c> icon.</summary>
        public static ComponentResourceKey SendToBack { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SendToBack));

        /// <summary>Lucide <c>send</c> icon.</summary>
        public static ComponentResourceKey Send { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Send));

        /// <summary>Lucide <c>separator-horizontal</c> icon.</summary>
        public static ComponentResourceKey SeparatorHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SeparatorHorizontal));

        /// <summary>Lucide <c>separator-vertical</c> icon.</summary>
        public static ComponentResourceKey SeparatorVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SeparatorVertical));

        /// <summary>Lucide <c>server-cog</c> icon.</summary>
        public static ComponentResourceKey ServerCog { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ServerCog));

        /// <summary>Lucide <c>server-crash</c> icon.</summary>
        public static ComponentResourceKey ServerCrash { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ServerCrash));

        /// <summary>Lucide <c>server-off</c> icon.</summary>
        public static ComponentResourceKey ServerOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ServerOff));

        /// <summary>Lucide <c>server-plus</c> icon.</summary>
        public static ComponentResourceKey ServerPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ServerPlus));

        /// <summary>Lucide <c>server</c> icon.</summary>
        public static ComponentResourceKey Server { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Server));

        /// <summary>Lucide <c>settings-2</c> icon.</summary>
        public static ComponentResourceKey Settings2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Settings2));

        /// <summary>Lucide <c>settings</c> icon.</summary>
        public static ComponentResourceKey Settings { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Settings));

        /// <summary>Lucide <c>shapes</c> icon.</summary>
        public static ComponentResourceKey Shapes { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Shapes));

        /// <summary>Lucide <c>share-2</c> icon.</summary>
        public static ComponentResourceKey Share2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Share2));

        /// <summary>Lucide <c>share</c> icon.</summary>
        public static ComponentResourceKey Share { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Share));

        /// <summary>Lucide <c>sheet</c> icon.</summary>
        public static ComponentResourceKey Sheet { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Sheet));

        /// <summary>Lucide <c>shell</c> icon.</summary>
        public static ComponentResourceKey Shell { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Shell));

        /// <summary>Lucide <c>shelving-unit</c> icon.</summary>
        public static ComponentResourceKey ShelvingUnit { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShelvingUnit));

        /// <summary>Lucide <c>shield-alert</c> icon.</summary>
        public static ComponentResourceKey ShieldAlert { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShieldAlert));

        /// <summary>Lucide <c>shield-ban</c> icon.</summary>
        public static ComponentResourceKey ShieldBan { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShieldBan));

        /// <summary>Lucide <c>shield-check</c> icon.</summary>
        public static ComponentResourceKey ShieldCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShieldCheck));

        /// <summary>Lucide <c>shield-cog-corner</c> icon.</summary>
        public static ComponentResourceKey ShieldCogCorner { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShieldCogCorner));

        /// <summary>Lucide <c>shield-cog</c> icon.</summary>
        public static ComponentResourceKey ShieldCog { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShieldCog));

        /// <summary>Lucide <c>shield-ellipsis</c> icon.</summary>
        public static ComponentResourceKey ShieldEllipsis { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShieldEllipsis));

        /// <summary>Lucide <c>shield-half</c> icon.</summary>
        public static ComponentResourceKey ShieldHalf { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShieldHalf));

        /// <summary>Lucide <c>shield-minus</c> icon.</summary>
        public static ComponentResourceKey ShieldMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShieldMinus));

        /// <summary>Lucide <c>shield-off</c> icon.</summary>
        public static ComponentResourceKey ShieldOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShieldOff));

        /// <summary>Lucide <c>shield-plus</c> icon.</summary>
        public static ComponentResourceKey ShieldPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShieldPlus));

        /// <summary>Lucide <c>shield-question-mark</c> icon.</summary>
        public static ComponentResourceKey ShieldQuestionMark { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShieldQuestionMark));

        /// <summary>Lucide <c>shield-user</c> icon.</summary>
        public static ComponentResourceKey ShieldUser { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShieldUser));

        /// <summary>Lucide <c>shield-x</c> icon.</summary>
        public static ComponentResourceKey ShieldX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShieldX));

        /// <summary>Lucide <c>shield</c> icon.</summary>
        public static ComponentResourceKey Shield { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Shield));

        /// <summary>Lucide <c>ship-wheel</c> icon.</summary>
        public static ComponentResourceKey ShipWheel { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShipWheel));

        /// <summary>Lucide <c>ship</c> icon.</summary>
        public static ComponentResourceKey Ship { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ship));

        /// <summary>Lucide <c>shirt</c> icon.</summary>
        public static ComponentResourceKey Shirt { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Shirt));

        /// <summary>Lucide <c>shopping-bag</c> icon.</summary>
        public static ComponentResourceKey ShoppingBag { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShoppingBag));

        /// <summary>Lucide <c>shopping-basket</c> icon.</summary>
        public static ComponentResourceKey ShoppingBasket { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShoppingBasket));

        /// <summary>Lucide <c>shopping-cart</c> icon.</summary>
        public static ComponentResourceKey ShoppingCart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShoppingCart));

        /// <summary>Lucide <c>shovel</c> icon.</summary>
        public static ComponentResourceKey Shovel { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Shovel));

        /// <summary>Lucide <c>shower-head</c> icon.</summary>
        public static ComponentResourceKey ShowerHead { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ShowerHead));

        /// <summary>Lucide <c>shredder</c> icon.</summary>
        public static ComponentResourceKey Shredder { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Shredder));

        /// <summary>Lucide <c>shrimp</c> icon.</summary>
        public static ComponentResourceKey Shrimp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Shrimp));

        /// <summary>Lucide <c>shrink</c> icon.</summary>
        public static ComponentResourceKey Shrink { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Shrink));

        /// <summary>Lucide <c>shrub</c> icon.</summary>
        public static ComponentResourceKey Shrub { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Shrub));

        /// <summary>Lucide <c>shuffle</c> icon.</summary>
        public static ComponentResourceKey Shuffle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Shuffle));

        /// <summary>Lucide <c>sigma</c> icon.</summary>
        public static ComponentResourceKey Sigma { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Sigma));

        /// <summary>Lucide <c>signal-high</c> icon.</summary>
        public static ComponentResourceKey SignalHigh { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SignalHigh));

        /// <summary>Lucide <c>signal-low</c> icon.</summary>
        public static ComponentResourceKey SignalLow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SignalLow));

        /// <summary>Lucide <c>signal-medium</c> icon.</summary>
        public static ComponentResourceKey SignalMedium { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SignalMedium));

        /// <summary>Lucide <c>signal-zero</c> icon.</summary>
        public static ComponentResourceKey SignalZero { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SignalZero));

        /// <summary>Lucide <c>signal</c> icon.</summary>
        public static ComponentResourceKey Signal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Signal));

        /// <summary>Lucide <c>signature</c> icon.</summary>
        public static ComponentResourceKey Signature { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Signature));

        /// <summary>Lucide <c>signpost-big</c> icon.</summary>
        public static ComponentResourceKey SignpostBig { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SignpostBig));

        /// <summary>Lucide <c>signpost</c> icon.</summary>
        public static ComponentResourceKey Signpost { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Signpost));

        /// <summary>Lucide <c>siren</c> icon.</summary>
        public static ComponentResourceKey Siren { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Siren));

        /// <summary>Lucide <c>skip-back</c> icon.</summary>
        public static ComponentResourceKey SkipBack { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SkipBack));

        /// <summary>Lucide <c>skip-forward</c> icon.</summary>
        public static ComponentResourceKey SkipForward { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SkipForward));

        /// <summary>Lucide <c>skull</c> icon.</summary>
        public static ComponentResourceKey Skull { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Skull));

        /// <summary>Lucide <c>slash</c> icon.</summary>
        public static ComponentResourceKey Slash { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Slash));

        /// <summary>Lucide <c>slice</c> icon.</summary>
        public static ComponentResourceKey Slice { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Slice));

        /// <summary>Lucide <c>sliders-horizontal</c> icon.</summary>
        public static ComponentResourceKey SlidersHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SlidersHorizontal));

        /// <summary>Lucide <c>sliders-vertical</c> icon.</summary>
        public static ComponentResourceKey SlidersVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SlidersVertical));

        /// <summary>Lucide <c>smartphone-charging</c> icon.</summary>
        public static ComponentResourceKey SmartphoneCharging { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SmartphoneCharging));

        /// <summary>Lucide <c>smartphone-nfc</c> icon.</summary>
        public static ComponentResourceKey SmartphoneNfc { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SmartphoneNfc));

        /// <summary>Lucide <c>smartphone</c> icon.</summary>
        public static ComponentResourceKey Smartphone { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Smartphone));

        /// <summary>Lucide <c>smile-plus</c> icon.</summary>
        public static ComponentResourceKey SmilePlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SmilePlus));

        /// <summary>Lucide <c>smile</c> icon.</summary>
        public static ComponentResourceKey Smile { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Smile));

        /// <summary>Lucide <c>snail</c> icon.</summary>
        public static ComponentResourceKey Snail { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Snail));

        /// <summary>Lucide <c>snowflake</c> icon.</summary>
        public static ComponentResourceKey Snowflake { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Snowflake));

        /// <summary>Lucide <c>soap-dispenser-droplet</c> icon.</summary>
        public static ComponentResourceKey SoapDispenserDroplet { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SoapDispenserDroplet));

        /// <summary>Lucide <c>sofa</c> icon.</summary>
        public static ComponentResourceKey Sofa { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Sofa));

        /// <summary>Lucide <c>solar-panel</c> icon.</summary>
        public static ComponentResourceKey SolarPanel { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SolarPanel));

        /// <summary>Lucide <c>soup</c> icon.</summary>
        public static ComponentResourceKey Soup { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Soup));

        /// <summary>Lucide <c>space</c> icon.</summary>
        public static ComponentResourceKey Space { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Space));

        /// <summary>Lucide <c>spade</c> icon.</summary>
        public static ComponentResourceKey Spade { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Spade));

        /// <summary>Lucide <c>sparkle</c> icon.</summary>
        public static ComponentResourceKey Sparkle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Sparkle));

        /// <summary>Lucide <c>sparkles</c> icon.</summary>
        public static ComponentResourceKey Sparkles { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Sparkles));

        /// <summary>Lucide <c>speaker</c> icon.</summary>
        public static ComponentResourceKey Speaker { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Speaker));

        /// <summary>Lucide <c>speech</c> icon.</summary>
        public static ComponentResourceKey Speech { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Speech));

        /// <summary>Lucide <c>spell-check-2</c> icon.</summary>
        public static ComponentResourceKey SpellCheck2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SpellCheck2));

        /// <summary>Lucide <c>spell-check</c> icon.</summary>
        public static ComponentResourceKey SpellCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SpellCheck));

        /// <summary>Lucide <c>spline-pointer</c> icon.</summary>
        public static ComponentResourceKey SplinePointer { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SplinePointer));

        /// <summary>Lucide <c>spline</c> icon.</summary>
        public static ComponentResourceKey Spline { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Spline));

        /// <summary>Lucide <c>split</c> icon.</summary>
        public static ComponentResourceKey Split { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Split));

        /// <summary>Lucide <c>spool</c> icon.</summary>
        public static ComponentResourceKey Spool { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Spool));

        /// <summary>Lucide <c>sport-shoe</c> icon.</summary>
        public static ComponentResourceKey SportShoe { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SportShoe));

        /// <summary>Lucide <c>spotlight</c> icon.</summary>
        public static ComponentResourceKey Spotlight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Spotlight));

        /// <summary>Lucide <c>spray-can</c> icon.</summary>
        public static ComponentResourceKey SprayCan { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SprayCan));

        /// <summary>Lucide <c>sprout</c> icon.</summary>
        public static ComponentResourceKey Sprout { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Sprout));

        /// <summary>Lucide <c>square-activity</c> icon.</summary>
        public static ComponentResourceKey SquareActivity { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareActivity));

        /// <summary>Lucide <c>square-arrow-down-left</c> icon.</summary>
        public static ComponentResourceKey SquareArrowDownLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareArrowDownLeft));

        /// <summary>Lucide <c>square-arrow-down-right</c> icon.</summary>
        public static ComponentResourceKey SquareArrowDownRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareArrowDownRight));

        /// <summary>Lucide <c>square-arrow-down</c> icon.</summary>
        public static ComponentResourceKey SquareArrowDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareArrowDown));

        /// <summary>Lucide <c>square-arrow-left</c> icon.</summary>
        public static ComponentResourceKey SquareArrowLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareArrowLeft));

        /// <summary>Lucide <c>square-arrow-out-down-left</c> icon.</summary>
        public static ComponentResourceKey SquareArrowOutDownLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareArrowOutDownLeft));

        /// <summary>Lucide <c>square-arrow-out-down-right</c> icon.</summary>
        public static ComponentResourceKey SquareArrowOutDownRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareArrowOutDownRight));

        /// <summary>Lucide <c>square-arrow-out-up-left</c> icon.</summary>
        public static ComponentResourceKey SquareArrowOutUpLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareArrowOutUpLeft));

        /// <summary>Lucide <c>square-arrow-out-up-right</c> icon.</summary>
        public static ComponentResourceKey SquareArrowOutUpRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareArrowOutUpRight));

        /// <summary>Lucide <c>square-arrow-right-enter</c> icon.</summary>
        public static ComponentResourceKey SquareArrowRightEnter { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareArrowRightEnter));

        /// <summary>Lucide <c>square-arrow-right-exit</c> icon.</summary>
        public static ComponentResourceKey SquareArrowRightExit { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareArrowRightExit));

        /// <summary>Lucide <c>square-arrow-right</c> icon.</summary>
        public static ComponentResourceKey SquareArrowRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareArrowRight));

        /// <summary>Lucide <c>square-arrow-up-left</c> icon.</summary>
        public static ComponentResourceKey SquareArrowUpLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareArrowUpLeft));

        /// <summary>Lucide <c>square-arrow-up-right</c> icon.</summary>
        public static ComponentResourceKey SquareArrowUpRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareArrowUpRight));

        /// <summary>Lucide <c>square-arrow-up</c> icon.</summary>
        public static ComponentResourceKey SquareArrowUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareArrowUp));

        /// <summary>Lucide <c>square-asterisk</c> icon.</summary>
        public static ComponentResourceKey SquareAsterisk { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareAsterisk));

        /// <summary>Lucide <c>square-bottom-dashed-scissors</c> icon.</summary>
        public static ComponentResourceKey SquareBottomDashedScissors { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareBottomDashedScissors));

        /// <summary>Lucide <c>square-centerline-dashed-horizontal</c> icon.</summary>
        public static ComponentResourceKey SquareCenterlineDashedHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareCenterlineDashedHorizontal));

        /// <summary>Lucide <c>square-centerline-dashed-vertical</c> icon.</summary>
        public static ComponentResourceKey SquareCenterlineDashedVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareCenterlineDashedVertical));

        /// <summary>Lucide <c>square-chart-gantt</c> icon.</summary>
        public static ComponentResourceKey SquareChartGantt { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareChartGantt));

        /// <summary>Lucide <c>square-check-big</c> icon.</summary>
        public static ComponentResourceKey SquareCheckBig { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareCheckBig));

        /// <summary>Lucide <c>square-check</c> icon.</summary>
        public static ComponentResourceKey SquareCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareCheck));

        /// <summary>Lucide <c>square-chevron-down</c> icon.</summary>
        public static ComponentResourceKey SquareChevronDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareChevronDown));

        /// <summary>Lucide <c>square-chevron-left</c> icon.</summary>
        public static ComponentResourceKey SquareChevronLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareChevronLeft));

        /// <summary>Lucide <c>square-chevron-right</c> icon.</summary>
        public static ComponentResourceKey SquareChevronRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareChevronRight));

        /// <summary>Lucide <c>square-chevron-up</c> icon.</summary>
        public static ComponentResourceKey SquareChevronUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareChevronUp));

        /// <summary>Lucide <c>square-code</c> icon.</summary>
        public static ComponentResourceKey SquareCode { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareCode));

        /// <summary>Lucide <c>square-dashed-bottom-code</c> icon.</summary>
        public static ComponentResourceKey SquareDashedBottomCode { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareDashedBottomCode));

        /// <summary>Lucide <c>square-dashed-bottom</c> icon.</summary>
        public static ComponentResourceKey SquareDashedBottom { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareDashedBottom));

        /// <summary>Lucide <c>square-dashed-kanban</c> icon.</summary>
        public static ComponentResourceKey SquareDashedKanban { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareDashedKanban));

        /// <summary>Lucide <c>square-dashed-mouse-pointer</c> icon.</summary>
        public static ComponentResourceKey SquareDashedMousePointer { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareDashedMousePointer));

        /// <summary>Lucide <c>square-dashed-text</c> icon.</summary>
        public static ComponentResourceKey SquareDashedText { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareDashedText));

        /// <summary>Lucide <c>square-dashed-top-solid</c> icon.</summary>
        public static ComponentResourceKey SquareDashedTopSolid { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareDashedTopSolid));

        /// <summary>Lucide <c>square-dashed</c> icon.</summary>
        public static ComponentResourceKey SquareDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareDashed));

        /// <summary>Lucide <c>square-divide</c> icon.</summary>
        public static ComponentResourceKey SquareDivide { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareDivide));

        /// <summary>Lucide <c>square-dot</c> icon.</summary>
        public static ComponentResourceKey SquareDot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareDot));

        /// <summary>Lucide <c>square-equal</c> icon.</summary>
        public static ComponentResourceKey SquareEqual { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareEqual));

        /// <summary>Lucide <c>square-function</c> icon.</summary>
        public static ComponentResourceKey SquareFunction { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareFunction));

        /// <summary>Lucide <c>square-kanban</c> icon.</summary>
        public static ComponentResourceKey SquareKanban { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareKanban));

        /// <summary>Lucide <c>square-library</c> icon.</summary>
        public static ComponentResourceKey SquareLibrary { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareLibrary));

        /// <summary>Lucide <c>square-m</c> icon.</summary>
        public static ComponentResourceKey SquareM { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareM));

        /// <summary>Lucide <c>square-menu</c> icon.</summary>
        public static ComponentResourceKey SquareMenu { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareMenu));

        /// <summary>Lucide <c>square-minus</c> icon.</summary>
        public static ComponentResourceKey SquareMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareMinus));

        /// <summary>Lucide <c>square-mouse-pointer</c> icon.</summary>
        public static ComponentResourceKey SquareMousePointer { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareMousePointer));

        /// <summary>Lucide <c>square-parking-off</c> icon.</summary>
        public static ComponentResourceKey SquareParkingOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareParkingOff));

        /// <summary>Lucide <c>square-parking</c> icon.</summary>
        public static ComponentResourceKey SquareParking { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareParking));

        /// <summary>Lucide <c>square-pause</c> icon.</summary>
        public static ComponentResourceKey SquarePause { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquarePause));

        /// <summary>Lucide <c>square-pen</c> icon.</summary>
        public static ComponentResourceKey SquarePen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquarePen));

        /// <summary>Lucide <c>square-percent</c> icon.</summary>
        public static ComponentResourceKey SquarePercent { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquarePercent));

        /// <summary>Lucide <c>square-pi</c> icon.</summary>
        public static ComponentResourceKey SquarePi { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquarePi));

        /// <summary>Lucide <c>square-pilcrow</c> icon.</summary>
        public static ComponentResourceKey SquarePilcrow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquarePilcrow));

        /// <summary>Lucide <c>square-play</c> icon.</summary>
        public static ComponentResourceKey SquarePlay { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquarePlay));

        /// <summary>Lucide <c>square-plus</c> icon.</summary>
        public static ComponentResourceKey SquarePlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquarePlus));

        /// <summary>Lucide <c>square-power</c> icon.</summary>
        public static ComponentResourceKey SquarePower { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquarePower));

        /// <summary>Lucide <c>square-radical</c> icon.</summary>
        public static ComponentResourceKey SquareRadical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareRadical));

        /// <summary>Lucide <c>square-round-corner</c> icon.</summary>
        public static ComponentResourceKey SquareRoundCorner { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareRoundCorner));

        /// <summary>Lucide <c>square-scissors</c> icon.</summary>
        public static ComponentResourceKey SquareScissors { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareScissors));

        /// <summary>Lucide <c>square-sigma</c> icon.</summary>
        public static ComponentResourceKey SquareSigma { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareSigma));

        /// <summary>Lucide <c>square-slash</c> icon.</summary>
        public static ComponentResourceKey SquareSlash { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareSlash));

        /// <summary>Lucide <c>square-split-horizontal</c> icon.</summary>
        public static ComponentResourceKey SquareSplitHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareSplitHorizontal));

        /// <summary>Lucide <c>square-split-vertical</c> icon.</summary>
        public static ComponentResourceKey SquareSplitVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareSplitVertical));

        /// <summary>Lucide <c>square-square</c> icon.</summary>
        public static ComponentResourceKey SquareSquare { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareSquare));

        /// <summary>Lucide <c>square-stack</c> icon.</summary>
        public static ComponentResourceKey SquareStack { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareStack));

        /// <summary>Lucide <c>square-star</c> icon.</summary>
        public static ComponentResourceKey SquareStar { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareStar));

        /// <summary>Lucide <c>square-stop</c> icon.</summary>
        public static ComponentResourceKey SquareStop { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareStop));

        /// <summary>Lucide <c>square-terminal</c> icon.</summary>
        public static ComponentResourceKey SquareTerminal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareTerminal));

        /// <summary>Lucide <c>square-user-round</c> icon.</summary>
        public static ComponentResourceKey SquareUserRound { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareUserRound));

        /// <summary>Lucide <c>square-user</c> icon.</summary>
        public static ComponentResourceKey SquareUser { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareUser));

        /// <summary>Lucide <c>square-x</c> icon.</summary>
        public static ComponentResourceKey SquareX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquareX));

        /// <summary>Lucide <c>square</c> icon.</summary>
        public static ComponentResourceKey Square { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Square));

        /// <summary>Lucide <c>squares-exclude</c> icon.</summary>
        public static ComponentResourceKey SquaresExclude { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquaresExclude));

        /// <summary>Lucide <c>squares-intersect</c> icon.</summary>
        public static ComponentResourceKey SquaresIntersect { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquaresIntersect));

        /// <summary>Lucide <c>squares-subtract</c> icon.</summary>
        public static ComponentResourceKey SquaresSubtract { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquaresSubtract));

        /// <summary>Lucide <c>squares-unite</c> icon.</summary>
        public static ComponentResourceKey SquaresUnite { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquaresUnite));

        /// <summary>Lucide <c>squircle-dashed</c> icon.</summary>
        public static ComponentResourceKey SquircleDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SquircleDashed));

        /// <summary>Lucide <c>squircle</c> icon.</summary>
        public static ComponentResourceKey Squircle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Squircle));

        /// <summary>Lucide <c>squirrel</c> icon.</summary>
        public static ComponentResourceKey Squirrel { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Squirrel));

        /// <summary>Lucide <c>stamp</c> icon.</summary>
        public static ComponentResourceKey Stamp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Stamp));

        /// <summary>Lucide <c>star-check</c> icon.</summary>
        public static ComponentResourceKey StarCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StarCheck));

        /// <summary>Lucide <c>star-half</c> icon.</summary>
        public static ComponentResourceKey StarHalf { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StarHalf));

        /// <summary>Lucide <c>star-minus</c> icon.</summary>
        public static ComponentResourceKey StarMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StarMinus));

        /// <summary>Lucide <c>star-off</c> icon.</summary>
        public static ComponentResourceKey StarOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StarOff));

        /// <summary>Lucide <c>star-plus</c> icon.</summary>
        public static ComponentResourceKey StarPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StarPlus));

        /// <summary>Lucide <c>star-x</c> icon.</summary>
        public static ComponentResourceKey StarX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StarX));

        /// <summary>Lucide <c>star</c> icon.</summary>
        public static ComponentResourceKey Star { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Star));

        /// <summary>Lucide <c>step-back</c> icon.</summary>
        public static ComponentResourceKey StepBack { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StepBack));

        /// <summary>Lucide <c>step-forward</c> icon.</summary>
        public static ComponentResourceKey StepForward { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StepForward));

        /// <summary>Lucide <c>stethoscope</c> icon.</summary>
        public static ComponentResourceKey Stethoscope { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Stethoscope));

        /// <summary>Lucide <c>sticker</c> icon.</summary>
        public static ComponentResourceKey Sticker { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Sticker));

        /// <summary>Lucide <c>sticky-note-check</c> icon.</summary>
        public static ComponentResourceKey StickyNoteCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StickyNoteCheck));

        /// <summary>Lucide <c>sticky-note-minus</c> icon.</summary>
        public static ComponentResourceKey StickyNoteMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StickyNoteMinus));

        /// <summary>Lucide <c>sticky-note-off</c> icon.</summary>
        public static ComponentResourceKey StickyNoteOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StickyNoteOff));

        /// <summary>Lucide <c>sticky-note-plus</c> icon.</summary>
        public static ComponentResourceKey StickyNotePlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StickyNotePlus));

        /// <summary>Lucide <c>sticky-note-x</c> icon.</summary>
        public static ComponentResourceKey StickyNoteX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StickyNoteX));

        /// <summary>Lucide <c>sticky-note</c> icon.</summary>
        public static ComponentResourceKey StickyNote { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StickyNote));

        /// <summary>Lucide <c>sticky-notes</c> icon.</summary>
        public static ComponentResourceKey StickyNotes { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StickyNotes));

        /// <summary>Lucide <c>stone</c> icon.</summary>
        public static ComponentResourceKey Stone { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Stone));

        /// <summary>Lucide <c>store</c> icon.</summary>
        public static ComponentResourceKey Store { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Store));

        /// <summary>Lucide <c>stretch-horizontal</c> icon.</summary>
        public static ComponentResourceKey StretchHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StretchHorizontal));

        /// <summary>Lucide <c>stretch-vertical</c> icon.</summary>
        public static ComponentResourceKey StretchVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(StretchVertical));

        /// <summary>Lucide <c>strikethrough</c> icon.</summary>
        public static ComponentResourceKey Strikethrough { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Strikethrough));

        /// <summary>Lucide <c>subscript</c> icon.</summary>
        public static ComponentResourceKey Subscript { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Subscript));

        /// <summary>Lucide <c>summary</c> icon.</summary>
        public static ComponentResourceKey Summary { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Summary));

        /// <summary>Lucide <c>sun-dim</c> icon.</summary>
        public static ComponentResourceKey SunDim { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SunDim));

        /// <summary>Lucide <c>sun-medium</c> icon.</summary>
        public static ComponentResourceKey SunMedium { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SunMedium));

        /// <summary>Lucide <c>sun-moon</c> icon.</summary>
        public static ComponentResourceKey SunMoon { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SunMoon));

        /// <summary>Lucide <c>sun-snow</c> icon.</summary>
        public static ComponentResourceKey SunSnow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SunSnow));

        /// <summary>Lucide <c>sun</c> icon.</summary>
        public static ComponentResourceKey Sun { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Sun));

        /// <summary>Lucide <c>sunrise</c> icon.</summary>
        public static ComponentResourceKey Sunrise { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Sunrise));

        /// <summary>Lucide <c>sunset</c> icon.</summary>
        public static ComponentResourceKey Sunset { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Sunset));

        /// <summary>Lucide <c>superscript</c> icon.</summary>
        public static ComponentResourceKey Superscript { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Superscript));

        /// <summary>Lucide <c>swatch-book</c> icon.</summary>
        public static ComponentResourceKey SwatchBook { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SwatchBook));

        /// <summary>Lucide <c>swiss-franc</c> icon.</summary>
        public static ComponentResourceKey SwissFranc { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SwissFranc));

        /// <summary>Lucide <c>switch-camera</c> icon.</summary>
        public static ComponentResourceKey SwitchCamera { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(SwitchCamera));

        /// <summary>Lucide <c>sword</c> icon.</summary>
        public static ComponentResourceKey Sword { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Sword));

        /// <summary>Lucide <c>swords</c> icon.</summary>
        public static ComponentResourceKey Swords { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Swords));

        /// <summary>Lucide <c>syringe</c> icon.</summary>
        public static ComponentResourceKey Syringe { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Syringe));

        // ─── T ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>table-2</c> icon.</summary>
        public static ComponentResourceKey Table2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Table2));

        /// <summary>Lucide <c>table-cells-merge</c> icon.</summary>
        public static ComponentResourceKey TableCellsMerge { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TableCellsMerge));

        /// <summary>Lucide <c>table-cells-split</c> icon.</summary>
        public static ComponentResourceKey TableCellsSplit { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TableCellsSplit));

        /// <summary>Lucide <c>table-columns-split</c> icon.</summary>
        public static ComponentResourceKey TableColumnsSplit { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TableColumnsSplit));

        /// <summary>Lucide <c>table-of-contents</c> icon.</summary>
        public static ComponentResourceKey TableOfContents { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TableOfContents));

        /// <summary>Lucide <c>table-properties</c> icon.</summary>
        public static ComponentResourceKey TableProperties { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TableProperties));

        /// <summary>Lucide <c>table-rows-split</c> icon.</summary>
        public static ComponentResourceKey TableRowsSplit { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TableRowsSplit));

        /// <summary>Lucide <c>table</c> icon.</summary>
        public static ComponentResourceKey Table { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Table));

        /// <summary>Lucide <c>tablet-smartphone</c> icon.</summary>
        public static ComponentResourceKey TabletSmartphone { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TabletSmartphone));

        /// <summary>Lucide <c>tablet</c> icon.</summary>
        public static ComponentResourceKey Tablet { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Tablet));

        /// <summary>Lucide <c>tablets</c> icon.</summary>
        public static ComponentResourceKey Tablets { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Tablets));

        /// <summary>Lucide <c>tag-plus</c> icon.</summary>
        public static ComponentResourceKey TagPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TagPlus));

        /// <summary>Lucide <c>tag-x</c> icon.</summary>
        public static ComponentResourceKey TagX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TagX));

        /// <summary>Lucide <c>tag</c> icon.</summary>
        public static ComponentResourceKey Tag { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Tag));

        /// <summary>Lucide <c>tags</c> icon.</summary>
        public static ComponentResourceKey Tags { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Tags));

        /// <summary>Lucide <c>tally-1</c> icon.</summary>
        public static ComponentResourceKey Tally1 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Tally1));

        /// <summary>Lucide <c>tally-2</c> icon.</summary>
        public static ComponentResourceKey Tally2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Tally2));

        /// <summary>Lucide <c>tally-3</c> icon.</summary>
        public static ComponentResourceKey Tally3 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Tally3));

        /// <summary>Lucide <c>tally-4</c> icon.</summary>
        public static ComponentResourceKey Tally4 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Tally4));

        /// <summary>Lucide <c>tally-5</c> icon.</summary>
        public static ComponentResourceKey Tally5 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Tally5));

        /// <summary>Lucide <c>tangent</c> icon.</summary>
        public static ComponentResourceKey Tangent { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Tangent));

        /// <summary>Lucide <c>target</c> icon.</summary>
        public static ComponentResourceKey Target { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Target));

        /// <summary>Lucide <c>telescope</c> icon.</summary>
        public static ComponentResourceKey Telescope { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Telescope));

        /// <summary>Lucide <c>tent-tree</c> icon.</summary>
        public static ComponentResourceKey TentTree { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TentTree));

        /// <summary>Lucide <c>tent</c> icon.</summary>
        public static ComponentResourceKey Tent { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Tent));

        /// <summary>Lucide <c>terminal</c> icon.</summary>
        public static ComponentResourceKey Terminal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Terminal));

        /// <summary>Lucide <c>test-tube-diagonal</c> icon.</summary>
        public static ComponentResourceKey TestTubeDiagonal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TestTubeDiagonal));

        /// <summary>Lucide <c>test-tube</c> icon.</summary>
        public static ComponentResourceKey TestTube { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TestTube));

        /// <summary>Lucide <c>test-tubes</c> icon.</summary>
        public static ComponentResourceKey TestTubes { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TestTubes));

        /// <summary>Lucide <c>text-align-center</c> icon.</summary>
        public static ComponentResourceKey TextAlignCenter { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TextAlignCenter));

        /// <summary>Lucide <c>text-align-end</c> icon.</summary>
        public static ComponentResourceKey TextAlignEnd { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TextAlignEnd));

        /// <summary>Lucide <c>text-align-justify</c> icon.</summary>
        public static ComponentResourceKey TextAlignJustify { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TextAlignJustify));

        /// <summary>Lucide <c>text-align-start</c> icon.</summary>
        public static ComponentResourceKey TextAlignStart { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TextAlignStart));

        /// <summary>Lucide <c>text-cursor-input</c> icon.</summary>
        public static ComponentResourceKey TextCursorInput { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TextCursorInput));

        /// <summary>Lucide <c>text-cursor</c> icon.</summary>
        public static ComponentResourceKey TextCursor { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TextCursor));

        /// <summary>Lucide <c>text-initial</c> icon.</summary>
        public static ComponentResourceKey TextInitial { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TextInitial));

        /// <summary>Lucide <c>text-quote</c> icon.</summary>
        public static ComponentResourceKey TextQuote { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TextQuote));

        /// <summary>Lucide <c>text-search</c> icon.</summary>
        public static ComponentResourceKey TextSearch { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TextSearch));

        /// <summary>Lucide <c>text-wrap</c> icon.</summary>
        public static ComponentResourceKey TextWrap { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TextWrap));

        /// <summary>Lucide <c>theater</c> icon.</summary>
        public static ComponentResourceKey Theater { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Theater));

        /// <summary>Lucide <c>thermometer-snowflake</c> icon.</summary>
        public static ComponentResourceKey ThermometerSnowflake { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ThermometerSnowflake));

        /// <summary>Lucide <c>thermometer-sun</c> icon.</summary>
        public static ComponentResourceKey ThermometerSun { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ThermometerSun));

        /// <summary>Lucide <c>thermometer</c> icon.</summary>
        public static ComponentResourceKey Thermometer { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Thermometer));

        /// <summary>Lucide <c>thumbs-down</c> icon.</summary>
        public static ComponentResourceKey ThumbsDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ThumbsDown));

        /// <summary>Lucide <c>thumbs-up</c> icon.</summary>
        public static ComponentResourceKey ThumbsUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ThumbsUp));

        /// <summary>Lucide <c>ticket-check</c> icon.</summary>
        public static ComponentResourceKey TicketCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TicketCheck));

        /// <summary>Lucide <c>ticket-minus</c> icon.</summary>
        public static ComponentResourceKey TicketMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TicketMinus));

        /// <summary>Lucide <c>ticket-percent</c> icon.</summary>
        public static ComponentResourceKey TicketPercent { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TicketPercent));

        /// <summary>Lucide <c>ticket-plus</c> icon.</summary>
        public static ComponentResourceKey TicketPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TicketPlus));

        /// <summary>Lucide <c>ticket-slash</c> icon.</summary>
        public static ComponentResourceKey TicketSlash { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TicketSlash));

        /// <summary>Lucide <c>ticket-x</c> icon.</summary>
        public static ComponentResourceKey TicketX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TicketX));

        /// <summary>Lucide <c>ticket</c> icon.</summary>
        public static ComponentResourceKey Ticket { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ticket));

        /// <summary>Lucide <c>tickets-plane</c> icon.</summary>
        public static ComponentResourceKey TicketsPlane { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TicketsPlane));

        /// <summary>Lucide <c>tickets</c> icon.</summary>
        public static ComponentResourceKey Tickets { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Tickets));

        /// <summary>Lucide <c>timeline</c> icon.</summary>
        public static ComponentResourceKey Timeline { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Timeline));

        /// <summary>Lucide <c>timer-off</c> icon.</summary>
        public static ComponentResourceKey TimerOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TimerOff));

        /// <summary>Lucide <c>timer-reset</c> icon.</summary>
        public static ComponentResourceKey TimerReset { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TimerReset));

        /// <summary>Lucide <c>timer</c> icon.</summary>
        public static ComponentResourceKey Timer { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Timer));

        /// <summary>Lucide <c>toggle-left</c> icon.</summary>
        public static ComponentResourceKey ToggleLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ToggleLeft));

        /// <summary>Lucide <c>toggle-right</c> icon.</summary>
        public static ComponentResourceKey ToggleRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ToggleRight));

        /// <summary>Lucide <c>toilet</c> icon.</summary>
        public static ComponentResourceKey Toilet { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Toilet));

        /// <summary>Lucide <c>tool-case</c> icon.</summary>
        public static ComponentResourceKey ToolCase { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ToolCase));

        /// <summary>Lucide <c>toolbox</c> icon.</summary>
        public static ComponentResourceKey Toolbox { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Toolbox));

        /// <summary>Lucide <c>tornado</c> icon.</summary>
        public static ComponentResourceKey Tornado { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Tornado));

        /// <summary>Lucide <c>torus</c> icon.</summary>
        public static ComponentResourceKey Torus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Torus));

        /// <summary>Lucide <c>touchpad-off</c> icon.</summary>
        public static ComponentResourceKey TouchpadOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TouchpadOff));

        /// <summary>Lucide <c>touchpad</c> icon.</summary>
        public static ComponentResourceKey Touchpad { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Touchpad));

        /// <summary>Lucide <c>towel-rack</c> icon.</summary>
        public static ComponentResourceKey TowelRack { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TowelRack));

        /// <summary>Lucide <c>tower-control</c> icon.</summary>
        public static ComponentResourceKey TowerControl { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TowerControl));

        /// <summary>Lucide <c>toy-brick</c> icon.</summary>
        public static ComponentResourceKey ToyBrick { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ToyBrick));

        /// <summary>Lucide <c>tractor</c> icon.</summary>
        public static ComponentResourceKey Tractor { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Tractor));

        /// <summary>Lucide <c>traffic-cone</c> icon.</summary>
        public static ComponentResourceKey TrafficCone { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TrafficCone));

        /// <summary>Lucide <c>train-front-tunnel</c> icon.</summary>
        public static ComponentResourceKey TrainFrontTunnel { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TrainFrontTunnel));

        /// <summary>Lucide <c>train-front</c> icon.</summary>
        public static ComponentResourceKey TrainFront { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TrainFront));

        /// <summary>Lucide <c>train-track</c> icon.</summary>
        public static ComponentResourceKey TrainTrack { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TrainTrack));

        /// <summary>Lucide <c>tram-front</c> icon.</summary>
        public static ComponentResourceKey TramFront { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TramFront));

        /// <summary>Lucide <c>transgender</c> icon.</summary>
        public static ComponentResourceKey Transgender { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Transgender));

        /// <summary>Lucide <c>trash-2</c> icon.</summary>
        public static ComponentResourceKey Trash2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Trash2));

        /// <summary>Lucide <c>trash</c> icon.</summary>
        public static ComponentResourceKey Trash { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Trash));

        /// <summary>Lucide <c>tree-deciduous</c> icon.</summary>
        public static ComponentResourceKey TreeDeciduous { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TreeDeciduous));

        /// <summary>Lucide <c>tree-palm</c> icon.</summary>
        public static ComponentResourceKey TreePalm { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TreePalm));

        /// <summary>Lucide <c>tree-pine</c> icon.</summary>
        public static ComponentResourceKey TreePine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TreePine));

        /// <summary>Lucide <c>trees</c> icon.</summary>
        public static ComponentResourceKey Trees { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Trees));

        /// <summary>Lucide <c>trending-down</c> icon.</summary>
        public static ComponentResourceKey TrendingDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TrendingDown));

        /// <summary>Lucide <c>trending-up-down</c> icon.</summary>
        public static ComponentResourceKey TrendingUpDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TrendingUpDown));

        /// <summary>Lucide <c>trending-up</c> icon.</summary>
        public static ComponentResourceKey TrendingUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TrendingUp));

        /// <summary>Lucide <c>triangle-alert</c> icon.</summary>
        public static ComponentResourceKey TriangleAlert { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TriangleAlert));

        /// <summary>Lucide <c>triangle-dashed</c> icon.</summary>
        public static ComponentResourceKey TriangleDashed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TriangleDashed));

        /// <summary>Lucide <c>triangle-right</c> icon.</summary>
        public static ComponentResourceKey TriangleRight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TriangleRight));

        /// <summary>Lucide <c>triangle</c> icon.</summary>
        public static ComponentResourceKey Triangle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Triangle));

        /// <summary>Lucide <c>trophy</c> icon.</summary>
        public static ComponentResourceKey Trophy { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Trophy));

        /// <summary>Lucide <c>truck-electric</c> icon.</summary>
        public static ComponentResourceKey TruckElectric { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TruckElectric));

        /// <summary>Lucide <c>truck</c> icon.</summary>
        public static ComponentResourceKey Truck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Truck));

        /// <summary>Lucide <c>turkish-lira</c> icon.</summary>
        public static ComponentResourceKey TurkishLira { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TurkishLira));

        /// <summary>Lucide <c>turntable</c> icon.</summary>
        public static ComponentResourceKey Turntable { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Turntable));

        /// <summary>Lucide <c>turtle</c> icon.</summary>
        public static ComponentResourceKey Turtle { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Turtle));

        /// <summary>Lucide <c>tv-minimal-play</c> icon.</summary>
        public static ComponentResourceKey TvMinimalPlay { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TvMinimalPlay));

        /// <summary>Lucide <c>tv-minimal</c> icon.</summary>
        public static ComponentResourceKey TvMinimal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TvMinimal));

        /// <summary>Lucide <c>tv</c> icon.</summary>
        public static ComponentResourceKey Tv { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Tv));

        /// <summary>Lucide <c>type-outline</c> icon.</summary>
        public static ComponentResourceKey TypeOutline { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(TypeOutline));

        /// <summary>Lucide <c>type</c> icon.</summary>
        public static ComponentResourceKey Type { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Type));

        // ─── U ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>umbrella-off</c> icon.</summary>
        public static ComponentResourceKey UmbrellaOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UmbrellaOff));

        /// <summary>Lucide <c>umbrella</c> icon.</summary>
        public static ComponentResourceKey Umbrella { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Umbrella));

        /// <summary>Lucide <c>underline</c> icon.</summary>
        public static ComponentResourceKey Underline { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Underline));

        /// <summary>Lucide <c>undo-2</c> icon.</summary>
        public static ComponentResourceKey Undo2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Undo2));

        /// <summary>Lucide <c>undo-dot</c> icon.</summary>
        public static ComponentResourceKey UndoDot { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UndoDot));

        /// <summary>Lucide <c>undo</c> icon.</summary>
        public static ComponentResourceKey Undo { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Undo));

        /// <summary>Lucide <c>unfold-horizontal</c> icon.</summary>
        public static ComponentResourceKey UnfoldHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UnfoldHorizontal));

        /// <summary>Lucide <c>unfold-vertical</c> icon.</summary>
        public static ComponentResourceKey UnfoldVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UnfoldVertical));

        /// <summary>Lucide <c>ungroup</c> icon.</summary>
        public static ComponentResourceKey Ungroup { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Ungroup));

        /// <summary>Lucide <c>university</c> icon.</summary>
        public static ComponentResourceKey University { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(University));

        /// <summary>Lucide <c>unlink-2</c> icon.</summary>
        public static ComponentResourceKey Unlink2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Unlink2));

        /// <summary>Lucide <c>unlink</c> icon.</summary>
        public static ComponentResourceKey Unlink { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Unlink));

        /// <summary>Lucide <c>unplug</c> icon.</summary>
        public static ComponentResourceKey Unplug { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Unplug));

        /// <summary>Lucide <c>upload</c> icon.</summary>
        public static ComponentResourceKey Upload { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Upload));

        /// <summary>Lucide <c>usb</c> icon.</summary>
        public static ComponentResourceKey Usb { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Usb));

        /// <summary>Lucide <c>user-check</c> icon.</summary>
        public static ComponentResourceKey UserCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserCheck));

        /// <summary>Lucide <c>user-cog</c> icon.</summary>
        public static ComponentResourceKey UserCog { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserCog));

        /// <summary>Lucide <c>user-key</c> icon.</summary>
        public static ComponentResourceKey UserKey { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserKey));

        /// <summary>Lucide <c>user-lock</c> icon.</summary>
        public static ComponentResourceKey UserLock { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserLock));

        /// <summary>Lucide <c>user-minus</c> icon.</summary>
        public static ComponentResourceKey UserMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserMinus));

        /// <summary>Lucide <c>user-pen</c> icon.</summary>
        public static ComponentResourceKey UserPen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserPen));

        /// <summary>Lucide <c>user-plus</c> icon.</summary>
        public static ComponentResourceKey UserPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserPlus));

        /// <summary>Lucide <c>user-round-arrow-left</c> icon.</summary>
        public static ComponentResourceKey UserRoundArrowLeft { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserRoundArrowLeft));

        /// <summary>Lucide <c>user-round-check</c> icon.</summary>
        public static ComponentResourceKey UserRoundCheck { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserRoundCheck));

        /// <summary>Lucide <c>user-round-cog</c> icon.</summary>
        public static ComponentResourceKey UserRoundCog { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserRoundCog));

        /// <summary>Lucide <c>user-round-key</c> icon.</summary>
        public static ComponentResourceKey UserRoundKey { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserRoundKey));

        /// <summary>Lucide <c>user-round-minus</c> icon.</summary>
        public static ComponentResourceKey UserRoundMinus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserRoundMinus));

        /// <summary>Lucide <c>user-round-pen</c> icon.</summary>
        public static ComponentResourceKey UserRoundPen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserRoundPen));

        /// <summary>Lucide <c>user-round-plus</c> icon.</summary>
        public static ComponentResourceKey UserRoundPlus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserRoundPlus));

        /// <summary>Lucide <c>user-round-search</c> icon.</summary>
        public static ComponentResourceKey UserRoundSearch { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserRoundSearch));

        /// <summary>Lucide <c>user-round-x</c> icon.</summary>
        public static ComponentResourceKey UserRoundX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserRoundX));

        /// <summary>Lucide <c>user-round</c> icon.</summary>
        public static ComponentResourceKey UserRound { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserRound));

        /// <summary>Lucide <c>user-search</c> icon.</summary>
        public static ComponentResourceKey UserSearch { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserSearch));

        /// <summary>Lucide <c>user-star</c> icon.</summary>
        public static ComponentResourceKey UserStar { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserStar));

        /// <summary>Lucide <c>user-x</c> icon.</summary>
        public static ComponentResourceKey UserX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UserX));

        /// <summary>Lucide <c>user</c> icon.</summary>
        public static ComponentResourceKey User { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(User));

        /// <summary>Lucide <c>users-round</c> icon.</summary>
        public static ComponentResourceKey UsersRound { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UsersRound));

        /// <summary>Lucide <c>users</c> icon.</summary>
        public static ComponentResourceKey Users { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Users));

        /// <summary>Lucide <c>utensils-crossed</c> icon.</summary>
        public static ComponentResourceKey UtensilsCrossed { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UtensilsCrossed));

        /// <summary>Lucide <c>utensils</c> icon.</summary>
        public static ComponentResourceKey Utensils { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Utensils));

        /// <summary>Lucide <c>utility-pole</c> icon.</summary>
        public static ComponentResourceKey UtilityPole { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(UtilityPole));

        // ─── V ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>van</c> icon.</summary>
        public static ComponentResourceKey Van { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Van));

        /// <summary>Lucide <c>variable</c> icon.</summary>
        public static ComponentResourceKey Variable { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Variable));

        /// <summary>Lucide <c>vault</c> icon.</summary>
        public static ComponentResourceKey Vault { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Vault));

        /// <summary>Lucide <c>vector-square</c> icon.</summary>
        public static ComponentResourceKey VectorSquare { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(VectorSquare));

        /// <summary>Lucide <c>vegan</c> icon.</summary>
        public static ComponentResourceKey Vegan { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Vegan));

        /// <summary>Lucide <c>venetian-mask</c> icon.</summary>
        public static ComponentResourceKey VenetianMask { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(VenetianMask));

        /// <summary>Lucide <c>venus-and-mars</c> icon.</summary>
        public static ComponentResourceKey VenusAndMars { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(VenusAndMars));

        /// <summary>Lucide <c>venus</c> icon.</summary>
        public static ComponentResourceKey Venus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Venus));

        /// <summary>Lucide <c>vibrate-off</c> icon.</summary>
        public static ComponentResourceKey VibrateOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(VibrateOff));

        /// <summary>Lucide <c>vibrate</c> icon.</summary>
        public static ComponentResourceKey Vibrate { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Vibrate));

        /// <summary>Lucide <c>video-off</c> icon.</summary>
        public static ComponentResourceKey VideoOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(VideoOff));

        /// <summary>Lucide <c>video</c> icon.</summary>
        public static ComponentResourceKey Video { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Video));

        /// <summary>Lucide <c>videotape</c> icon.</summary>
        public static ComponentResourceKey Videotape { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Videotape));

        /// <summary>Lucide <c>view</c> icon.</summary>
        public static ComponentResourceKey View { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(View));

        /// <summary>Lucide <c>voicemail</c> icon.</summary>
        public static ComponentResourceKey Voicemail { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Voicemail));

        /// <summary>Lucide <c>volleyball</c> icon.</summary>
        public static ComponentResourceKey Volleyball { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Volleyball));

        /// <summary>Lucide <c>volume-1</c> icon.</summary>
        public static ComponentResourceKey Volume1 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Volume1));

        /// <summary>Lucide <c>volume-2</c> icon.</summary>
        public static ComponentResourceKey Volume2 { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Volume2));

        /// <summary>Lucide <c>volume-off</c> icon.</summary>
        public static ComponentResourceKey VolumeOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(VolumeOff));

        /// <summary>Lucide <c>volume-x</c> icon.</summary>
        public static ComponentResourceKey VolumeX { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(VolumeX));

        /// <summary>Lucide <c>volume</c> icon.</summary>
        public static ComponentResourceKey Volume { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Volume));

        /// <summary>Lucide <c>vote</c> icon.</summary>
        public static ComponentResourceKey Vote { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Vote));

        // ─── W ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>wallet-cards</c> icon.</summary>
        public static ComponentResourceKey WalletCards { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WalletCards));

        /// <summary>Lucide <c>wallet-minimal</c> icon.</summary>
        public static ComponentResourceKey WalletMinimal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WalletMinimal));

        /// <summary>Lucide <c>wallet</c> icon.</summary>
        public static ComponentResourceKey Wallet { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Wallet));

        /// <summary>Lucide <c>wallpaper</c> icon.</summary>
        public static ComponentResourceKey Wallpaper { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Wallpaper));

        /// <summary>Lucide <c>wand-sparkles</c> icon.</summary>
        public static ComponentResourceKey WandSparkles { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WandSparkles));

        /// <summary>Lucide <c>wand</c> icon.</summary>
        public static ComponentResourceKey Wand { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Wand));

        /// <summary>Lucide <c>warehouse</c> icon.</summary>
        public static ComponentResourceKey Warehouse { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Warehouse));

        /// <summary>Lucide <c>washing-machine</c> icon.</summary>
        public static ComponentResourceKey WashingMachine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WashingMachine));

        /// <summary>Lucide <c>watch</c> icon.</summary>
        public static ComponentResourceKey Watch { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Watch));

        /// <summary>Lucide <c>waves-arrow-down</c> icon.</summary>
        public static ComponentResourceKey WavesArrowDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WavesArrowDown));

        /// <summary>Lucide <c>waves-arrow-up</c> icon.</summary>
        public static ComponentResourceKey WavesArrowUp { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WavesArrowUp));

        /// <summary>Lucide <c>waves-horizontal</c> icon.</summary>
        public static ComponentResourceKey WavesHorizontal { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WavesHorizontal));

        /// <summary>Lucide <c>waves-ladder</c> icon.</summary>
        public static ComponentResourceKey WavesLadder { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WavesLadder));

        /// <summary>Lucide <c>waves-vertical</c> icon.</summary>
        public static ComponentResourceKey WavesVertical { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WavesVertical));

        /// <summary>Lucide <c>waypoints</c> icon.</summary>
        public static ComponentResourceKey Waypoints { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Waypoints));

        /// <summary>Lucide <c>webcam-off</c> icon.</summary>
        public static ComponentResourceKey WebcamOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WebcamOff));

        /// <summary>Lucide <c>webcam</c> icon.</summary>
        public static ComponentResourceKey Webcam { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Webcam));

        /// <summary>Lucide <c>webhook-off</c> icon.</summary>
        public static ComponentResourceKey WebhookOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WebhookOff));

        /// <summary>Lucide <c>webhook</c> icon.</summary>
        public static ComponentResourceKey Webhook { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Webhook));

        /// <summary>Lucide <c>weight-tilde</c> icon.</summary>
        public static ComponentResourceKey WeightTilde { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WeightTilde));

        /// <summary>Lucide <c>weight</c> icon.</summary>
        public static ComponentResourceKey Weight { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Weight));

        /// <summary>Lucide <c>wheat-off</c> icon.</summary>
        public static ComponentResourceKey WheatOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WheatOff));

        /// <summary>Lucide <c>wheat</c> icon.</summary>
        public static ComponentResourceKey Wheat { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Wheat));

        /// <summary>Lucide <c>whole-word</c> icon.</summary>
        public static ComponentResourceKey WholeWord { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WholeWord));

        /// <summary>Lucide <c>wifi-cog</c> icon.</summary>
        public static ComponentResourceKey WifiCog { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WifiCog));

        /// <summary>Lucide <c>wifi-high</c> icon.</summary>
        public static ComponentResourceKey WifiHigh { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WifiHigh));

        /// <summary>Lucide <c>wifi-low</c> icon.</summary>
        public static ComponentResourceKey WifiLow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WifiLow));

        /// <summary>Lucide <c>wifi-off</c> icon.</summary>
        public static ComponentResourceKey WifiOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WifiOff));

        /// <summary>Lucide <c>wifi-pen</c> icon.</summary>
        public static ComponentResourceKey WifiPen { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WifiPen));

        /// <summary>Lucide <c>wifi-sync</c> icon.</summary>
        public static ComponentResourceKey WifiSync { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WifiSync));

        /// <summary>Lucide <c>wifi-zero</c> icon.</summary>
        public static ComponentResourceKey WifiZero { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WifiZero));

        /// <summary>Lucide <c>wifi</c> icon.</summary>
        public static ComponentResourceKey Wifi { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Wifi));

        /// <summary>Lucide <c>wind-arrow-down</c> icon.</summary>
        public static ComponentResourceKey WindArrowDown { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WindArrowDown));

        /// <summary>Lucide <c>wind</c> icon.</summary>
        public static ComponentResourceKey Wind { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Wind));

        /// <summary>Lucide <c>wine-off</c> icon.</summary>
        public static ComponentResourceKey WineOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WineOff));

        /// <summary>Lucide <c>wine</c> icon.</summary>
        public static ComponentResourceKey Wine { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Wine));

        /// <summary>Lucide <c>workflow</c> icon.</summary>
        public static ComponentResourceKey Workflow { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Workflow));

        /// <summary>Lucide <c>worm</c> icon.</summary>
        public static ComponentResourceKey Worm { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Worm));

        /// <summary>Lucide <c>wrench-off</c> icon.</summary>
        public static ComponentResourceKey WrenchOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(WrenchOff));

        /// <summary>Lucide <c>wrench</c> icon.</summary>
        public static ComponentResourceKey Wrench { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Wrench));

        // ─── X ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>x-line-top</c> icon.</summary>
        public static ComponentResourceKey XLineTop { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(XLineTop));

        /// <summary>Lucide <c>x</c> icon.</summary>
        public static ComponentResourceKey X { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(X));

        // ─── Z ──────────────────────────────────────────────────────────────────────────

        /// <summary>Lucide <c>zap-off</c> icon.</summary>
        public static ComponentResourceKey ZapOff { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZapOff));

        /// <summary>Lucide <c>zap</c> icon.</summary>
        public static ComponentResourceKey Zap { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(Zap));

        /// <summary>Lucide <c>zodiac-aquarius</c> icon.</summary>
        public static ComponentResourceKey ZodiacAquarius { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZodiacAquarius));

        /// <summary>Lucide <c>zodiac-aries</c> icon.</summary>
        public static ComponentResourceKey ZodiacAries { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZodiacAries));

        /// <summary>Lucide <c>zodiac-cancer</c> icon.</summary>
        public static ComponentResourceKey ZodiacCancer { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZodiacCancer));

        /// <summary>Lucide <c>zodiac-capricorn</c> icon.</summary>
        public static ComponentResourceKey ZodiacCapricorn { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZodiacCapricorn));

        /// <summary>Lucide <c>zodiac-gemini</c> icon.</summary>
        public static ComponentResourceKey ZodiacGemini { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZodiacGemini));

        /// <summary>Lucide <c>zodiac-leo</c> icon.</summary>
        public static ComponentResourceKey ZodiacLeo { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZodiacLeo));

        /// <summary>Lucide <c>zodiac-libra</c> icon.</summary>
        public static ComponentResourceKey ZodiacLibra { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZodiacLibra));

        /// <summary>Lucide <c>zodiac-ophiuchus</c> icon.</summary>
        public static ComponentResourceKey ZodiacOphiuchus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZodiacOphiuchus));

        /// <summary>Lucide <c>zodiac-pisces</c> icon.</summary>
        public static ComponentResourceKey ZodiacPisces { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZodiacPisces));

        /// <summary>Lucide <c>zodiac-sagittarius</c> icon.</summary>
        public static ComponentResourceKey ZodiacSagittarius { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZodiacSagittarius));

        /// <summary>Lucide <c>zodiac-scorpio</c> icon.</summary>
        public static ComponentResourceKey ZodiacScorpio { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZodiacScorpio));

        /// <summary>Lucide <c>zodiac-taurus</c> icon.</summary>
        public static ComponentResourceKey ZodiacTaurus { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZodiacTaurus));

        /// <summary>Lucide <c>zodiac-virgo</c> icon.</summary>
        public static ComponentResourceKey ZodiacVirgo { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZodiacVirgo));

        /// <summary>Lucide <c>zoom-in</c> icon.</summary>
        public static ComponentResourceKey ZoomIn { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZoomIn));

        /// <summary>Lucide <c>zoom-out</c> icon.</summary>
        public static ComponentResourceKey ZoomOut { get; } =
            new ComponentResourceKey(typeof(LucideIconKeys), nameof(ZoomOut));

    }
}
