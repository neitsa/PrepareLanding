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
        private int _dateTicks = 1;

        private int _dayOfQuadrum;
        private string _dayOfQuadrumString;

        private int _numberOfTilesForFeature = 1;

        private string _numberOfTilesForFeatureString;
        private Quadrum _quadrum;

        private int _selectedTileIdForTemperatureForecast = -1;
        private int _year;
        private string _yearString;

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
            DrawAnimalsCanGrazeNowSelection();
            NewColumn();
            DrawTemperatureForecast();
            End();
        }

        private void DrawTemperatureForecast()
        {
            DrawEntryHeader("Temperature Forecast", backgroundColor: Color.magenta);

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

            ListingStandard.GapLine(DefaultGapLineHeight);

            /*
             * Day / Quadrum / Year selector
             */
            var backupAnchor = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleLeft;

            // day
            var daySelector = ListingStandard.GetRect(30f);
            var dayLabelRect = daySelector.LeftPart(0.70f);
            var dayFieldRect = daySelector.RightPart(0.30f);
            Widgets.Label(dayLabelRect, "Quadrum Day [1, 15]: ");
            Widgets.TextFieldNumeric(dayFieldRect, ref _dayOfQuadrum, ref _dayOfQuadrumString, 1,
                GenDate.DaysPerQuadrum);

            ListingStandard.Gap(6f);

            // quadrum
            var quadrumRect = ListingStandard.GetRect(30f);
            var quadrumButtonRect = quadrumRect.LeftHalf();
            if (Widgets.ButtonText(quadrumButtonRect, "Select Quadrum"))
            {
                // get all possible enumeration values for hilliness
                var quadrumList = Enum.GetValues(typeof(Quadrum)).Cast<Quadrum>().ToList();

                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (var quadrum in quadrumList)
                {
                    if (quadrum == Quadrum.Undefined)
                        continue;

                    var label = quadrum.Label();

                    var menuOption = new FloatMenuOption(label,
                        delegate { _quadrum = quadrum; });
                    floatMenuOptions.Add(menuOption);
                }

                var floatMenu = new FloatMenu(floatMenuOptions, "Select Quadrum");
                Find.WindowStack.Add(floatMenu);
            }
            var quadrumLabelRect = quadrumRect.RightHalf();
            Widgets.Label(quadrumLabelRect, _quadrum.ToString());

            ListingStandard.Gap(6f);

            // year
            var yearSelector = ListingStandard.GetRect(30f);
            var yearLabelRect = yearSelector.LeftPart(0.7f);
            var yearFieldRect = yearSelector.RightPart(0.3f);
            Widgets.Label(yearLabelRect, $"Year [{GenDate.DefaultStartingYear}, {GenDate.DefaultStartingYear + 50}]: ");
            Widgets.TextFieldNumeric(yearFieldRect, ref _year, ref _yearString, GenDate.DefaultStartingYear,
                GenDate.DefaultStartingYear + 50);

            // translate day, quadrum and year to ticks
            _dateTicks = WorldData.DateToTicks(_dayOfQuadrum - 1, _quadrum, _year);

            // date display
            var dateNowRect = ListingStandard.GetRect(30f);
            var labelDateLeftRect = dateNowRect.LeftPart(0.20f);
            Widgets.Label(labelDateLeftRect, "Date: ");
            var labelDateRightRect = dateNowRect.RightPart(0.60f);
            var dateString = GenDate.DateReadoutStringAt(_dateTicks,
                Find.WorldGrid.LongLatOf(_selectedTileIdForTemperatureForecast));
            Widgets.Label(labelDateRightRect, dateString);

            Text.Anchor = backupAnchor;

            ListingStandard.GapLine(DefaultGapLineHeight);

            /*
             * Forecast
             */
            if (ListingStandard.ButtonText("View Temperature Forecast"))
            {
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
                    new TableView<TemperatureForecastForTwelfth>("Forecast For Twelves", tempsForTwelves,
                        twelvesGetters);

                /*
                 * Forecast for hours of day
                 */
                var temperaturesForHoursOfDay =
                    TemperatureData.TemperaturesForDay(_selectedTileIdForTemperatureForecast, _dateTicks);
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
                    new TableView<TemperatureForecastForDay>($"Forecast for {GenDate.HoursPerDay} Hours [{dateString}]",
                        temperaturesForHoursOfDay,
                        temperaturesForHoursGetters);

                /*
                 * Forecast for days or year
                 */
                var tempsForDaysOfYear =
                    TemperatureData.TemperaturesForYear(_selectedTileIdForTemperatureForecast, _dateTicks);
                var temperaturesForDaysOfYearGetters = new List<ColumnData<TemperatureForecastForYear>>
                {
                    new ColumnData<TemperatureForecastForYear>("Day", "Day of Year", tffy => $"{tffy.Day}"),
                    new ColumnData<TemperatureForecastForYear>("Min", "Minimum Temperature of Day",
                        tffy => $"{tffy.MinTemp:F2}"),
                    new ColumnData<TemperatureForecastForYear>("Max", "Maximum Temperature of Day",
                        tffy => $"{tffy.MaxTemp:F2}"),
                    new ColumnData<TemperatureForecastForYear>("OffSeason", "Offset from Season Average",
                        tffy => $"{tffy.OffsetFromSeasonCycle:F2}"),
                    new ColumnData<TemperatureForecastForYear>("OffDRV", "Offset from Daily Random Variation",
                        tffy => $"{tffy.OffsetFromDailyRandomVariation:F2}")
                };
                var tableViewTempForYear =
                    new TableView<TemperatureForecastForYear>("Forecast for Next Year", tempsForDaysOfYear,
                        temperaturesForDaysOfYearGetters);

                /*
                 * Window and views
                 */
                var temperatureWindow = new TableWindow(_selectedTileIdForTemperatureForecast, _dateTicks, 0.33f);

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
            _numberOfTilesForFeature = _gameData.UserData.MostLeastItem.NumberOfItems;
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