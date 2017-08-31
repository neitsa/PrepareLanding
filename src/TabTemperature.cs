using System;
using System.Collections.Generic;
using System.Linq;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Core.Gui.Tab;
using PrepareLanding.Core.Gui.Window;
using PrepareLanding.GameData;
using RimWorld;
using UnityEngine;
using Verse;

namespace PrepareLanding
{
    public class TabTemperature : TabGuiUtility
    {
        private readonly GameData.GameData _gameData;

        private int _numberOfTilesForFeature = 1;

        private string _numberOfTilesForFeatureString;

        private int _selectedTileIdForTemperatureForecast = -1;

        public TabTemperature(GameData.GameData gameData, float columnSizePercent = 0.25f) :
            base(columnSizePercent)
        {
            _gameData = gameData;
        }

        /// <summary>Gets whether the tab can be draw or not.</summary>
        public override bool CanBeDrawn { get; set; } = true;

        /// <summary>
        ///     A unique identifier for the Tab.
        /// </summary>
        public override string Id => Name;

        /// <summary>
        ///     The name of the tab (that is actually displayed at its top).
        /// </summary>
        public override string Name => "Temperature";

        /// <summary>
        ///     Draw the actual content of this window.
        /// </summary>
        /// <param name="inRect">The <see cref="Rect" /> inside which to draw.</param>
        public override void Draw(Rect inRect)
        {
            Begin(inRect);
            DrawTemperaturesSelection();
            DrawGrowingPeriodSelection();
            NewColumn();
            DrawRainfallSelection();
            DrawMostLeastFeatureSelection();

            // "Animals Can Graze Now" relies on game ticks as VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsNowAt calls
            //   GenTemperature.GetTemperatureAtTile which calls GenTemperature.GetTemperatureFromSeasonAtTile
            //  This last function takes GenTicks.TicksAbs as argument but the results are not consistent between calls...
            // All in all: better not calling it during the "select landing site" page.
            if (GenScene.InPlayScene)
                DrawAnimalsCanGrazeNowSelection();

            DrawTemperatureForecast();
            End();
        }

        private void DrawTemperatureForecast()
        {
            DrawEntryHeader("Temperature Forecast", backgroundColor: ColorLibrary.GrassGreen);

            var tileId = Find.WorldSelector.selectedTile;

            if (!Find.WorldSelector.AnyObjectOrTileSelected || tileId < 0)
            {
                var labelRect = ListingStandard.GetRect(DefaultElementHeight);
                Widgets.Label(labelRect, "Pick a tile on world map!");
                _selectedTileIdForTemperatureForecast = -1;
                return;
            }

            ListingStandard.LabelDouble("Selected Tile: ", tileId.ToString());
            if (_selectedTileIdForTemperatureForecast != tileId)
                _selectedTileIdForTemperatureForecast = tileId;

            if (ListingStandard.ButtonText("View"))
            {
                //TODO: change ticks to a selected user time

                /*
                 * Forecast for hours of day
                 */
                var temperaturesForHoursOfDay =
                    TemperatureData.TemperaturesForDay(_selectedTileIdForTemperatureForecast, 1);
                var temperaturesForHoursGetters = new List<ColumnData<TemperatureForecastForDay>>
                {
                    new ColumnData<TemperatureForecastForDay>("Hour", "Hour of Day", tffd => $"{tffd.Hour}"),
                    new ColumnData<TemperatureForecastForDay>("Temp", "Outdoor Temperature",
                        tffd => $"{tffd.OutdoorTemperature:F1}"),
                    //new ColumnData<TemperatureForecastForDay>("RandomVar", "Daily Random Variation", tffd => $"{tffd.DailyRandomVariation:F1}"),
                    new ColumnData<TemperatureForecastForDay>("OffDRV", "Offset from Daily Random Variation",
                        tffd => $"{tffd.OffsetFromDailyRandomVariation:F1}"),
                    new ColumnData<TemperatureForecastForDay>("OffSeason", "Offset from Season Average",
                        tffd => $"{tffd.OffsetFromSeasonCycle:F1}"),
                    new ColumnData<TemperatureForecastForDay>("SunEff", "Offset from Sun Cycle",
                        tffd => $"{tffd.OffsetFromSunCycle:F1}")
                };
                var tableViewTempForDay =
                    new TableView<TemperatureForecastForDay>(temperaturesForHoursOfDay, temperaturesForHoursGetters);

                /*
                 * Forecast for twelves of year.
                 */
                var tempsForTwelves = TemperatureData.TemperaturesForTwelfth(_selectedTileIdForTemperatureForecast);
                var twelvesGetters = new List<ColumnData<TemperatureForecastForTwelfth>>
                {
                    new ColumnData<TemperatureForecastForTwelfth>("Quadrum", "Quadrum of Year",
                        tfft => $"{tfft.Twelfth.GetQuadrum()}"),
                    new ColumnData<TemperatureForecastForTwelfth>("Season", "Season of Year",
                        tfft => $"{tfft.Twelfth.GetSeason(tfft.Latitude)}"),
                    new ColumnData<TemperatureForecastForTwelfth>("Twelfth", "Twelfth of Year",
                        tfft => $"{tfft.Twelfth}"),
                    new ColumnData<TemperatureForecastForTwelfth>("Avg. Temp", "Average Temperature for Twelfth",
                        tfft => $"{tfft.AverageTemperatureForTwelfth:F2}")
                };
                var tableViewTempForTwelves =
                    new TableView<TemperatureForecastForTwelfth>(tempsForTwelves, twelvesGetters);

                /*
                 * Forecast for days or year
                 */
                var tempsForDaysOfYear = TemperatureData.TemperaturesForYear(_selectedTileIdForTemperatureForecast, 1);
                var temperaturesForDaysOfYearGetters = new List<ColumnData<TemperatureForecastForYear>>
                {
                    new ColumnData<TemperatureForecastForYear>("Day", "Day of Year", tffy => $"{tffy.Day}"),
                    new ColumnData<TemperatureForecastForYear>("Min", "MinimumTemperature of Day",
                        tffy => $"{tffy.MinTemp:F2}"),
                    new ColumnData<TemperatureForecastForYear>("Max", "Maximum Temperature of Day",
                        tffy => $"{tffy.MaxTemp:F2}"),
                    new ColumnData<TemperatureForecastForYear>("OffSeason", "Offset from Season Average",
                        tffy => $"{tffy.OffsetFromSeasonCycle:F2}"),
                    new ColumnData<TemperatureForecastForYear>("OffDRV", "Offset from Daily Random Variation",
                        tffy => $"{tffy.OffsetFromDailyRandomVariation:F2}")
                };
                var tableViewTempForYear =
                    new TableView<TemperatureForecastForYear>(tempsForDaysOfYear, temperaturesForDaysOfYearGetters);

                /*
                 * Window and views
                 */
                var temperatureWindow = new TableWindow(0.33f);

                temperatureWindow.ClearTables();
                temperatureWindow.AddTable(tableViewTempForDay);
                temperatureWindow.AddTable(tableViewTempForTwelves);
                temperatureWindow.AddTable(tableViewTempForYear);

                Find.WindowStack.Add(temperatureWindow);
            }
        }

