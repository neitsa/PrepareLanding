using System;
using System.Text;
using PrepareLanding.Core.Gui.Tab;
using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PrepareLanding.Core.Extensions;
using RimWorld.Planet;

namespace PrepareLanding
{
    public class TabGodMode : TabGuiUtility
    {
        public const int MaxNumberOfRoads = 2;

        public const int MaxNumberOfRivers = 2;

        private readonly GameData.GameData _gameData;

        private string _chosenAverageTemperatureString;

        private string _chosenRainfallString;

        private string _chosenElevationString;

        private Vector2 _scrollPosRoadSelection;

        private Vector2 _scrollPosRiverSelection;

        public TabGodMode(GameData.GameData gameData, float columnSizePercent) : base(columnSizePercent)
        {
            _gameData = gameData;
        }

        /// <summary>A unique identifier for the Tab.</summary>
        public override string Id => "GodMode";

        /// <summary>The name of the tab (that is actually displayed at its top).</summary>
        public override string Name => "God Mode";

        /// <summary>Gets whether the tab can be drawn or not.</summary>
        public override bool CanBeDrawn
        {
            get { return Prefs.DevMode && DebugSettings.godMode; }
            set { }
        }

        /// <summary>Draw the content of the tab.</summary>
        /// <param name="inRect">The <see cref="T:UnityEngine.Rect" /> in which to draw the tab content.</param>
        public override void Draw(Rect inRect)
        { 
            Begin(inRect);
            DrawBiomeTypesSelection();
            DrawTemperatureSelection();
            DrawHillinessTypeSelection();
            DrawElevationSelection();
            DrawRainfallSelection();
            DrawRiverTypesSelection();
            NewColumn();
            DrawRoadTypesSelection();
            DrawDebugContent();
            End();
        }

        private void DrawDebugContent()
        {
            DrawEntryHeader("Debug", backgroundColor: ColorLibrary.RoyalPurple);

            var tileId = Find.WorldSelector.selectedTile;

            if (!Find.WorldSelector.AnyObjectOrTileSelected || tileId < 0)
            {
                var labelRect = ListingStandard.GetRect(DefaultElementHeight);
                Widgets.Label(labelRect, "Pick a tile first");
                _gameData.GodModeData.SelectedTileId = -1;
                return;
            }

            ListingStandard.LabelDouble("SelTile: ", tileId.ToString());

            if (_gameData.GodModeData.SelectedTileId != tileId)
            {
                _gameData.GodModeData.InitFromTileId(tileId);
            }

            if (ListingStandard.ButtonText("Debug Test"))
            {
                if (Find.WorldObjects.AnyFactionBaseAt(tileId))
                {
                    Messages.Message("You're not allowed to change a faction tile.", MessageSound.RejectInput);
                    _gameData.GodModeData.SelectedTileId = -1;
                    return;
                }

                var tile = Find.World.grid[tileId];
                Log.Message(tile.ToString());
                Log.Message($"Seasonal Temp: {Find.World.tileTemperatures.GetSeasonalTemp(tileId)}");
                Log.Message($"GenTemperature.GetTemperatureAtTile: {GenTemperature.GetTemperatureAtTile(tileId)}");
                var map = Current.Game.FindMap(tileId);
                if (map != null)
                {
                    Log.Message($"Outdoor Temp: {Find.World.tileTemperatures.GetOutdoorTemp(tileId)}");
                    map.mapTemperature.DebugLogTemps();
                }
                else
                {
                    Log.Message("Map is null");
                }

                /*
                 * setup tile
                 */

                if (_gameData.GodModeData.Biome != null)
                    tile.biome = _gameData.GodModeData.Biome;

                tile.temperature = _gameData.GodModeData.AverageTemperature;

                if(_gameData.GodModeData.Hilliness != Hilliness.Undefined)
                    tile.hilliness = _gameData.GodModeData.Hilliness;

                tile.elevation = _gameData.GodModeData.Elevation;

                tile.rainfall = _gameData.GodModeData.Rainfall;

                LogTemperatureInfo(tileId);
            }

            if (ListingStandard.ButtonText("Test dirtying map"))
            {
                // TODO: just check the required layer, it might save time
                // TODO: see if a long queued event is required
                Find.World.renderer.SetAllLayersDirty();
            }
        }

        protected virtual void DrawBiomeTypesSelection()  // TODO : factorize this function with the one from TabTerrain
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

            var temperatureRectSpace = ListingStandard.GetRect(30f);
            Widgets.Label(temperatureRectSpace.LeftPart(0.8f), $"Avg. Temp. (°C) [{TemperatureTuning.MinimumTemperature}, {TemperatureTuning.MaximumTemperature}]");
            Widgets.TextFieldNumeric(temperatureRectSpace.RightPart(0.2f), ref averageTemperature, ref _chosenAverageTemperatureString, TemperatureTuning.MinimumTemperature, TemperatureTuning.MaximumTemperature);
            _gameData.GodModeData.AverageTemperature = averageTemperature;
        }

