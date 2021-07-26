using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PrepareLanding.Core.Gui
{
    public class ButtonDescriptor
    {
        private enum ButtonType
        {
            Unknown = 0,
            /// <summary>
            /// A standard button
            /// </summary>
            ButtonText = 1,

            /// <summary>
            /// A button with a float menu
            /// </summary>
            ButtonTextFloatMenu = 2,
        }

        private List<FloatMenuOption> _floatMenuOptions;

        private FloatMenu _floatMenu;

        private ButtonType _buttonType;

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
            _buttonType = ButtonType.ButtonText;
        }

        public ButtonDescriptor(string label, string tooltip = null, DisplayState displayState = DisplayState.Always)
        {
            Label = label;
            Action = null;
            ToolTip = tooltip;
            DisplayState = displayState;
            _buttonType = ButtonType.ButtonText;
        }

        public void AddFloatMenu(string title, bool needSelection = false)
        {
            if (_floatMenuOptions == null || _floatMenuOptions.Count == 0)
                return;

            _buttonType = ButtonType.ButtonTextFloatMenu;

            _floatMenu = new FloatMenu(_floatMenuOptions, title, needSelection);
        }

        public void AddFloatMenuOption(string label, Action actionClick, Action<Rect> actionMouseOver, MenuOptionPriority menuOptionPriority = MenuOptionPriority.Default)
        {
            if(_floatMenuOptions == null)
                _floatMenuOptions = new List<FloatMenuOption>();

            var menuOption = new FloatMenuOption(label, actionClick, menuOptionPriority, actionMouseOver);
            _floatMenuOptions.Add(menuOption);
        }

        private void DisplayFloatMenu()
        {
            if (_floatMenu == null)
                return;

            Find.WindowStack.Add(_floatMenu);
        }

        public void DrawButton(Rect buttonRect)
        {
            if (!CanBeDisplayed)
                return;

            // display button; if clicked: call the related action
            if (Verse.Widgets.ButtonText(buttonRect, Label))
            {
                Action?.Invoke();

                if (_floatMenu != null && _buttonType == ButtonType.ButtonTextFloatMenu)
                    DisplayFloatMenu();
            }

            // display tool-tip (if any)
            if (!string.IsNullOrEmpty(ToolTip))
                TooltipHandler.TipRegion(buttonRect, ToolTip);


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