        private void DrawAnimalsCanGrazeNowSelection()
        {
            DrawEntryHeader("Animals", backgroundColor: ColorFromFilterSubjectThingDef("Animals Can Graze Now"));

            var rect = ListingStandard.GetRect(DefaultElementHeight);
            var tmpCheckState = _gameData.UserData.ChosenAnimalsCanGrazeNowState;
            Core.Gui.Widgets.CheckBoxLabeledMulti(rect, "Animals Can Graze Now:", ref tmpCheckState);

            _gameData.UserData.ChosenAnimalsCanGrazeNowState = tmpCheckState;
        }

        private void DrawGrowingPeriodSelection()
        {
            const string label = "Growing Period";
            DrawEntryHeader($"{label} (days)", backgroundColor: ColorFromFilterSubjectThingDef("Growing Periods"));

            var boundField = _gameData.UserData.GrowingPeriod;

            var tmpCheckedOn = boundField.Use;

            ListingStandard.Gap();
            ListingStandard.CheckboxLabeled(label, ref tmpCheckedOn, $"Use Min/Max {label}");
            boundField.Use = tmpCheckedOn;

            // MIN

            if (ListingStandard.ButtonText($"Min {label}"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (var growingTwelfth in boundField.Options)
                {
                    var menuOption = new FloatMenuOption(growingTwelfth.GrowingDaysString(),
                        delegate { boundField.Min = growingTwelfth; });
                    floatMenuOptions.Add(menuOption);
                }

                var floatMenu = new FloatMenu(floatMenuOptions, $"Select {label}");
                Find.WindowStack.Add(floatMenu);
            }

            ListingStandard.LabelDouble($"Min. {label}:", boundField.Min.GrowingDaysString());

            // MAX

            if (ListingStandard.ButtonText($"Max {label}"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (var growingTwelfth in boundField.Options)
                {
                    var menuOption = new FloatMenuOption(growingTwelfth.GrowingDaysString(),
                        delegate { boundField.Max = growingTwelfth; });
                    floatMenuOptions.Add(menuOption);
                }

                var floatMenu = new FloatMenu(floatMenuOptions, $"Select {label}");
                Find.WindowStack.Add(floatMenu);
            }

            ListingStandard.LabelDouble($"Max. {label}:", boundField.Max.GrowingDaysString());
        }

        private void DrawRainfallSelection()
        {
            // Max rainfall is defined as private in RimWorld.Planet.WorldMaterials.RainfallMax
            const float rainfallMin = 0f;
            const float rainfallMax = 5000f;

            DrawEntryHeader($"Rain Fall (mm) [{rainfallMin}, {rainfallMax}]",
                backgroundColor: ColorFromFilterSubjectThingDef("Rain Falls"));

            DrawUsableMinMaxNumericField(_gameData.UserData.RainFall, "Rain Fall", rainfallMin, rainfallMax);
        }

        private void DrawTemperaturesSelection()
        {
            const float tempMin = TemperatureTuning.MinimumTemperature;
            const float tempMax = TemperatureTuning.MaximumTemperature;

            DrawEntryHeader($"Temperatures (°C) [{tempMin}, {tempMax}]",
                backgroundColor: ColorFromFilterSubjectThingDef("Average Temperatures"));

            DrawUsableMinMaxNumericField(_gameData.UserData.AverageTemperature, "Average Temperature",
                tempMin, tempMax);
            DrawUsableMinMaxNumericField(_gameData.UserData.WinterTemperature, "Winter Temperature",
                tempMin, tempMax);
            DrawUsableMinMaxNumericField(_gameData.UserData.SummerTemperature, "Summer Temperature",
                tempMin, tempMax);
        }

        private void DrawMostLeastFeatureSelection()
        {
            DrawEntryHeader("Most / Least Feature", backgroundColor: ColorFromFilterSubjectThingDef("Biomes"));

            /*
             * Select Feature
             */
            const string selectFeature = "Select Feature";

            if (ListingStandard.ButtonText(selectFeature))
            {
                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (var feature in Enum.GetValues(typeof(MostLeastFeature)).Cast<MostLeastFeature>())
                {
                    var menuOption = new FloatMenuOption(feature.ToString(),
                        delegate { _gameData.UserData.MostLeastItem.Feature = feature; });
                    floatMenuOptions.Add(menuOption);
                }

                var floatMenu = new FloatMenu(floatMenuOptions, selectFeature);
                Find.WindowStack.Add(floatMenu);
            }

            /*
             * Number of tiles to select.
             */
            ListingStandard.GapLine(DefaultGapLineHeight);

            var tilesNumberRect = ListingStandard.GetRect(DefaultElementHeight);
            var leftRect = tilesNumberRect.LeftPart(0.80f);
            var rightRect = tilesNumberRect.RightPart(0.20f);

            Widgets.Label(leftRect, "Number of Tiles [1, 10000]:");
            Widgets.TextFieldNumeric(rightRect, ref _numberOfTilesForFeature, ref _numberOfTilesForFeatureString,
                1, 10000);
            _gameData.UserData.MostLeastItem.NumberOfItems = _numberOfTilesForFeature;

            /*
             * Select Feature Type (most / least)
             */

            const string selectFeatureType = "Select Feature Type";

            if (ListingStandard.ButtonText(selectFeatureType))
            {
                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (var featureType in Enum.GetValues(typeof(MostLeastType)).Cast<MostLeastType>())
                {
                    var menuOption = new FloatMenuOption(featureType.ToString(),
                        delegate { _gameData.UserData.MostLeastItem.FeatureType = featureType; });
                    floatMenuOptions.Add(menuOption);
                }

                var floatMenu = new FloatMenu(floatMenuOptions, selectFeatureType);
                Find.WindowStack.Add(floatMenu);
            }

            /*
             * Result label
             */
            string text;
            if (_gameData.UserData.MostLeastItem.Feature == MostLeastFeature.None)
            {
                text = $"Push \"{selectFeature}\" button first.";
            }
            else if (_gameData.UserData.MostLeastItem.FeatureType == MostLeastType.None)
            {
                text = $"Now use the \"{selectFeatureType}\" button.";
            }
            else
            {
                var highestLowest = _gameData.UserData.MostLeastItem.FeatureType == MostLeastType.Most
                    ? "highest"
                    : "lowest";
                var tiles = _gameData.UserData.MostLeastItem.NumberOfItems > 1 ? "tiles" : "tile";
                text =
                    $"Selecting {_gameData.UserData.MostLeastItem.NumberOfItems} {tiles} with the {highestLowest} {_gameData.UserData.MostLeastItem.Feature}";
            }

            ListingStandard.Label($"Result: {text}", DefaultElementHeight * 2);
        }
    }
}