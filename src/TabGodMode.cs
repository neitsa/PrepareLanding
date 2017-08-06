using System;
using System.Text;
using PrepareLanding.Core.Gui.Tab;
using RimWorld;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using PrepareLanding.Core.Extensions;
using RimWorld.Planet;

namespace PrepareLanding
{
    public class TabGodMode : TabGuiUtility
    {
        private readonly GameData.GameData _gameData;

        private BiomeDef _chosenBiome;

        private float _chosenAverageTemperature;

        private string _chosenAverageTemperatureString;

        private Hilliness _chosenHilliness;

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
                return;
            }

            ListingStandard.LabelDouble("SelTile: ", tileId.ToString());

            if (ListingStandard.ButtonText("Debug Test"))
            {
                var tile = Find.World.grid[tileId];
                Log.Message(tile.ToString());
                Log.Message($"Outdoor Temp: {Find.World.tileTemperatures.GetOutdoorTemp(tileId)}");
                Log.Message($"Seasonal Temp: {Find.World.tileTemperatures.GetSeasonalTemp(tileId)}");
                Log.Message($"GenTemperature.GetTemperatureAtTile: {GenTemperature.GetTemperatureAtTile(tileId)}");
                var map = Current.Game.FindMap(tileId);
                if (map != null)
                {
                    map.mapTemperature.DebugLogTemps();
                }
                else
                {
                    Log.Message("Map is null");
                }

                /*
                 * setup tile
                 */
                tile.temperature = _chosenAverageTemperature;

                if (_chosenBiome != null)
                    tile.biome = _chosenBiome;

                if(_chosenHilliness != Hilliness.Undefined)
                    tile.hilliness = _chosenHilliness;

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
            DrawEntryHeader("Biome Types");

            var biomeDefs = _gameData.DefData.BiomeDefs;

            // "Select" button
            if (ListingStandard.ButtonText("Select Biome"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();

                // add a dummy 'Any' fake biome type. This sets the chosen biome to null.
                Action actionClick = delegate { _chosenBiome = null; };
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
                    actionClick = delegate { _chosenBiome = currentBiomeDef; };
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

            var rightLabel = _chosenBiome != null ? _chosenBiome.LabelCap : "Any";
            ListingStandard.LabelDouble("Biome:", rightLabel);

            var currHeightAfter = ListingStandard.CurHeight;

            // display tool-tip over label
            if (_chosenBiome != null)
            {
                var currentRect = ListingStandard.GetRect(0f);
                currentRect.height = currHeightAfter - currHeightBefore;
                if (!string.IsNullOrEmpty(_chosenBiome.description))
                    TooltipHandler.TipRegion(currentRect, _chosenBiome.description);
            }
        }

        protected void DrawTemperatureSelection()
        {
            DrawEntryHeader("Temperature");

            var goToTileOptionRectSpace = ListingStandard.GetRect(30f);
            var rects = goToTileOptionRectSpace.SplitRectWidthEvenly(2);
            Widgets.Label(rects[0], "Avg. Temp. (°C):");
            Widgets.TextFieldNumeric(rects[1], ref _chosenAverageTemperature, ref _chosenAverageTemperatureString, TemperatureTuning.MinimumTemperature, TemperatureTuning.MaximumTemperature);
        }

        protected virtual void DrawHillinessTypeSelection()
        {
            DrawEntryHeader($"{"Terrain".Translate()} Types");

            if (ListingStandard.ButtonText("Select Terrain"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (var hillinessValue in _gameData.DefData.HillinessCollection)
                {
                    var label = "Any";

                    if (hillinessValue != Hilliness.Undefined)
                        label = hillinessValue.GetLabelCap();

                    var menuOption = new FloatMenuOption(label,
                        delegate { _chosenHilliness = hillinessValue; });
                    floatMenuOptions.Add(menuOption);
                }

                var floatMenu = new FloatMenu(floatMenuOptions, "Select terrain");
                Find.WindowStack.Add(floatMenu);
            }

            // note: RimWorld logs an error when .GetLabelCap() is used on Hilliness.Undefined
            var rightLabel = _chosenHilliness != Hilliness.Undefined
                ? _chosenHilliness.GetLabelCap()
                : "Any";
            ListingStandard.LabelDouble($"{"Terrain".Translate()}:", rightLabel);
        }

        private static void LogTemperatureInfo(int tileId, int absTicks = GenDate.TicksPerHour * GenDate.GameStartHourOfDay)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("----- ** Debug Log Temp ** ------");
            var num = Find.WorldGrid.LongLatOf(tileId).y;
            stringBuilder.AppendLine("Latitude " + num);
            stringBuilder.AppendLine("-----Temperature for each hour this day------");
            stringBuilder.AppendLine("Hour    Temp    SunEffect");
            var num2 = absTicks % RimWorld.GenDate.TicksPerDay;
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