        protected void DrawRainfallSelection()
        {
            DrawEntryHeader("Rainfall", backgroundColor: ColorLibrary.RoyalPurple);

            // min is obviously 0; max is defined in RimWorld.Planet.WorldGenStep_Terrain.RainfallFinishFallAltitude
            const float minRainfall = 0f;
            const float maxRainfall = 5000f;

            var rainFall = _gameData.GodModeData.Rainfall;
            _chosenRainfallString = rainFall.ToString("F0", CultureInfo.InvariantCulture);

            var rainfallRectSpace = ListingStandard.GetRect(30f);
            Widgets.Label(rainfallRectSpace.LeftPart(0.8f), $"Rainfall (mm) [{minRainfall}, {maxRainfall}]"); 
            Widgets.TextFieldNumeric(rainfallRectSpace.RightPart(0.2f), ref rainFall, ref _chosenRainfallString, minRainfall, maxRainfall);

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
            _chosenElevationString = null;

            var elevationRectSpace = ListingStandard.GetRect(30f);
            Widgets.Label(elevationRectSpace.LeftPart(0.8f), $"Elevation (m) [{minElevation}, {maxElevation}]");
            Widgets.TextFieldNumeric(elevationRectSpace.RightPart(0.2f), ref elevation, ref _chosenElevationString, minElevation, maxElevation);

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
            {
                _gameData.GodModeData.ResetSelectedRoadDefs();
            }

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
                            Messages.Message($"Can't have more than {MaxNumberOfRoads} types of road per tile.", MessageSound.RejectInput);
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
            {
                _gameData.GodModeData.ResetSelectedRiverDefs();
            }

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
                            Messages.Message($"Can't have more than {MaxNumberOfRivers} types of river per tile.", MessageSound.RejectInput);
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

        private static void LogTemperatureInfo(int tileId, int absTicks = GenDate.TicksPerHour * GenDate.GameStartHourOfDay)
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                Log.Message($"absTicks: {absTicks}");
                Log.Message($"TicksAbs: {Find.TickManager.TicksAbs}");
                Log.Message($"Num2: {Find.TickManager.TicksAbs - Find.TickManager.TicksAbs % 60000}");
            }

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("----- ** Debug Log Temp ** ------");
            var num = Find.WorldGrid.LongLatOf(tileId).y;
            stringBuilder.AppendLine("Latitude " + num);
            stringBuilder.AppendLine("-----Temperature for each hour this day------");
            stringBuilder.AppendLine("Hour    Temp    SunEffect");
            var num2 = absTicks  - absTicks % RimWorld.GenDate.TicksPerDay; // would give 0 on the 1st day
            for (var i = 0; i < 24; i++)
            {
                var absTick = num2 + i * GenDate.TicksPerHour;
                stringBuilder.Append(i.ToString().PadRight(5));
                stringBuilder.Append(Find.World.tileTemperatures.OutdoorTemperatureAt(tileId, absTick).ToString("F2").PadRight(8));
                stringBuilder.Append(GenTemperature.OffsetFromSunCycle(absTick, tileId).ToString("F2"));
                stringBuilder.AppendLine();
            }
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("-----Temperature for each twelfth this year------");
            for (var j = 0; j < 12; j++)
            {
                var twelfth = (Twelfth)j;
                var num3 = Find.World.tileTemperatures.AverageTemperatureForTwelfth(tileId, twelfth);
                stringBuilder.AppendLine(string.Concat(twelfth.GetQuadrum(), "/", twelfth.GetSeason(num), " - ", twelfth.ToString(), " ", num3.ToString("F2")));
            }
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("-----Temperature for each day this year------");
            stringBuilder.AppendLine("Tile avg: " + Find.World.grid[tileId].temperature + "°C");
            stringBuilder.AppendLine("Seasonal shift: " + GenTemperature.SeasonalShiftAmplitudeAt(tileId));
            stringBuilder.AppendLine("Equatorial distance: " + Find.WorldGrid.DistanceFromEquatorNormalized(tileId));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("Day  Lo   Hi   OffsetFromSeason RandomDailyVariation");
            for (var k = 0; k < 60; k++)
            {
                var absTick2 = (int)(k * GenDate.TicksPerDay + 15000f); // 6th hour
                var absTick3 = (int)(k * GenDate.TicksPerDay + 45000f); // 18th hour
                stringBuilder.Append(k.ToString().PadRight(8));
                stringBuilder.Append(Find.World.tileTemperatures.OutdoorTemperatureAt(tileId, absTick2).ToString("F2").PadRight(11));
                stringBuilder.Append(Find.World.tileTemperatures.OutdoorTemperatureAt(tileId, absTick3).ToString("F2").PadRight(11));
                stringBuilder.Append(GenTemperature.OffsetFromSeasonCycle(absTick3, tileId).ToString("F2").PadRight(11));
                stringBuilder.Append(Find.World.tileTemperatures.OffsetFromDailyRandomVariation(tileId, absTick3).ToString("F2"));
                stringBuilder.AppendLine();
            }
            Log.Message(stringBuilder.ToString());
        }
    }
}
