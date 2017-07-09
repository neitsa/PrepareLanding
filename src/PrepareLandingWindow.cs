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

        private readonly PrepareLandingUserData _userData;

        private TabGuiUtilityTemperature _tabGuiUtilityTemperature;
        private TabGuiUtilityTerrain _tabGuiUtilityTerrain;
        private TabGuiUtilityInfo _tabGuiUtilityInfo;
        private TabGuiUtilityOptions _tabGuiUtilityOptions;

        private readonly List<ITabGuiUtility> _tabGuiUtilities = new List<ITabGuiUtility>();

        private readonly TabGuiUtilityController _tabController = new TabGuiUtilityController();

        public PrepareLandingWindow(PrepareLandingUserData userData)
        {
            doCloseButton = false; // explicitly disable close button, we'll draw it ourselves
            doCloseX = true;
            optionalTitle = "Prepare Landing";
            MinimizedWindow.WindowLabel = optionalTitle;

            _userData = userData;
        }

        protected override float Margin => 0f;

        public override Vector2 InitialSize => new Vector2(1024f, 768f);

        public override void PreOpen()
        {
            base.PreOpen();

            /* 
             * GUI utilities 
             */
            _tabGuiUtilityTerrain = new TabGuiUtilityTerrain(_userData, 0.30f);
            _tabGuiUtilityTemperature = new TabGuiUtilityTemperature(_userData, 0.30f);
            _tabGuiUtilityInfo = new TabGuiUtilityInfo(_userData, 0.48f);
            _tabGuiUtilityOptions = new TabGuiUtilityOptions(_userData, 0.30f);

            _tabGuiUtilities.Add(_tabGuiUtilityTerrain);
            _tabGuiUtilities.Add(_tabGuiUtilityTemperature);
            _tabGuiUtilities.Add(_tabGuiUtilityInfo);
            _tabGuiUtilities.Add(_tabGuiUtilityOptions);

            _tabController.AddTabRange(_tabGuiUtilities);
        }

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
            if(!Minimized)
                PrepareLanding.Instance.TileDrawer?.RemoveAllTiles();
        }

        protected void DoBottomsButtons(Rect inRect)
        {
            const uint numButtons = 3;
            var buttonsY = windowRect.height - 55f;

            var buttonsRect = inRect.SpaceEvenlyFromCenter(buttonsY, numButtons, _bottomButtonSize.x,
                _bottomButtonSize.y, 10f);
            if (buttonsRect.Count != numButtons)
            {
                //TODO log error
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