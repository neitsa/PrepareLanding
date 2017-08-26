using System;

namespace PrepareLanding.Core.Extensions
{
    public static class MathExtensions
    {
        public static float PreciseRound(this float f, int digits,
            MidpointRounding rounding = MidpointRounding.AwayFromZero)
        {
            if (digits < 0)
                digits = 0;

            return (float) Math.Round((decimal) f, digits, rounding);
        }

        public static bool IsInEpsilonRange(this float f, float x, float epsilon)
        {
            return Math.Abs(f - x) <= epsilon;
        }
    }
}