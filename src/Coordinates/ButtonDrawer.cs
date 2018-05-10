using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace PrepareLanding.Coordinates
{
    public class ButtonTextDescriptor
    {
        public ButtonTextDescriptor(string buttonText, string toolTip)
        {
            ButtonText = buttonText;
            ToolTip = toolTip;
        }

        public string ButtonText { get; }

        public string ToolTip { get; }
    }

    public class ButtonTextDescriptorCoordinates : ButtonTextDescriptor
    {
        public ButtonTextDescriptorCoordinates(string buttonText, string toolTip, Vector3 coords) : base(buttonText,
            toolTip)
        {
            Coordinates = coords;
        }

        public Vector3 Coordinates { get; }
    }

    public class DrawerButton
    {
        public DrawerButton(string toolTip, Action<Rect> drawContent, Action<DrawerButton> unfoldAction,
            float buttonWidth = 15f, float drawerHeight = 40.0f)
        {
            ToolTip = toolTip;
            DrawerHeight = drawerHeight;
            ButtonWidth = buttonWidth;
            DrawContent = drawContent;
            UnfoldAction = unfoldAction;
        }

        public float ButtonWidth { get; }

        public Action<Rect> DrawContent { get; }

        public float DrawerHeight { get; }
        public bool IsUnfolded { get; set; }

        public string Text => IsUnfolded ? "▲" : "▼";

        public string ToolTip { get; }

        public Action<DrawerButton> UnfoldAction { get; }
    }

    public class ButtonDrawerHandler
    {
        private readonly List<DrawerButton> _buttonList = new List<DrawerButton>();

        public float ButtonSpace { get; set; }

        public void AddButton(DrawerButton buttonDescriptor)
        {
            _buttonList.Add(buttonDescriptor);
            ButtonSpace = 5f;
        }

        public void DrawButtons(Rect inRect)
        {
            var rectX = inRect.x;
            foreach (var button in _buttonList)
            {
                var buttonRect = new Rect(inRect) { width = button.ButtonWidth, x = rectX };
                if (Widgets.ButtonText(buttonRect, button.Text))
                {
                    button.IsUnfolded = !button.IsUnfolded;
                    if (button.IsUnfolded)
                        UnfoldAllOtherButtons(button);

                    button.UnfoldAction?.Invoke(button);
                }

                var toolTipText = button.IsUnfolded
                    ? $"{"PLCOORDBDRAW_Close".Translate()} {button.ToolTip}"
                    : $"{"PLCOORDBDRAW_Show".Translate()} {button.ToolTip}";
                TooltipHandler.TipRegion(buttonRect, toolTipText);

                rectX += button.ButtonWidth + ButtonSpace;
            }
        }

        public void DrawUnfoldedContent(Rect inRect)
        {
            foreach (var button in _buttonList)
            {
                if (!button.IsUnfolded)
                    continue;

                button.DrawContent?.Invoke(inRect);
                break;
            }
        }

        private void UnfoldAllOtherButtons(DrawerButton unfoldedButton)
        {
            for (var i = _buttonList.Count - 1; i > -1; i--)
            {
                if (_buttonList[i] == unfoldedButton)
                    continue;

                if (_buttonList[i].IsUnfolded)
                {
                    _buttonList[i].IsUnfolded = false;
                    _buttonList[i].UnfoldAction?.Invoke(_buttonList[i]);
                }
            }
        }
    }
}
