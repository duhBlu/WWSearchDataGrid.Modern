using System;

namespace WWControls.Wpf
{
    /// <summary>
    /// Cross-target stand-in for <c>Math.Clamp</c>, which does not exist on .NET Framework.
    /// Matches its semantics, including throwing when <paramref name="min"/> exceeds
    /// <paramref name="max"/>.
    /// </summary>
    internal static class MathCompat
    {
        public static double Clamp(double value, double min, double max)
        {
            if (min > max)
                throw new ArgumentException($"'{min}' cannot be greater than '{max}'.");

            return value < min ? min : value > max ? max : value;
        }
    }
}
