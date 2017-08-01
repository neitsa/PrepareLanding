using System.Collections.Generic;
using RimWorld;
using Verse;

namespace PrepareLanding.Extensions
{
    public static class TwelfthExtensions
    {
        public static string GrowingDaysString(this Twelfth twelfth)
        {
            switch (twelfth)
            {
                case Twelfth.Undefined:
                    return "NoGrowingPeriod".Translate();
                case Twelfth.Twelfth:
                    return "GrowYearRound".Translate();

                default:
                    return "PeriodDays".Translate(((int) twelfth + 1) * GenDate.DaysPerTwelfth);
            }
        }

        public static bool IsEqualOrGreaterGrowingPeriod(this Twelfth twelfth, Twelfth other)
        {
            // note: consider Twelfth.Undefined as 0, the other Twelfth just stay in their order.
            var thisTwelfthInt = (int) twelfth + 1 % 13;
            var otherTwelfthInt = (int) other + 1 % 13;

            return thisTwelfthInt >= otherTwelfthInt;
        }

        public static int Compare(this Twelfth thisTwelfth, Twelfth other)
        {
            // note: consider Twelfth.Undefined as 0, the other Twelfth just stay in their order.
            var thisTwelfthInt = (int) thisTwelfth + 1 % 13;
            var otherTwelfthInt = (int) other + 1 % 13;

            return Comparer<int>.Default.Compare(thisTwelfthInt, otherTwelfthInt);
        }

        public static int ToGrowingDays(this Twelfth thisTwelfth)
        {
            return ((int) thisTwelfth + 1 % 13) * 5;
        }
    }
}