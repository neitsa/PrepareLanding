using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Core.Gui.Tab;
using PrepareLanding.GameData;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PrepareLanding
{
    public class TabGodMode : TabGuiUtility
    {
        public const int MaxNumberOfRoads = 2;

        public const int MaxNumberOfRivers = 2;

        public const int MinNumberOfStones = 2;

        public const int MaxNumberOfStones = 3;

        private readonly GameData.GameData _gameData;

        private string _chosenAverageTemperatureString;

        private string _chosenElevationString;

        private string _chosenRainfallString;

        private bool _redrawMapEnabled;

        private Vector2 _scrollPosRiverSelection;

        private Vector2 _scrollPosRoadSelection;

        private Vector2 _scrollPosStoneSelection;

        private ThingDef _selectedStoneDef;

        public TabGodMode(GameData.GameData gameData, float columnSizePercent) : base(columnSizePercent)
        {
            _gameData = gameData;

            PrepareLanding.Instance.EventHandler.WorldGeneratedOrLoaded += ExecuteOnWorldGeneratedOrLoaded;
        }

        /// <summary>Gets whether the tab can be drawn or not.</summary>
        public override bool CanBeDrawn
        {
            get { return Prefs.DevMode && DebugSettings.godMode && Current.ProgramState != ProgramState.Playing; }
            set { }
        }

        /// <summary>A unique identifier for the Tab.</summary>
        public override string Id => "GodMode";

        /// <summary>The name of the tab (that is actually displayed at its top).</summary>
        public override string Name => "God Mode";

        private void ExecuteOnWorldGeneratedOrLoaded()
        {
            _redrawMapEnabled = false;
        }

        /// <summary>Draw the content of the tab.</summary>
        /// <param name="inRect">The <see cref="T:UnityEngine.Rect" /> in which to draw the tab content.</param>
        public override void Draw(Rect inRect)
        {
            Begin(inRect);

            var tileId = Find.WorldSelector.selectedTile;
            if (tileId == -1)
            {
                NewColumn();
                DrawTileSelection();
                End();
                return;
            }

            DrawBiomeTypesSelection();
            DrawTemperatureSelection();
            DrawHillinessTypeSelection();
            DrawElevationSelection();
            DrawRainfallSelection();
#if GOD_MODE_SRR
            DrawRiverTypesSelection();
#endif
            NewColumn();
#if GOD_MODE_SRR
            DrawRoadTypesSelection();
#endif
            DrawTileSelection();
            DrawStoneTypesSelection();
            NewColumn();
            DrawTemperatureInfo();
            End();
        }

        private void DrawTileSelection()
        {
            DrawEntryHeader("Tile Setup", backgroundColor: ColorLibrary.RoyalPurple);

            var tileId = Find.WorldSelector.selectedTile;

            if (!Find.WorldSelector.AnyObjectOrTileSelected || tileId < 0)
            {
                var labelRect = ListingStandard.GetRect(DefaultElementHeight);
                Widgets.Label(labelRect, "Pick a tile on world map!");
                _gameData.GodModeData.SelectedTileId = -1;
                return;
            }

            ListingStandard.LabelDouble("Selected Tile: ", tileId.ToString());

            if (_gameData.GodModeData.SelectedTileId != tileId)
                _gameData.GodModeData.InitFromTileId(tileId);

            if (ListingStandard.ButtonText("Set Tile"))
            {
                if (Find.WorldObjects.AnyFactionBaseAt(tileId))
                {
                    Messages.Message("You're not allowed to change a faction tile.", MessageTypeDefOf.RejectInput);
                    _gameData.GodModeData.SelectedTileId = -1;
                    return;
                }

                PrepareLanding.Instance.TileFilter.ClearMatchingTiles();

                //TODO: you need to clear the temperature cache for the tile, otherwise it's wrong after being changed...
                // see RimWorld.Planet.TileTemperaturesComp.RetrieveCachedData for the cache itself. It's private...
                // but could be cleaned with RimWorld.Planet.TileTemperaturesComp.ClearCaches() but that would invalidate *all* caches...

                _redrawMapEnabled = _gameData.GodModeData.SetupTile();
            }

            var heightBefore = ListingStandard.StartCaptureHeight();
            if (ListingStandard.ButtonText("Redraw Map"))
                if (_redrawMapEnabled)
                    LongEventHandler.QueueLongEvent(delegate { Find.World.renderer.SetAllLayersDirty(); },
                        "GeneratingWorld", true, null);
                else
                    Messages.Message("You need to change a tile first to be able to redraw the map.",
                        MessageTypeDefOf.RejectInput);
            var tooltipRect = ListingStandard.EndCaptureHeight(heightBefore);
            TooltipHandler.TipRegion(tooltipRect,
                "[Warning: this redraws the map entirely; use it only when you have finished *all* your modifications.]");
        }

        protected virtual void DrawTemperatureInfo()
        {
            DrawEntryHeader("Temperature Info", backgroundColor: Color.yellow);

            /*
             * Forecast
             */
            if (ListingStandard.ButtonText("View Temperature Forecast"))
            {
                var tileId = _gameData.GodModeData.SelectedTileId;
                TabTemperature.ViewTemperatureForecast(tileId, WorldData.NowToTicks(tileId));
            }
        }

        protected virtual void DrawBiomeTypesSelection() // TODO : factorize this function with the one from TabTerrain
        {
            DrawEntryHeader("Biome Types", backgroundColor: ColorLibrary.RoyalPurple);

            var biomeDefs = _gameData.DefData.BiomeDefs;

            // "Select" button
            if (ListingStandard.ButtonText("Select Biome"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();

                // add a dummy 'Any' fake biome type. This sets the chosen biome to null.
                Action actionClick = delegate { _gameData.GodModeData.Biome = null; };
                // tool-tip when hovering above the 'Any' biome name on the floating menu
                Action mouseOverAction = delegate
                {
                    var mousePos = Event.current.mousePosition;
                    var rect = new Rect(mousePos.x, mousePos.y, DefaultElementHeight, DefaultElementHeight);

                    TooltipHandler.TipRegion(rect, "Any Biome");
                };
                var menuOption = new FloatMenuOption("Any", actionClick, MenuOptionPriority.Default, mouseOverAction);
                floatMenuOptions.Add(menuOption);

                // loop through all known biomes
                foreach (var currentBiomeDef in biomeDefs)
                {
                    // clicking on the floating menu saves the selected biome
                    actionClick = delegate { _gameData.GodModeData.Biome = currentBiomeDef; };
                    // tool-tip when hovering above the biome name on the floating menu
                    mouseOverAction = delegate
                    {
                        var mousePos = Event.current.mousePosition;
                        var rect = new Rect(mousePos.x, mousePos.y, DefaultElementHeight, DefaultElementHeight);

                        TooltipHandler.TipRegion(rect, currentBiomeDef.description);
                    };

                    //create the floating menu
                    menuOption = new FloatMenuOption(currentBiomeDef.LabelCap, actionClick, MenuOptionPriority.Default,
                        mouseOverAction);
                    // add it to the list of floating menu options
                    floatMenuOptions.Add(menuOption);
                }

                // create the floating menu
                var floatMenu = new FloatMenu(floatMenuOptions, "Select Biome Type");

                // add it to the window stack to display it
                Find.WindowStack.Add(floatMenu);
            }

            var currHeightBefore = ListingStandard.CurHeight;

            var rightLabel = _gameData.GodModeData.Biome != null ? _gameData.GodModeData.Biome.LabelCap : "Any";
            ListingStandard.LabelDouble("Biome:", rightLabel);

            var currHeightAfter = ListingStandard.CurHeight;

            // display tool-tip over label
            if (_gameData.GodModeData.Biome != null)
            {
                var currentRect = ListingStandard.GetRect(0f);
                currentRect.height = currHeightAfter - currHeightBefore;
                if (!string.IsNullOrEmpty(_gameData.GodModeData.Biome.description))
                    TooltipHandler.TipRegion(currentRect, _gameData.GodModeData.Biome.description);
            }
        }

        protected void DrawTemperatureSelection()
        {
            DrawEntryHeader("Temperature", backgroundColor: ColorLibrary.RoyalPurple);

            var averageTemperature = _gameData.GodModeData.AverageTemperature;
            _chosenAverageTemperatureString = averageTemperature.ToString("F1", CultureInfo.InvariantCulture);

            var temperatureRectSpace = ListingStandard.GetRect(DefaultElementHeight);
            Widgets.Label(temperatureRectSpace.LeftPart(0.8f),
                $"Avg. Temp. (°C) [{TemperatureTuning.MinimumTemperature}, {TemperatureTuning.MaximumTemperature}]");
            Core.Gui.Widgets.TextFieldNumeric(temperatureRectSpace.RightPart(0.2f), ref averageTemperature,
                ref _chosenAverageTemperatureString, TemperatureTuning.MinimumTemperature,
                TemperatureTuning.MaximumTemperature);

            _gameData.GodModeData.AverageTemperature = averageTemperature;
        }

        protected void DrawRainfallSelection()
        {
            DrawEntryHeader("Rainfall", backgroundColor: ColorLibrary.RoyalPurple);

            // min is obviously 0; max is defined in RimWorld.Planet.WorldGenStep_Terrain.RainfallFinishFallAltitude
            const float minRainfall = 0f;
            const float maxRainfall = 5000f;

            var rainFall = _gameData.GodModeData.Rainfall;
            _chosenRainfallString = rainFall.ToString("F1", CultureInfo.InvariantCulture);

            var rainfallRectSpace = ListingStandard.GetRect(DefaultElementHeight);
            Widgets.Label(rainfallRectSpace.LeftPart(0.8f), $"Rainfall (mm) [{minRainfall}, {maxRainfall}]");
            Core.Gui.Widgets.TextFieldNumeric(rainfallRectSpace.RightPart(0.2f), ref rainFall,
                ref _chosenRainfallString, minRainfall, maxRainfall);

            _gameData.GodModeData.Rainfall = rainFall;
        }

        protected void DrawElevationSelection()
        {
            DrawEntryHeader("Elevation", backgroundColor: ColorLibrary.RoyalPurple);

            // see RimWorld.Planet.WorldGenStep_Terrain.ElevationRange for min / max
            // max is also defined in RimWorld.Planet.WorldMaterials.ElevationMax
            const float minElevation = -500f;
            const float maxElevation = 5000f;

            var elevation = _gameData.GodModeData.Elevation;
            _chosenElevationString = elevation.ToString("F1", CultureInfo.InvariantCulture);

            var elevationRectSpace = ListingStandard.GetRect(DefaultElementHeight);
            Widgets.Label(elevationRectSpace.LeftPart(0.8f), $"Elevation (m) [{minElevation}, {maxElevation}]");
            Core.Gui.Widgets.TextFieldNumeric(elevationRectSpace.RightPart(0.2f), ref elevation,
                ref _chosenElevationString, minElevation, maxElevation);

            _gameData.GodModeData.Elevation = elevation;
        }

        protected virtual void DrawHillinessTypeSelection()
        {
            DrawEntryHeader("Terrain Types", backgroundColor: ColorLibrary.RoyalPurple);

            if (ListingStandard.ButtonText("Select Terrain"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (var hillinessValue in _gameData.DefData.HillinessCollection)
                {
                    var label = "Any";

                    if (hillinessValue != Hilliness.Undefined)
                        label = hillinessValue.GetLabelCap();

                    var menuOption = new FloatMenuOption(label,
                        delegate { _gameData.GodModeData.Hilliness = hillinessValue; });
                    floatMenuOptions.Add(menuOption);
                }

                var floatMenu = new FloatMenu(floatMenuOptions, "Select terrain");
                Find.WindowStack.Add(floatMenu);
            }

            // note: RimWorld logs an error when .GetLabelCap() is used on Hilliness.Undefined
            var rightLabel = _gameData.GodModeData.Hilliness != Hilliness.Undefined
                ? _gameData.GodModeData.Hilliness.GetLabelCap()
                : "Any";
            ListingStandard.LabelDouble($"{"Terrain".Translate()}:", rightLabel);
        }

        protected virtual void DrawRoadTypesSelection()
        {
            DrawEntryHeader("Road Types", backgroundColor: ColorLibrary.RoyalPurple);

            var roadDefs = _gameData.DefData.RoadDefs;
            var selectedRoadDefs = _gameData.GodModeData.SelectedRoadDefs;

            if (ListingStandard.ButtonText("Reset"))
                _gameData.GodModeData.ResetSelectedRoadDefs();

            /*
             * ScrollView
             */

            var scrollViewHeight = roadDefs.Count * DefaultElementHeight;
            var inLs = ListingStandard.BeginScrollView(5 * DefaultElementHeight, scrollViewHeight,
                ref _scrollPosRoadSelection, DefaultScrollableViewShrinkWidth);

            // display road elements
            foreach (var roadDef in roadDefs)
            {
                // save temporary state as it might change in CheckBoxLabeledMulti
                var tmpState = selectedRoadDefs[roadDef];

                var itemRect = inLs.GetRect(DefaultElementHeight);
                Widgets.CheckboxLabeled(itemRect, roadDef.LabelCap, ref tmpState);

                // if the state changed, update the item with the new state
                if (tmpState != selectedRoadDefs[roadDef])
                {
                    if (tmpState)
                    {
                        var countTrue = selectedRoadDefs.Values.Count(selectedRoadDefValue => selectedRoadDefValue);
                        if (countTrue >= MaxNumberOfRoads)
                        {
                            Messages.Message($"Can't have more than {MaxNumberOfRoads} types of road per tile.",
                                MessageTypeDefOf.RejectInput);
                            tmpState = false;
                        }
                    }

                    selectedRoadDefs[roadDef] = tmpState;
                }

                if (!string.IsNullOrEmpty(roadDef.description))
                    TooltipHandler.TipRegion(itemRect, roadDef.description);
            }

            ListingStandard.EndScrollView(inLs);
        }

        protected virtual void DrawRiverTypesSelection()
        {
            DrawEntryHeader("River Types", backgroundColor: ColorLibrary.RoyalPurple);

            var riverDefs = _gameData.DefData.RiverDefs;
            var selectedRiverDefs = _gameData.GodModeData.SelectedRiverDefs;

            if (ListingStandard.ButtonText("Reset"))
                _gameData.GodModeData.ResetSelectedRiverDefs();

            /*
             * ScrollView
             */

            var scrollViewHeight = riverDefs.Count * DefaultElementHeight;
            var inLs = ListingStandard.BeginScrollView(5 * DefaultElementHeight, scrollViewHeight,
                ref _scrollPosRiverSelection, DefaultScrollableViewShrinkWidth);

            // display road elements
            foreach (var riverDef in riverDefs)
            {
                // save temporary state as it might change in CheckBoxLabeledMulti
                var tmpState = selectedRiverDefs[riverDef];

                var itemRect = inLs.GetRect(DefaultElementHeight);
                Widgets.CheckboxLabeled(itemRect, riverDef.LabelCap, ref tmpState);

                // if the state changed, update the item with the new state
                if (tmpState != selectedRiverDefs[riverDef])
                {
                    if (tmpState)
                    {
                        var countTrue = selectedRiverDefs.Values.Count(selectedRiverDefValue => selectedRiverDefValue);
                        if (countTrue >= MaxNumberOfRivers)
                        {
                            Messages.Message($"Can't have more than {MaxNumberOfRivers} types of river per tile.",
                                MessageTypeDefOf.RejectInput);
                            tmpState = false;
                        }
                    }

                    selectedRiverDefs[riverDef] = tmpState;
                }

                if (!string.IsNullOrEmpty(riverDef.description))
                    TooltipHandler.TipRegion(itemRect, riverDef.description);
            }

            ListingStandard.EndScrollView(inLs);
        }

        protected virtual void DrawStoneTypesSelection()
        {
            DrawEntryHeader("StoneTypesHere".Translate(), backgroundColor: ColorFromFilterSubjectThingDef("Stones"));

            var selectedStoneDefs = _gameData.GodModeData.SelectedStoneDefs;
            var orderedStoneDefs = _gameData.GodModeData.OrderedStoneDefs;

            // Reset button: reset all entries to Off state
            if (ListingStandard.ButtonText("Reset"))
                _gameData.GodModeData.ResetSelectedStoneDefs();

            // re-orderable list group
            var reorderableGroup = ReorderableWidget.NewGroup(delegate(int from, int to)
            {
                orderedStoneDefs.ReorderElements(from, to);
                SoundDefOf.TickHigh.PlayOneShotOnCamera();
            });

            var maxNumStones = (InRect.height - ListingStandard.CurHeight - DefaultGapLineHeight -
                                DefaultElementHeight - 15f) / DefaultElementHeight;
            var maxHeight = maxNumStones * DefaultElementHeight;
            var height = Mathf.Min(selectedStoneDefs.Count * DefaultElementHeight, maxHeight);

            // stone types, standard selection

            var inLs = ListingStandard.BeginScrollView(height, selectedStoneDefs.Count * DefaultElementHeight,
                ref _scrollPosStoneSelection, DefaultScrollableViewShrinkWidth);

            foreach (var currentOrderedStoneDef in orderedStoneDefs)
            {
                var itemRect = inLs.GetRect(DefaultElementHeight);


                var tmpState = selectedStoneDefs[currentOrderedStoneDef];
                var selected = currentOrderedStoneDef == _selectedStoneDef;

                if (Widgets.CheckboxLabeledSelectable(itemRect, currentOrderedStoneDef.LabelCap, ref selected,
                    ref tmpState))
                    _selectedStoneDef = currentOrderedStoneDef;

                // if the state changed, update the item with the new state
                if (tmpState != selectedStoneDefs[currentOrderedStoneDef])
                    selectedStoneDefs[currentOrderedStoneDef] = tmpState;

                ReorderableWidget.Reorderable(reorderableGroup, itemRect);

                if (!string.IsNullOrEmpty(currentOrderedStoneDef.description))
                    TooltipHandler.TipRegion(itemRect, currentOrderedStoneDef.description);
            }

            ListingStandard.EndScrollView(inLs);
        }
    }
}