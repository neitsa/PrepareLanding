using System;
using PrepareLanding.Extensions;
using UnityEngine;
using Verse;

namespace PrepareLanding.Gui.Window
{
    public class MinimizedWindow : Verse.Window
    {
        private readonly MinimizableWindow _parentWindow;

        private readonly Listing_Standard _listingStandard;

        private bool _minimizedWindowHasAddedContent;

        public event Action<Listing_Standard, Rect> AddMinimizedWindowContent;

        public MinimizedWindow(MinimizableWindow parentWindow, string windowLabel = null)
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

            _listingStandard = new Listing_Standard();
        }

        public bool Closed { get; private set; }

        public override Vector2 InitialSize => new Vector2(180f, 80f);

        protected override float Margin => 5f;

        public string WindowLabel { get; set; }

        public event Action OnMinimizedWindowClosed = delegate { };

        public override void DoWindowContents(Rect inRect)
        {
            var rect = inRect;

            // set up column size
            _listingStandard.ColumnWidth = inRect.width;

            // begin drawing
            _listingStandard.Begin(rect);

            // get a Rect for the next label (without actually 'allocating' it in the Listing_Standard)
            var nextRect = _listingStandard.VirtualRect(30f);
            // split the Rect
            var labelRect = nextRect.LeftPart(0.9f);
            var buttonRect = nextRect.RightPart(0.1f);

            // add 'title' for the minimized window
            _listingStandard.GetRect(30f);
            GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
            Verse.Widgets.Label(labelRect, WindowLabel);
            GenUI.ResetLabelAlign();

            // add 'maximize' button
            if (Verse.Widgets.ButtonText(buttonRect, "▲"))
                Close();

            TooltipHandler.TipRegion(buttonRect, "Maximize Window");

            // make some space before eventual content
            _listingStandard.GapLine(6f);

            // subscribers can add new content to the minimized window.
            AddMinimizedWindowContent?.Invoke(_listingStandard, inRect);

            // check if the height changed (meaning new content was added)
            if (Math.Abs(windowRect.height - InitialSize.y) > 1f && !_minimizedWindowHasAddedContent)
            {
                // recalculate y position
                windowRect.y = (UI.screenHeight - windowRect.height) / 2;

                // do it only once
                _minimizedWindowHasAddedContent = true;
            }

            // end drawing
            _listingStandard.End();
        }

        protected override void SetInitialSizeAndPosition()
        {
            // reposition on the bottom left of the screen but above the other utilities
            windowRect = new Rect(UI.screenWidth - InitialSize.x - 40f, UI.screenHeight - InitialSize.y - 80f,
                InitialSize.x, InitialSize.y);
            windowRect = windowRect.Rounded();
        }

        public override void Close(bool doCloseSound = true)
        {
            // close this minimized window
            base.Close(doCloseSound);

            // display the parent (add it to the window stack)
            Find.WindowStack.Add(_parentWindow);

            // tell subscribers that this minimized window has been closed
            OnMinimizedWindowClosed.Invoke();

            Closed = true;
            _minimizedWindowHasAddedContent = false;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            Closed = false;
            _minimizedWindowHasAddedContent = false;
        }

        public override void WindowOnGUI()
        {
            if (!_parentWindow.IsWindowValidInContext)
            {
                _parentWindow.ForceClose();
                return;
            }

            base.WindowOnGUI();
        }
    }
}