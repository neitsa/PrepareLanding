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
        private const float GapBetweenButtons = 10f;

        private const int MaxDisplayedTileWhenMinimized = 30;
        private readonly Vector2 _bottomButtonSize = new Vector2(130f, 30f);

        private readonly PairList<string, Action> _bottomButtonsPairList;

        private readonly List<ITabGuiUtility> _tabGuiUtilities = new List<ITabGuiUtility>();

        private readonly List<float> _buttonsSplitPct = new List<float> {0.33f, 0.33f, 0.33f};

        private Vector2 _scrollPosMatchingTiles;

        private int _selectedTileIndex;

        private int _tileDisplayIndexStart;

        public PrepareLandingWindow(PrepareLandingUserData userData)
        {
            doCloseButton = false; // explicitly disable close button, we'll draw it ourselves
            doCloseX = true;
            optionalTitle = "Prepare Landing";
            MinimizedWindow.WindowLabel = optionalTitle;
            MinimizedWindow.AddMinimizedWindowContent += AddMinimizedWindowContent;

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

                        // reset starting display index
                        _tileDisplayIndexStart = 0;

                        // reset selected index
                        _selectedTileIndex = -1;

                        // do the tile filtering
                        PrepareLanding.Instance.TileFilter.Filter();
                    }
                },
                {
                    "Reset Filters", delegate
                    {
                        SoundDefOf.TickLow.PlayOneShotOnCamera();
                        userData.ResetAllFields();
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

                        // reset starting display index
                        _tileDisplayIndexStart = 0;

                        // reset selected index
                        _selectedTileIndex = -1;

                        ForceClose();
                    }
                }
            };
        }

        public TabGuiUtilityController TabController { get; } = new TabGuiUtilityController();

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
                PrepareLanding.Instance.TileHighlighter.RemoveAllTiles();
        }

        protected void DoBottomsButtons(Rect inRect)
        {
            var numButtons = _bottomButtonsPairList.Count;
            var buttonsY = windowRect.height - 55f;

            var buttonsRect = inRect.SpaceEvenlyFromCenter(buttonsY, numButtons, _bottomButtonSize.x,
                _bottomButtonSize.y, GapBetweenButtons);
            if (buttonsRect.Count != numButtons)
            {
                Log.ErrorOnce(
                    $"[PrepareLanding] Couldn't not get enough room for {numButtons} (in PrepareLandingWindow.DoBottomsButtons)",
                    0x1237cafe);
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

        protected void AddMinimizedWindowContent(Listing_Standard listingStandard, Rect inRect)
        {
            /* constants used for GUI elements */

            // default line height
            const float gapLineHeight = 4f;
            // default visual element height
            const float elementHeight = 30f;

            //check if we have something to display (tiles)
            var matchingTiles = PrepareLanding.Instance.TileFilter.AllMatchingTiles;
            var matchingTilesCount = matchingTiles.Count;
            if (matchingTilesCount == 0)
            {
                // revert to initial window size if needed
                MinimizedWindow.windowRect.height = MinimizedWindow.InitialSize.y;
                return;
            }

            /*
             * Buttons
             */

            if (listingStandard.ButtonText("Clear Filtered Tiles"))
            {
                // clear everything
                PrepareLanding.Instance.TileFilter.ClearMatchingTiles();

                // reset starting display index
                _tileDisplayIndexStart = 0;

                // reset selected index
                _selectedTileIndex = -1;

                // don't go further as there are no tile content to draw
                return;
            }

            var bottomButtonsRect = listingStandard.GetRect(30f);
            var splittedRect = bottomButtonsRect.SplitRectWidth(_buttonsSplitPct);

            if (Widgets.ButtonText(splittedRect[0], "<"))
                if (_tileDisplayIndexStart >= MaxDisplayedTileWhenMinimized)
                    _tileDisplayIndexStart -= MaxDisplayedTileWhenMinimized;
                else
                    Messages.Message("Reached start of tile list.", MessageSound.RejectInput);

            TooltipHandler.TipRegion(splittedRect[0], "Previous Available Tiles");

            if (Widgets.ButtonText(splittedRect[1], "0"))
                _tileDisplayIndexStart = 0;

            TooltipHandler.TipRegion(splittedRect[1], "Tile List Start");

            if (Widgets.ButtonText(splittedRect[2], ">"))
            {
                _tileDisplayIndexStart += MaxDisplayedTileWhenMinimized;
                if (_tileDisplayIndexStart > matchingTilesCount)
                {
                    Messages.Message($"No more tiles available to display (max: {matchingTilesCount}).",
                        MessageSound.RejectInput);
                    _tileDisplayIndexStart -= MaxDisplayedTileWhenMinimized;
                }
            }
            TooltipHandler.TipRegion(splittedRect[2], "Next Available Tiles");

            // number of elements (tiles) to display
            var itemsToDisplay = Math.Min(matchingTilesCount - _tileDisplayIndexStart, MaxDisplayedTileWhenMinimized);

            // label to display where we actually are in the tile list
            GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
            var heightBefore = listingStandard.StartCaptureHeight();
            listingStandard.Label(
                $"{_tileDisplayIndexStart}: {_tileDisplayIndexStart + itemsToDisplay - 1} / {matchingTilesCount - 1}",
                elementHeight);
            GenUI.ResetLabelAlign();
            var counterLabelRect = listingStandard.EndCaptureHeight(heightBefore);
            Gui.Widgets.DrawHighlightColor(counterLabelRect, Color.cyan, 0.50f);

            // add a gap before the scroll view
            listingStandard.Gap(gapLineHeight);

            /*
             * Calculate heights
             */

            // height of the scrollable outer Rect (visible portion of the scroll view, not the 'virtual' one)
            var maxScrollViewOuterHeight = inRect.height - listingStandard.CurHeight;

            // recalculate window height: initial size + visible scroll view height + current height of the listing standard (hence accounting for all buttons above)
            var newWindowHeight = MinimizedWindow.InitialSize.y + maxScrollViewOuterHeight + listingStandard.CurHeight;

            // minimized window height can't be more than 70% of the screen height
            MinimizedWindow.windowRect.height = Mathf.Min(newWindowHeight, UI.screenHeight * 0.70f);

            // height of the 'virtual' portion of the scroll view
            var scrollableViewHeight = itemsToDisplay * elementHeight + gapLineHeight * MaxDisplayedTileWhenMinimized;

            /*
             * Scroll view
             */
            var innerLs = listingStandard.BeginScrollView(maxScrollViewOuterHeight, scrollableViewHeight,
                ref _scrollPosMatchingTiles, 16f);

            var endIndex = _tileDisplayIndexStart + itemsToDisplay;
            for (var i = _tileDisplayIndexStart; i < endIndex; i++)
            {
                var selectedTileId = matchingTiles[i];

                // get latitude & longitude for the tile
                var vector = Find.WorldGrid.LongLatOf(selectedTileId);
                var labelText = $"{i}: {vector.y.ToStringLatitude()} {vector.x.ToStringLongitude()}";

                // display the label
                var labelRect = innerLs.GetRect(elementHeight);
                var selected = i == _selectedTileIndex;
                if (Gui.Widgets.LabelSelectable(labelRect, labelText, ref selected, TextAnchor.MiddleCenter))
                {
                    // go to the location of the selected tile
                    _selectedTileIndex = i;
                    Find.WorldInterface.SelectedTile = selectedTileId;
                    Find.WorldCameraDriver.JumpTo(Find.WorldGrid.GetTileCenter(Find.WorldInterface.SelectedTile));
                }
                // add a thin line between each label
                innerLs.GapLine(gapLineHeight);
            }

            listingStandard.EndScrollView(innerLs);
        }
    }
}