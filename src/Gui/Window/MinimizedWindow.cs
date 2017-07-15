using System;
using PrepareLanding.Extensions;
using UnityEngine;
using Verse;

namespace PrepareLanding.Gui.Window
{
    public class MinimizedWindow : Verse.Window
    {
        private readonly Verse.Window _parentWindow;

        private readonly Listing_Standard _listingStandard;

        private bool _minimizedWindowHasAddedContent;

        public event Action<Listing_Standard, Rect> AddMinimizedWindowContent;

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

            // begin Rect position
            _listingStandard.Begin(rect);

            // get a Rect for the next label (without actually 'allocating' it in the Listing_Standard)
            var nextRect = _listingStandard.VirtualRect(30f);
            // split the Rect
            var labelRect = nextRect.LeftPart(0.9f);
            var buttonRect = nextRect.RightPart(0.1f);

            _listingStandard.GetRect(30f);
            GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
            Verse.Widgets.Label(labelRect, WindowLabel);
            GenUI.ResetLabelAlign();

            if (Verse.Widgets.ButtonText(buttonRect, "▲"))
                Close();

            TooltipHandler.TipRegion(buttonRect, "Maximize Window");

            _listingStandard.GapLine();

            AddMinimizedWindowContent?.Invoke(_listingStandard, inRect);

            // check if the height changed (meaning new content was added)
            if (Math.Abs(windowRect.height - InitialSize.y) > 1f && !_minimizedWindowHasAddedContent)
            {
                // recalculate position
                windowRect.y = (UI.screenHeight - windowRect.height) / 2;

                // do it only once
                _minimizedWindowHasAddedContent = true;
            }

            _listingStandard.End();
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
            _minimizedWindowHasAddedContent = false;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            Closed = false;
            _minimizedWindowHasAddedContent = false;
        }
    }
}