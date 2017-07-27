using System;
using System.Collections.Generic;
using System.Linq;
using PrepareLanding.Extensions;
using PrepareLanding.Gui;
using PrepareLanding.Gui.Tab;
using PrepareLanding.Gui.Window;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;
using Widgets = Verse.Widgets;

namespace PrepareLanding
{
    public class PrepareLandingWindow : MinimizableWindow
    {
        private const float GapBetweenButtons = 10f;

        private const int MaxDisplayedTileWhenMinimized = 30;

        private readonly List<ButtonDescriptor> _bottomButtonsDescriptorList;
        private readonly Vector2 _bottomButtonSize = new Vector2(105f, 30f);
        private readonly List<ButtonDescriptor> _minimizedWindowButtonsDescriptorList;

        private readonly List<ITabGuiUtility> _tabGuiUtilities = new List<ITabGuiUtility>();

        private Vector2 _scrollPosMatchingTiles;

        private int _selectedTileIndex;

        private int _tileDisplayIndexStart;

        private PrepareLandingUserData _userData;

        public PrepareLandingWindow(PrepareLandingUserData userData)
        {
            _userData = userData;

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
            _tabGuiUtilities.Add(new TabGuiUtilityLoadSave(userData, 0.48f));

            TabController.Clear();
            TabController.AddTabRange(_tabGuiUtilities);

            /*
             * Bottom buttons
             */

            #region BOTTOM_BUTTONS

            var buttonFilterTiles = new ButtonDescriptor("Filter Tiles",
                delegate
                {
                    SoundDefOf.TickLow.PlayOneShotOnCamera();

                    // reset starting display index
                    _tileDisplayIndexStart = 0;

                    // reset selected index
                    _selectedTileIndex = -1;

                    // do the tile filtering
                    PrepareLanding.Instance.TileFilter.Filter();
                });

            var buttonResetFilters = new ButtonDescriptor("Reset Filters",
                delegate
                {
                    SoundDefOf.TickLow.PlayOneShotOnCamera();
                    userData.ResetAllFields();
                });

            var buttonMinimize = new ButtonDescriptor("Minimize",
                delegate
                {
                    SoundDefOf.TickHigh.PlayOneShotOnCamera();
                    Minimize();
                });

            var buttonClose = new ButtonDescriptor("CloseButton".Translate(),
                delegate
                {
                    SoundDefOf.TickHigh.PlayOneShotOnCamera();

                    // reset starting display index
                    _tileDisplayIndexStart = 0;

                    // reset selected index
                    _selectedTileIndex = -1;

                    ForceClose();
                }, displayState: DisplayState.Entry | DisplayState.MapInitializing);


            _bottomButtonsDescriptorList =
                new List<ButtonDescriptor> {buttonFilterTiles, buttonResetFilters, buttonMinimize, buttonClose};

            #endregion BOTTOM_BUTTONS

            /*
             * Minimized window buttons
             */

            #region MINIMIZED_WINDOW_BUTTONS

            var buttonListStart = new ButtonDescriptor("<<", delegate
            {
                // reset starting display index
                _tileDisplayIndexStart = 0;
            }, "Go to start of tile list.");

            var buttonPreviousPage = new ButtonDescriptor("<", delegate
            {
                if (_tileDisplayIndexStart >= MaxDisplayedTileWhenMinimized)
                    _tileDisplayIndexStart -= MaxDisplayedTileWhenMinimized;
                else
                    Messages.Message("Reached start of tile list.", MessageSound.RejectInput);
            }, "Go to previous list page.");

            var buttonNextPage = new ButtonDescriptor(">", delegate
            {
                var matchingTilesCount = PrepareLanding.Instance.TileFilter.AllMatchingTiles.Count;
                _tileDisplayIndexStart += MaxDisplayedTileWhenMinimized;
                if (_tileDisplayIndexStart > matchingTilesCount)
                {
                    Messages.Message($"No more tiles available to display (max: {matchingTilesCount}).",
                        MessageSound.RejectInput);
                    _tileDisplayIndexStart -= MaxDisplayedTileWhenMinimized;
                }
            }, "Go to next list page.");

            var buttonListEnd = new ButtonDescriptor(">>", delegate
            {
                var matchingTilesCount = PrepareLanding.Instance.TileFilter.AllMatchingTiles.Count;
                var tileDisplayIndexStart = matchingTilesCount - matchingTilesCount % MaxDisplayedTileWhenMinimized;
                if (tileDisplayIndexStart == _tileDisplayIndexStart)
                    Messages.Message($"No more tiles available to display (max: {matchingTilesCount}).",
                        MessageSound.RejectInput);

                _tileDisplayIndexStart = tileDisplayIndexStart;
            }, "Go to end of list.");

            _minimizedWindowButtonsDescriptorList =
                new List<ButtonDescriptor> {buttonListStart, buttonPreviousPage, buttonNextPage, buttonListEnd};

            #endregion MINIMIZED_WINDOW_BUTTONS
        }

