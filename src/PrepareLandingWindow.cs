using System;
using System.Collections.Generic;
using PrepareLanding.Collections;
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
        private readonly Vector2 _bottomButtonSize = new Vector2(130f, 30f);

        private readonly float _gapBetweenButtons = 10f;

        private readonly List<ITabGuiUtility> _tabGuiUtilities = new List<ITabGuiUtility>();

        private readonly PairList<string, Action> _bottomButtonsPairList;

        public TabGuiUtilityController TabController { get; } = new TabGuiUtilityController();

        public PrepareLandingWindow(PrepareLandingUserData userData)
        {
            doCloseButton = false; // explicitly disable close button, we'll draw it ourselves
            doCloseX = true;
            optionalTitle = "Prepare Landing";
            MinimizedWindow.WindowLabel = optionalTitle;

            /* 
             * GUI utilities (tabs)
             */
            _tabGuiUtilities.Clear();
            _tabGuiUtilities.Add(new TabGuiUtilityTerrain(userData, 0.30f));
            _tabGuiUtilities.Add(new TabGuiUtilityTemperature(userData, 0.30f));
            _tabGuiUtilities.Add(new TabGuiUtilityFilteredTiles(0.48f));
            _tabGuiUtilities.Add(new TabGuiUtilityInfo(userData, 0.48f));
            _tabGuiUtilities.Add(new TabGuiUtilityOptions(userData, 0.30f));

            TabController.Clear();
            TabController.AddTabRange(_tabGuiUtilities);

            /*
             * Bottom buttons
             */

            _bottomButtonsPairList = new PairList<string, Action>
            {
                {
                    "Filter Tiles", delegate
                    {
                        SoundDefOf.TickLow.PlayOneShotOnCamera();
                        PrepareLanding.Instance.TileFilter.Filter();
                    }
                },
                {
                    "Reset Filters", delegate
                    {
                        SoundDefOf.TickLow.PlayOneShotOnCamera();
                        PrepareLanding.Instance.UserData.ResetAllFields();
                    }
                },
                {
                    "Minimize", delegate
                    {
                        SoundDefOf.TickHigh.PlayOneShotOnCamera();
                        Minimize();
                    }
                },
                {
                    "CloseButton".Translate(), delegate
                    {
                        SoundDefOf.TickHigh.PlayOneShotOnCamera();
                        Close();
                    }
                }
            };
        }

        protected override float Margin => 0f;

        public override Vector2 InitialSize => new Vector2(1024f, 768f);


        public override void DoWindowContents(Rect inRect)
        {
            inRect.yMin += 72f;
            Widgets.DrawMenuSection(inRect);

            TabController.DrawTabs(inRect);

            inRect = inRect.ContractedBy(17f);

            TabController.DrawSelectedTab(inRect);

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
           var numButtons = _bottomButtonsPairList.Count;
            var buttonsY = windowRect.height - 55f;

            var buttonsRect = inRect.SpaceEvenlyFromCenter(buttonsY, numButtons, _bottomButtonSize.x,
                _bottomButtonSize.y, _gapBetweenButtons);
            if (buttonsRect.Count != numButtons)
            {
                Log.ErrorOnce($"[PrepareLanding] Couldn't not get enough room for {numButtons} (in PrepareLandingWindow.DoBottomsButtons)", 0x1237cafe);
                return;
            }

            for (var i = 0; i < _bottomButtonsPairList.Count; i++)
            {
                var buttonPairList = _bottomButtonsPairList[i];
                var name = buttonPairList.Key;
                var action = buttonPairList.Value;

                if (Widgets.ButtonText(buttonsRect[i], name))
                    action();
            }
        }
    }
}