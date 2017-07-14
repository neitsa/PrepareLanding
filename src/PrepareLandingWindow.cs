using System.Collections.Generic;
using PrepareLanding.Extensions;
using PrepareLanding.Gui.Tab;
using PrepareLanding.Gui.Window;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PrepareLanding
{
    public class PrepareLandingWindow : MinimizableWindow
    {
        private readonly Vector2 _bottomButtonSize = new Vector2(160f, 30f);

        private readonly TabGuiUtilityController _tabController = new TabGuiUtilityController();

        private readonly List<ITabGuiUtility> _tabGuiUtilities = new List<ITabGuiUtility>();

        public PrepareLandingWindow(PrepareLandingUserData userData)
        {
            doCloseButton = false; // explicitly disable close button, we'll draw it ourselves
            doCloseX = true;
            optionalTitle = "Prepare Landing";
            MinimizedWindow.WindowLabel = optionalTitle;

            /* 
             * GUI utilities (tabs)
             */
            var tabGuiUtilityTerrain = new TabGuiUtilityTerrain(userData, 0.30f);
            var tabGuiUtilityTemperature = new TabGuiUtilityTemperature(userData, 0.30f);
            var tabGuiUtilityInfo = new TabGuiUtilityInfo(userData, 0.48f);
            var tabGuiUtilityOptions = new TabGuiUtilityOptions(userData, 0.30f);

            _tabGuiUtilities.Clear();
            _tabGuiUtilities.Add(tabGuiUtilityTerrain);
            _tabGuiUtilities.Add(tabGuiUtilityTemperature);
            _tabGuiUtilities.Add(tabGuiUtilityInfo);
            _tabGuiUtilities.Add(tabGuiUtilityOptions);

            _tabController.Clear();
            _tabController.AddTabRange(_tabGuiUtilities);
        }

        protected override float Margin => 0f;

        public override Vector2 InitialSize => new Vector2(1024f, 768f);


        public override void DoWindowContents(Rect inRect)
        {
            inRect.yMin += 72f;
            Widgets.DrawMenuSection(inRect);

            _tabController.DrawTabs(inRect);

            inRect = inRect.ContractedBy(17f);

            _tabController.DrawSelectedTab(inRect);

            DoBottomsButtons(inRect);
        }

        public override void PostClose()
        {
            base.PostClose();

            // when the window is closed and it's not minimized, disable all highlighted tiles
            if (!Minimized)
                PrepareLanding.Instance.TileHighlighter?.RemoveAllTiles();
        }

        protected void DoBottomsButtons(Rect inRect)
        {
            const uint numButtons = 3;
            var buttonsY = windowRect.height - 55f;

            var buttonsRect = inRect.SpaceEvenlyFromCenter(buttonsY, numButtons, _bottomButtonSize.x,
                _bottomButtonSize.y, 10f);
            if (buttonsRect.Count != numButtons)
            {
                Log.ErrorOnce($"[PrepareLanding] Couldn't not get enough room for {numButtons} (in PrepareLandingWindow.DoBottomsButtons)", 0x1237cafe);
                return;
            }

            if (Widgets.ButtonText(buttonsRect[0], "Filter"))
            {
                SoundDefOf.TickLow.PlayOneShotOnCamera();
                Log.Message("[PrepareLanding] Pressed Filter button");
                PrepareLanding.Instance.TileFilter.Filter();
            }

            if (Widgets.ButtonText(buttonsRect[1], "Minimize"))
            {
                SoundDefOf.TickHigh.PlayOneShotOnCamera();
                Minimize();
            }

            if (Widgets.ButtonText(buttonsRect[2], "CloseButton".Translate()))
            {
                SoundDefOf.TickHigh.PlayOneShotOnCamera();
                Close();
            }
        }
    }
}