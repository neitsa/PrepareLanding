using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        private const int MaxDisplayedTileWhenMinimized = 30;

        private readonly List<ButtonDescriptor> _minimizedWindowButtonsDescriptorList;

        public TabFilteredTiles(float columnSizePercent) : base(columnSizePercent)
        {
            #region MINIMIZED_WINDOW_BUTTONS

            var buttonListStart = new ButtonDescriptor("<<", delegate
            {
                // reset starting display index
                TileDisplayIndexStart = 0;
            }, "PLMWFTIL_GoToStartOfTileList".Translate());

            var buttonPreviousPage = new ButtonDescriptor("<", delegate
            {
                if (TileDisplayIndexStart >= MaxDisplayedTileWhenMinimized)
                    TileDisplayIndexStart -= MaxDisplayedTileWhenMinimized;
                else
                    Messages.Message("PLMWFTIL_ReachedListStart".Translate(), MessageTypeDefOf.RejectInput);
            }, "PLMWFTIL_GoToPreviousListPage".Translate());

            var buttonNextPage = new ButtonDescriptor(">", delegate
            {
                var matchingTilesCount = PrepareLanding.Instance.TileFilter.AllMatchingTiles.Count;
                TileDisplayIndexStart += MaxDisplayedTileWhenMinimized;
                if (TileDisplayIndexStart > matchingTilesCount)
                {
                    Messages.Message($"{"PLMWFTIL_NoMoreTilesAvailable".Translate()} {matchingTilesCount}).",
                        MessageTypeDefOf.RejectInput);
                    TileDisplayIndexStart -= MaxDisplayedTileWhenMinimized;
                }
            }, "PLMWFTIL_GoToNextListPage".Translate());

            var buttonListEnd = new ButtonDescriptor(">>", delegate
            {
                var matchingTilesCount = PrepareLanding.Instance.TileFilter.AllMatchingTiles.Count;
                var tileDisplayIndexStart = matchingTilesCount - matchingTilesCount % MaxDisplayedTileWhenMinimized;
                if (tileDisplayIndexStart == TileDisplayIndexStart)
                    Messages.Message($"{"PLMWFTIL_NoMoreTilesAvailable".Translate()} {matchingTilesCount}).",
                        MessageTypeDefOf.RejectInput);

                TileDisplayIndexStart = tileDisplayIndexStart;
            }, "PLMWFTIL_GoToEndOfList".Translate());

            _minimizedWindowButtonsDescriptorList =
                new List<ButtonDescriptor> {buttonListStart, buttonPreviousPage, buttonNextPage, buttonListEnd};

            #endregion MINIMIZED_WINDOW_BUTTONS
        }

        /// <summary>A unique identifier for the Tab.</summary>
        public override string Id => "FilteredTiles";

        /// <summary>The name of the tab (that is actually displayed at its top).</summary>
        public override string Name => "PLMWFTIL_FilteredTiles".Translate();

        /// <summary>Gets whether the tab can be draw or not.</summary>
        public override bool CanBeDrawn { get; set; } = true;

        public int SelectedTileIndex { get; set; } = -1;

        public int TileDisplayIndexStart { get; set; }

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

        private void DrawFilteredTiles()
        {
            DrawEntryHeader("PLMWFTIL_FilteredTiles".Translate(), backgroundColor: Color.yellow);

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

            if (ListingStandard.ButtonText("PLMWFTIL_ClearFilteredTiles".Translate()))
            {
                // clear everything
                PrepareLanding.Instance.TileFilter.ClearMatchingTiles();

                // reset starting display index
                TileDisplayIndexStart = 0;

                // reset selected index
                SelectedTileIndex = -1;

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
            var itemsToDisplay = Math.Min(matchingTilesCount - TileDisplayIndexStart, MaxDisplayedTileWhenMinimized);

            // label to display where we actually are in the tile list
            GenUI.SetLabelAlign(TextAnchor.MiddleCenter);
            var heightBefore = ListingStandard.StartCaptureHeight();
            ListingStandard.Label(
                $"{TileDisplayIndexStart}: {TileDisplayIndexStart + itemsToDisplay - 1} / {matchingTilesCount - 1}",
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

            var endIndex = TileDisplayIndexStart + itemsToDisplay;
            for (var i = TileDisplayIndexStart; i < endIndex; i++)
            {
                var selectedTileId = matchingTiles[i];
                var selectedTile = Find.World.grid[selectedTileId];

                // get latitude & longitude for the tile
                var vector = Find.WorldGrid.LongLatOf(selectedTileId);
                var labelText =
                    $"{i}: {vector.y.ToStringLatitude()} {vector.x.ToStringLongitude()} - {selectedTile.biome.LabelCap} ; {selectedTileId}";

                // display the label
                var labelRect = innerLs.GetRect(DefaultElementHeight);
                var selected = i == SelectedTileIndex;
                if (Core.Gui.Widgets.LabelSelectable(labelRect, labelText, ref selected, TextAnchor.MiddleCenter))
                {
                    // go to the location of the selected tile
                    SelectedTileIndex = i;
                    Find.WorldInterface.SelectedTile = selectedTileId;
                    Find.WorldCameraDriver.JumpTo(Find.WorldGrid.GetTileCenter(Find.WorldInterface.SelectedTile));
                }
                // add a thin line between each label
                innerLs.GapLine(gapLineHeight);
            }

            ListingStandard.EndScrollView(innerLs);
        }

        private void DrawSelectedTileInfo()
        {
            DrawEntryHeader("PLMWFTIL_SelectedTileInfo".Translate(), backgroundColor: Color.yellow);

            var matchingTiles = PrepareLanding.Instance.TileFilter.AllMatchingTiles;

            if (SelectedTileIndex < 0 || SelectedTileIndex >= matchingTiles.Count)
                return;

            ListingStandard.verticalSpacing = 0f;

            var selTileId = matchingTiles[SelectedTileIndex];
            var selTile = Find.World.grid[selTileId];

            ListingStandard.Label(selTile.biome.description);
            ListingStandard.Gap(8f);
            ListingStandard.GapLine();
            if (!selTile.biome.implemented)
            {
                ListingStandard.Label(selTile.biome.LabelCap + " " + "BiomeNotImplemented".Translate());
            }
            ListingStandard.LabelDouble("Terrain".Translate(), selTile.hilliness.GetLabelCap());
            if (selTile.Roads != null)
            {
                ListingStandard.LabelDouble("Road".Translate(), (from roadlink in selTile.Roads
                                                                  select roadlink.road.label).Distinct().ToCommaList(true).CapitalizeFirst());
            }
            if (selTile.Rivers != null)
            {
                ListingStandard.LabelDouble("River".Translate(), selTile.Rivers.MaxBy(riverlink => riverlink.river.degradeThreshold).river.LabelCap);
            }
            if (!Find.World.Impassable(selTileId))
            {
                var stringBuilder = new StringBuilder();
                var tile = selTileId;
                const bool perceivedStatic = false;
                var explanation = stringBuilder;
                var rightLabel = (WorldPathGrid.CalculatedMovementDifficultyAt(tile, perceivedStatic, null, explanation) * Find.WorldGrid.GetRoadMovementDifficultyMultiplier(selTileId, -1, stringBuilder)).ToString("0.#");
                if (WorldPathGrid.WillWinterEverAffectMovementDifficulty(selTileId) && WorldPathGrid.GetCurrentWinterMovementDifficultyOffset(selTileId, null) < 2f)
                {
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine();
                    stringBuilder.Append(" (");
                    stringBuilder.Append("MovementDifficultyOffsetInWinter".Translate("+" + 2f.ToString("0.#")));
                    stringBuilder.Append(")");
                }
                ListingStandard.LabelDouble("MovementDifficulty".Translate(), rightLabel, stringBuilder.ToString());
            }
            if (selTile.biome.canBuildBase)
            {
                ListingStandard.LabelDouble("StoneTypesHere".Translate(), (from rt in Find.World.NaturalRockTypesIn(selTileId)
                                                                            select rt.label).ToCommaList(true).CapitalizeFirst());
            }
            ListingStandard.LabelDouble("Elevation".Translate(), selTile.elevation.ToString("F0") + "m");
            ListingStandard.GapLine();
            ListingStandard.LabelDouble("AvgTemp".Translate(), GenTemperature.GetAverageTemperatureLabel(selTileId));
            ListingStandard.LabelDouble("OutdoorGrowingPeriod".Translate(), Zone_Growing.GrowingQuadrumsDescription(selTileId));
            ListingStandard.LabelDouble("Rainfall".Translate(), selTile.rainfall.ToString("F0") + "mm");
            if (selTile.biome.foragedFood != null && selTile.biome.forageability > 0f)
            {
                ListingStandard.LabelDouble("Forageability".Translate(), selTile.biome.forageability.ToStringPercent() + " (" + selTile.biome.foragedFood.label + ")");
            }
            else
            {
                ListingStandard.LabelDouble("Forageability".Translate(), "0%");
            }
            ListingStandard.LabelDouble("AnimalsCanGrazeNow".Translate(), (!VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsNowAt(selTileId)) ? "No".Translate() : "Yes".Translate());
            ListingStandard.GapLine();
            ListingStandard.LabelDouble("AverageDiseaseFrequency".Translate(),
                $"{(60f / selTile.biome.diseaseMtbDays):F1} {"PerYear".Translate()}");
            ListingStandard.LabelDouble("TimeZone".Translate(), GenDate.TimeZoneAt(Find.WorldGrid.LongLatOf(selTileId).x).ToStringWithSign());
            var stringBuilder2 = new StringBuilder();
            var rot = Find.World.CoastDirectionAt(selTileId);
            if (rot.IsValid)
            {
                stringBuilder2.AppendWithComma(("HasCoast" + rot).Translate());
            }
            if (Find.World.HasCaves(selTileId))
            {
                stringBuilder2.AppendWithComma("HasCaves".Translate());
            }
            if (stringBuilder2.Length > 0)
            {
                ListingStandard.LabelDouble("SpecialFeatures".Translate(), stringBuilder2.ToString().CapitalizeFirst());
            }
            if (Prefs.DevMode)
            {
                ListingStandard.LabelDouble("Debug world tile ID", selTileId.ToString());
            }
        }
    }
}