        public TabGuiUtilityController TabController { get; } = new TabGuiUtilityController();

        protected override float Margin => 0f;

        public override Vector2 InitialSize => new Vector2(1024f, 768f);

        public override bool IsWindowValidInContext => WorldRendererUtility.WorldRenderedNow && (Find.WindowStack.IsOpen<PrepareLandingWindow>() || Find.WindowStack.IsOpen<MinimizedWindow>());

        public override void DoWindowContents(Rect inRect)
        {
            inRect.yMin += 72f;
            Widgets.DrawMenuSection(inRect);

            TabController.DrawTabs(inRect);

            inRect = inRect.ContractedBy(17f);

            TabController.DrawSelectedTab(inRect);

            DoBottomsButtons(inRect);
        }

        public override void PreOpen()
        {
            base.PreOpen();

            /*
             * note: this code is in PreOpen() rather than in the ctor because otherwise RimWorld would crash (more precisely, Unity crashes).
             * I can't remember exactly where, but it deals with Unity calculating the text size of a floating menu.
             * So better to let this code here rather than in the ctor.
             */
            if (Enumerable.Any(_bottomButtonsDescriptorList, buttonDesctipor => buttonDesctipor.Label == "Load / Save"))
                return;

            var buttonSaveLoadPreset = new ButtonDescriptor("Load / Save", "Load or Save Filter Presets");
            buttonSaveLoadPreset.AddFloatMenuOption("Save", delegate
                {
                    //_userData.PresetManager.TestSave();
                    var tab = TabController.TabById("LoadSave") as TabGuiUtilityLoadSave;
                    if (tab == null)
                        return;

                    tab.LoadSaveMode = LoadSaveMode.Save;
                    TabController.SetSelectedTabById("LoadSave");
                }, delegate
                {
                    var mousePos = Event.current.mousePosition;
                    var rect = new Rect(mousePos.x, mousePos.y, 30f, 30f);

                    TooltipHandler.TipRegion(rect, "Save current filter states to a preset.");
                }
            );
            buttonSaveLoadPreset.AddFloatMenuOption("Load", delegate
                {
                    //_userData.PresetManager.TestLoad();
                    var tab = TabController.TabById("LoadSave") as TabGuiUtilityLoadSave;
                    if (tab == null)
                        return;

                    tab.LoadSaveMode = LoadSaveMode.Load;
                    TabController.SetSelectedTabById("LoadSave");

                }, delegate
                {
                    var mousePos = Event.current.mousePosition;
                    var rect = new Rect(mousePos.x, mousePos.y, 30f, 30f);

                    TooltipHandler.TipRegion(rect, "Load a preset.");
                }
            );
            buttonSaveLoadPreset.AddFloatMenu("Select Action");
            _bottomButtonsDescriptorList.Add(buttonSaveLoadPreset);
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
            var numButtons = _bottomButtonsDescriptorList.Count;
            
            // do not display the close button while playing ("World" button on bottom menu bar was clicked)
            if (Current.ProgramState == ProgramState.Playing)
                numButtons -= 1;

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

            for (var i = 0; i < _bottomButtonsDescriptorList.Count; i++)
            {
                // get button descriptor
                var buttonDescriptor = _bottomButtonsDescriptorList[i];

                buttonDescriptor.DrawButton(buttonsRect[i]);
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

            var buttonsRectSpace = listingStandard.GetRect(30f);
            var splittedRect = buttonsRectSpace.SplitRectWidthEvenly(_minimizedWindowButtonsDescriptorList.Count);

            for (var i = 0; i < _minimizedWindowButtonsDescriptorList.Count; i++)
            {
                // get button descriptor
                var buttonDescriptor = _minimizedWindowButtonsDescriptorList[i];

                // display button; if clicked: call the related action
                if (Widgets.ButtonText(splittedRect[i], buttonDescriptor.Label))
                    buttonDescriptor.Action();

                // display tool-tip (if any)
                if (!string.IsNullOrEmpty(buttonDescriptor.ToolTip))
                    TooltipHandler.TipRegion(splittedRect[i], buttonDescriptor.ToolTip);
            }

            /*
             * Display label (where we actually are in the tile list)
             */

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