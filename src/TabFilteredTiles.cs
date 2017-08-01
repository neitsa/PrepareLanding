using System;
using System.Collections.Generic;
using System.Linq;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Core.Gui;
using PrepareLanding.Core.Gui.Tab;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Widgets = Verse.Widgets;

namespace PrepareLanding
{
    public class TabFilteredTiles : TabGuiUtility
    {
        private Vector2 _scrollPosMatchingTiles;

        private int _selectedTileIndex = -1;

        private int _tileDisplayIndexStart;

        private const int MaxDisplayedTileWhenMinimized = 30;

        private readonly List<ButtonDescriptor> _minimizedWindowButtonsDescriptorList;

        public TabFilteredTiles(float columnSizePercent) : base(columnSizePercent)
        {
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

        /// <summary>A unique identifier for the Tab.</summary>
        public override string Id => "FilteredTiles";

        /// <summary>The name of the tab (that is actually displayed at its top).</summary>
        public override string Name => "Filtered Tiles";

        /// <summary>Gets whether the tab can be draw or not.</summary>
        public override bool CanBeDrawn { get; set; } = true;

        /// <summary>Draw the content of the tab.</summary>
        /// <param name="inRect">The <see cref="T:UnityEngine.Rect" /> in which to draw the tab content.</param>
        public override void Draw(Rect inRect)
        {
            Begin(inRect);
            DrawFilteredTiles();
            NewColumn();
            DrawSelectedTileInfo();
            End();
        }

        protected void DrawFilteredTiles()
        {
            DrawEntryHeader("Filtered Tiles", backgroundColor: Color.yellow);

            // default line height
            const float gapLineHeight = 4f;

            //check if we have something to display (tiles)
            var matchingTiles = PrepareLanding.Instance.TileFilter.AllMatchingTiles;
            var matchingTilesCount = matchingTiles.Count;
            if (matchingTilesCount == 0)
                return;

            /*
             * Buttons
             */

            if (ListingStandard.ButtonText("Clear Filtered Tiles"))
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

            var buttonsRectSpace = ListingStandard.GetRect(30f);
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
             * Label
             */

            // number of elements (tiles) to display
            var itemsToDisplay = Math.Min(matchingTilesCount - _tileDisplayIndexStart, MaxDisplayedTileWhenMinimized);

            // label to display where we actually are in the tile list
            GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
            var heightBefore = ListingStandard.StartCaptureHeight();
            ListingStandard.Label(
                $"{_tileDisplayIndexStart}: {_tileDisplayIndexStart + itemsToDisplay - 1} / {matchingTilesCount - 1}",
                DefaultElementHeight);
            GenUI.ResetLabelAlign();
            var counterLabelRect = ListingStandard.EndCaptureHeight(heightBefore);
            Core.Gui.Widgets.DrawHighlightColor(counterLabelRect, Color.cyan, 0.50f);

            // add a gap before the scroll view
            ListingStandard.Gap(gapLineHeight);

            /*
             * Calculate heights
             */

            // height of the scrollable outer Rect (visible portion of the scroll view, not the 'virtual' one)
            var maxScrollViewOuterHeight = InRect.height - ListingStandard.CurHeight - 30f;

            // height of the 'virtual' portion of the scroll view
            var scrollableViewHeight = itemsToDisplay * DefaultElementHeight + gapLineHeight * MaxDisplayedTileWhenMinimized;

            /*
             * Scroll view
             */
            var innerLs = ListingStandard.BeginScrollView(maxScrollViewOuterHeight, scrollableViewHeight,
                ref _scrollPosMatchingTiles, 16f);

            var endIndex = _tileDisplayIndexStart + itemsToDisplay;
            for (var i = _tileDisplayIndexStart; i < endIndex; i++)
            {
                var selectedTileId = matchingTiles[i];
                var selectedTile = Find.World.grid[selectedTileId];

                // get latitude & longitude for the tile
                var vector = Find.WorldGrid.LongLatOf(selectedTileId);
                var labelText =
                    $"{i}: {vector.y.ToStringLatitude()} {vector.x.ToStringLongitude()} - {selectedTile.biome.LabelCap} ; {selectedTileId}";

                // display the label
                var labelRect = innerLs.GetRect(DefaultElementHeight);
                var selected = i == _selectedTileIndex;
                if (Core.Gui.Widgets.LabelSelectable(labelRect, labelText, ref selected, TextAnchor.MiddleCenter))
                {
                    // go to the location of the selected tile
                    _selectedTileIndex = i;
                    Find.WorldInterface.SelectedTile = selectedTileId;
                    Find.WorldCameraDriver.JumpTo(Find.WorldGrid.GetTileCenter(Find.WorldInterface.SelectedTile));
                }
                // add a thin line between each label
                innerLs.GapLine(gapLineHeight);
            }

            ListingStandard.EndScrollView(innerLs);
        }

        protected void DrawSelectedTileInfo()
        {
            DrawEntryHeader("Selected Tile Info", backgroundColor: Color.yellow);

            var matchingTiles = PrepareLanding.Instance.TileFilter.AllMatchingTiles;

            if (_selectedTileIndex < 0 || _selectedTileIndex >= matchingTiles.Count)
                return;

            ListingStandard.verticalSpacing = 0f;

            var selTileId = matchingTiles[_selectedTileIndex];
            var selTile = Find.World.grid[selTileId];

            ListingStandard.Label(selTile.biome.LabelCap);
            var y = Find.WorldGrid.LongLatOf(selTileId).y;
            ListingStandard.Label(selTile.biome.description);
            ListingStandard.Gap(8f);
            ListingStandard.GapLine();
            if (!selTile.biome.implemented)
            {
                ListingStandard.Label(selTile.biome.LabelCap + " " + "BiomeNotImplemented".Translate());
            }
            ListingStandard.LabelDouble("Terrain".Translate(), selTile.hilliness.GetLabelCap());
            if (selTile.VisibleRoads != null)
            {
                ListingStandard.LabelDouble("Road".Translate(), GenText.ToCommaList((from roadlink in selTile.VisibleRoads
                    select roadlink.road.label).Distinct()).CapitalizeFirst());
            }
            if (selTile.VisibleRivers != null)
            {
                ListingStandard.LabelDouble("River".Translate(), selTile.VisibleRivers.MaxBy((riverlink) => riverlink.river.degradeThreshold).river.LabelCap);
            }
            if (!Find.World.Impassable(selTileId))
            {
                const int num = 2500;
                var numTicks = Mathf.Min(num + WorldPathGrid.CalculatedCostAt(selTileId, false), 120000);
                ListingStandard.LabelDouble("MovementTimeNow".Translate(), numTicks.ToStringTicksToPeriod());
                var numTicks2 = Mathf.Min(num + WorldPathGrid.CalculatedCostAt(selTileId, false, Season.Summer.GetMiddleYearPct(y)), 120000);
                ListingStandard.LabelDouble("MovementTimeSummer".Translate(), numTicks2.ToStringTicksToPeriod());
                var numTicks3 = Mathf.Min(num + WorldPathGrid.CalculatedCostAt(selTileId, false, Season.Winter.GetMiddleYearPct(y)), 120000);
                ListingStandard.LabelDouble("MovementTimeWinter".Translate(), numTicks3.ToStringTicksToPeriod());
            }
            if (selTile.biome.canBuildBase)
            {
                ListingStandard.LabelDouble("StoneTypesHere".Translate(), GenText.ToCommaList(from rt in Find.World.NaturalRockTypesIn(selTileId)
                    select rt.label).CapitalizeFirst());
            }
            ListingStandard.LabelDouble("Elevation".Translate(), selTile.elevation.ToString("F0") + "m");
            ListingStandard.GapLine();
            ListingStandard.LabelDouble("AvgTemp".Translate(), selTile.temperature.ToStringTemperature());
            var celsiusTemp = GenTemperature.AverageTemperatureAtTileForTwelfth(selTileId, Season.Winter.GetMiddleTwelfth(y));
            ListingStandard.LabelDouble("AvgWinterTemp".Translate(), celsiusTemp.ToStringTemperature());
            var celsiusTemp2 = GenTemperature.AverageTemperatureAtTileForTwelfth(selTileId, Season.Summer.GetMiddleTwelfth(y));
            ListingStandard.LabelDouble("AvgSummerTemp".Translate(), celsiusTemp2.ToStringTemperature());
            ListingStandard.LabelDouble("OutdoorGrowingPeriod".Translate(), Zone_Growing.GrowingQuadrumsDescription(selTileId));
            ListingStandard.LabelDouble("Rainfall".Translate(), selTile.rainfall.ToString("F0") + "mm");
            ListingStandard.LabelDouble("AnimalsCanGrazeNow".Translate(), (!VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsNowAt(selTileId)) ? "No".Translate() : "Yes".Translate());
            ListingStandard.GapLine();
            ListingStandard.LabelDouble("TimeZone".Translate(), GenDate.TimeZoneAt(Find.WorldGrid.LongLatOf(selTileId).x).ToStringWithSign());
            var rot = Find.World.CoastDirectionAt(selTileId);
            if (rot.IsValid)
            {
                ListingStandard.LabelDouble(string.Empty, ("HasCoast" + rot).Translate());
            }
            if (Prefs.DevMode)
            {
                ListingStandard.LabelDouble("Debug world tile ID", selTileId.ToString());
            }
        }
    }
}
