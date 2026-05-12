using System;
using System.Globalization;

namespace WWSearchDataGrid.Modern.Core.Display
{
    /// <summary>
    /// Single entry point for resolving a <see cref="MaskType"/> to a concrete
    /// <see cref="IMaskFormatter"/>. Centralizes the "is this type implemented yet?" diagnostic
    /// so consumers (edit settings, behaviors, converters) don't repeat the switch.
    /// </summary>
    public static class MaskFormatterFactory
    {
        /// <summary>
        /// Creates an <see cref="IMaskFormatter"/> for <paramref name="type"/>.
        /// Throws <see cref="NotSupportedException"/> for types that don't have an engine yet.
        /// </summary>
        /// <param name="type">Which engine the mask string targets.</param>
        /// <param name="mask">The mask pattern (grammar varies by <paramref name="type"/>).</param>
        /// <param name="promptChar">Character shown for empty required slots (default <c>'_'</c>).</param>
        /// <param name="culture">Culture for number / date formatting; defaults to current.</param>
        public static IMaskFormatter Create(
            MaskType type,
            string mask,
            char promptChar = '_',
            CultureInfo culture = null)
        {
            if (culture == null) culture = CultureInfo.CurrentCulture;

            switch (type)
            {
                case MaskType.Simple:
                    return new SimpleMaskFormatter(mask, promptChar);

                case MaskType.Numeric:
                    return new NumericMaskFormatter(mask, culture);

                case MaskType.DateTime:
                case MaskType.DateOnly:
                case MaskType.TimeOnly:
                    // Single engine handles all three — the format string the consumer supplies
                    // determines whether the resulting mask is date-only, time-only, or both.
                    // Per-type validation (rejecting time specifiers in DateOnly, etc.) is a
                    // future refinement.
                    return new DateTimeMaskFormatter(mask, culture);

                case MaskType.TimeSpan:
                    return new TimeSpanMaskFormatter(mask, culture);

                case MaskType.DateTimeOffset:
                case MaskType.RegEx:
                case MaskType.SimpleRegEx:
                    throw NotImplemented(type);

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown MaskType.");
            }
        }

        /// <summary>
        /// Validates that <paramref name="type"/> has an engine without instantiating one.
        /// Use from edit-settings template builders to fail fast on misconfiguration.
        /// </summary>
        public static void EnsureSupported(MaskType type)
        {
            switch (type)
            {
                case MaskType.Simple:
                case MaskType.Numeric:
                case MaskType.DateTime:
                case MaskType.DateOnly:
                case MaskType.TimeOnly:
                case MaskType.TimeSpan:
                    return;
                default:
                    throw NotImplemented(type);
            }
        }

        private static NotSupportedException NotImplemented(MaskType type) =>
            new NotSupportedException(
                $"MaskType '{type}' is not yet implemented. Implemented engines: Simple, Numeric, " +
                $"DateTime / DateOnly / TimeOnly, TimeSpan. For other types, use " +
                $"GridColumn.DisplayStringFormat (display side) and the corresponding editor " +
                $"(edit side) until the engine ships.");
    }
}
