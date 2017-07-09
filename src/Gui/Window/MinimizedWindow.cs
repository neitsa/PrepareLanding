using System;
using UnityEngine;
using Verse;

namespace PrepareLanding.Gui.Window
{
    public class MinimizedWindow : Verse.Window
    {
        private const float MaximizeButtonWidth = 15f;

        private readonly Verse.Window _parentWindow;

        public bool Closed { get; private set; }

        public MinimizedWindow(Verse.Window parentWindow, string windowLabel = null)
        {
            _parentWindow = parentWindow;

            if (windowLabel.NullOrEmpty())
                WindowLabel = !_parentWindow.optionalTitle.NullOrEmpty()
                    ? _parentWindow.optionalTitle
                    : "Minimized Window";

            doCloseX = false;
            doCloseButton = false;
            closeOnEscapeKey = false;
            preventCameraMotion = false;
            absorbInputAroundWindow = false;
            draggable = true;

            WindowLabel = windowLabel;

            // unless visible, consider it closed by default
            Closed = true;
        }

        public override Vector2 InitialSize => new Vector2(160f, 80f);

        protected override float Margin => 5f;

        public string WindowLabel { get; set; }

        public event Action OnMinimizedWindowClosed = delegate { };

        public override void DoWindowContents(Rect inRect)
        {
            var rect = inRect;

            rect.width = InitialSize.x - Margin * 2 - MaximizeButtonWidth - 5f;
            rect.height = 30;
            rect.y = (InitialSize.y - Margin - rect.height) / 2f;

            Verse.Widgets.Label(rect, WindowLabel);

            var buttonRect = rect;
            buttonRect.x += rect.width + 5f;
            buttonRect.width = MaximizeButtonWidth;
            if (Verse.Widgets.ButtonText(buttonRect, "▲", true, true))
                Close();

            TooltipHandler.TipRegion(buttonRect, "Maximize Window");
        }

        protected override void SetInitialSizeAndPosition()
        {
            // on the bottom left of the screen but above the other utilities
            windowRect = new Rect(UI.screenWidth - InitialSize.x - 40f, UI.screenHeight - InitialSize.y - 80f,
                InitialSize.x, InitialSize.y);
            windowRect = windowRect.Rounded();
        }

        public override void Close(bool doCloseSound = true)
        {
            base.Close(doCloseSound);
            Find.WindowStack.Add(_parentWindow);
            OnMinimizedWindowClosed.Invoke();
            Closed = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            Closed = false;
        }
    }
}