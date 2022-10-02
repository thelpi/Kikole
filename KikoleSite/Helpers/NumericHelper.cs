using System;

namespace KikoleSite.Helpers
{
    internal static class NumericHelper
    {
        internal static int ToPercentRate(this int numerator, int denominator)
        {
            return denominator == 0 ? 0 : (int)Math.Round(numerator / (decimal)denominator * 100);
        }
    }
}
