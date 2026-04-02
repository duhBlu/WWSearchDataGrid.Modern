using System;

namespace WWSearchDataGrid.Modern.Core.Display
{
    /// <summary>
    /// Display value provider that uses a mask pattern to format raw values.
    /// Wraps <see cref="MaskFormatter"/> to implement <see cref="IDisplayValueProvider"/>.
    /// </summary>
    public class MaskDisplayProvider : IDisplayValueProvider
    {
        private readonly MaskFormatter _formatter;

        /// <summary>
        /// Creates a new MaskDisplayProvider with the specified mask pattern.
        /// </summary>
        /// <param name="mask">The mask pattern (e.g., "0+\.00", "(000) 000-0000")</param>
        /// <param name="promptChar">Character for empty required slots (default: '_')</param>
        public MaskDisplayProvider(string mask, char promptChar = '_')
        {
            _formatter = new MaskFormatter(mask, promptChar);
        }

        /// <summary>
        /// Formats a raw value through the mask pattern.
        /// </summary>
        public string FormatValue(object rawValue)
        {
            return _formatter.Format(rawValue);
        }

        /// <summary>
        /// Parses a masked display string back to its unmasked raw value.
        /// </summary>
        public object ParseValue(string displayText)
        {
            return _formatter.Parse(displayText);
        }

        public bool CanParse => true;

        public bool UseRawComparison => true;

        /// <summary>
        /// Strips mask literal characters from user input, leaving only data characters.
        /// "(573)" → "573", "(573) 555-" → "573555"
        /// </summary>
        public string StripLiterals(string text) => _formatter.StripLiterals(text);

        /// <summary>
        /// Formats a value aligned to the end of the mask.
        /// "1234" → "(___) ___-1234" for phone mask.
        /// </summary>
        public string FormatEndAligned(string value) => _formatter.FormatEndAligned(value);
    }
}
