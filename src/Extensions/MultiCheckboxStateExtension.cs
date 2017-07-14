using System;
using Verse;

namespace PrepareLanding.Extensions
{
    public static class MultiCheckboxStateExtension
    {
        public static MultiCheckboxState NextState(this MultiCheckboxState state)
        {
            MultiCheckboxState nextstate;

            // On -> Partial -> Off -> On ...
            switch (state)
            {
                case MultiCheckboxState.On:
                    nextstate = MultiCheckboxState.Partial;
                    break;
                case MultiCheckboxState.Off:
                    nextstate = MultiCheckboxState.On;
                    break;
                case MultiCheckboxState.Partial:
                    nextstate = MultiCheckboxState.Off;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }

            return nextstate;
        }
    }
}