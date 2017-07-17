using System;

namespace PrepareLanding.Gui
{
    public class ButtonDescriptor
    {

        public string Label { get; set; }

        public Action Action { get; set; }

        public string ToolTip { get; set; }

        public ButtonDescriptor(string label, Action action, string tooltip = null)
        {
            Label = label;
            Action = action;
            ToolTip = tooltip;
        }
    }
}
