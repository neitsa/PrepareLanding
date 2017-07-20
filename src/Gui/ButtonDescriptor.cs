using System;
using Verse;

namespace PrepareLanding.Gui
{
    public class ButtonDescriptor
    {

        public string Label { get; set; }

        public Action Action { get; set; }

        public string ToolTip { get; set; }

        public DisplayState DisplayState { get; set; }

        public bool CanBeDisplayed
        {
            get
            {
                switch (DisplayState)
                {
                    case DisplayState.Never:
                        return false;
                    case DisplayState.Always:
                        return true;
                }

                bool retValue;
                switch (Current.ProgramState)
                {
                    case ProgramState.Entry:
                        retValue = (DisplayState & DisplayState.Entry) != 0;
                        break;
                    case ProgramState.MapInitializing:
                        retValue = (DisplayState & DisplayState.MapInitializing) != 0;
                        break;
                    case ProgramState.Playing:
                        retValue = (DisplayState & DisplayState.Playing) != 0;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return retValue;
            }
        }

        public ButtonDescriptor(string label, Action action, string tooltip = null, DisplayState displayState = DisplayState.Always)
        {
            Label = label;
            Action = action;
            ToolTip = tooltip;
            DisplayState = displayState;
        }
    }

    [Flags]
    public enum DisplayState
    {
        Never = 0,
        
        /* same names as Verse.programState */
        Entry = 1 << 0,
        MapInitializing = 1 << 1,
        Playing = 1 << 2,

        // all values (except Never)
        Always = Entry | MapInitializing | Playing
    }
}
