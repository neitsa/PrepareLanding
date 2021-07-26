using System;
using System.Collections.Generic;
using System.Linq;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Core.Gui.Tab;
using PrepareLanding.Core.Gui.Window;
using PrepareLanding.Filters;
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

        private int _numberOfTilesForCharacteristic = 1;

        private string _numberOfTilesForCharacteristicString;
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
        public override string Id => "Temperature";

        /// <summary>
        ///     The name of the tab (that is actually displayed at its top).
        /// </summary>
        public override string Name => "PLMWT2T_TabName".Translate();

        /// <summary>
        ///     Draw the actual content of this window.
        /// </summary>
        /// <param name="inRect">The <see cref="Rect" /> inside which to draw.</param>
        public override void Draw(Rect inRect)
        {
            Begin(inRect);
            DrawMostLeastCharacteristicSelection();
            DrawCaveSelection();
            DrawFeatureSelection();
            DrawOpenCoordinatesWindow();
            NewColumn(true);
            DrawTemperaturesSelection();
            DrawGrowingPeriodSelection();
            NewColumn();
            DrawRainfallSelection();
            DrawAnimalsCanGrazeNowSelection();
            DrawTemperatureForecast();
            End();
        }

        private void DrawFeatureSelection()
        {
            DrawEntryHeader("PLMWT2T_WorldFeatureSelection".Translate(),
                backgroundColor: ColorFromFilterType(typeof(TileFilterWorldFeature)));

            var features = _gameData.WorldData.WorldFeatures;

            // "Select" button
            if (ListingStandard.ButtonText("PLMWT2T_SelectWorldFeature".Translate()))
            {
                var floatMenuOptions = new List<FloatMenuOption>();

                // add a dummy 'Any' fake feature type. This sets the chosen feature to null.
                Action actionClick = delegate { _gameData.UserData.WorldFeature = null; };
                // tool-tip when hovering above the 'Any' feature name on the floating menu
                void MouseOverAction(Rect r)
                {
                    var mousePos = Event.current.mousePosition;
                    var rect = new Rect(mousePos.x, mousePos.y, DefaultElementHeight, DefaultElementHeight);

                    TooltipHandler.TipRegion(rect, "PLMWT2T_AnyWorldFeature".Translate());
                }

                var menuOption = new FloatMenuOption("PLMW_SelectAny".Translate(), actionClick, MenuOptionPriority.Default, MouseOverAction);
                floatMenuOptions.Add(menuOption);

                // loop through all known feature
                foreach (var currentFeature in features)
                {
                    // do not allow ocean and lakes
                    if (currentFeature.def.rootBiomes.Count != 0)
                    {
                        if (currentFeature.def.rootBiomes.Contains(BiomeDefOf.Ocean) ||
                            currentFeature.def.rootBiomes.Contains(BiomeDefOf.Lake))
                            continue;
                    }
                    // TODO: handle other water bodies, you'll need to parse the def name as there are no other ways
                    // see \Mods\Core\Defs\Misc\FeatureDefs\Features.xml
                    // or another solution would be to patch the definition (e.g. OuterOcean) to have a root biome as "Ocean" (or lake or whatever water body).
                    //if(currentFeature.def.defName contains "Ocean")

                    // clicking on the floating menu saves the selected feature
                    actionClick = delegate { _gameData.UserData.WorldFeature = currentFeature; };

                    //create the floating menu
                    menuOption = new FloatMenuOption(currentFeature.name, actionClick);
                    // add it to the list of floating menu options
                    floatMenuOptions.Add(menuOption);
                }

                // create the floating menu
                var floatMenu = new FloatMenu(floatMenuOptions, "PLMWT2T_SelectWorldFeature".Translate());

                // add it to the window stack to display it
                Find.WindowStack.Add(floatMenu);
            }

            var currHeightBefore = ListingStandard.CurHeight;

            var rightLabel = _gameData.UserData.WorldFeature != null ? _gameData.UserData.WorldFeature.name : (string)"PLMW_SelectAny".Translate();
            ListingStandard.LabelDouble($"{"PLMWT2T_WorldFeature".Translate()}:", rightLabel);

            var currHeightAfter = ListingStandard.CurHeight;

            // display tool-tip over label
            if (_gameData.UserData.WorldFeature != null)
            {
                var currentRect = ListingStandard.GetRect(0f);
                currentRect.height = currHeightAfter - currHeightBefore;
                if (!string.IsNullOrEmpty(_gameData.UserData.WorldFeature.name))
                    TooltipHandler.TipRegion(currentRect, _gameData.UserData.WorldFeature.name);
            }
        }

        private void DrawOpenCoordinatesWindow()
        {
            DrawEntryHeader("PLMWT2T_CoordinatesWindow".Translate(), backgroundColor: Color.magenta);

            if (!ListingStandard.ButtonText("PLMWT2T_OpenCoordinatesWindow".Translate()))
                return;

            if (Coordinates.MainWindow.IsInWindowStack)
                return;

            if (!Coordinates.MainWindow.CanBeDisplayed)
                return;

            var coordinatesWindow = new Coordinates.MainWindow();
            Find.WindowStack.Add(coordinatesWindow);
        }

        private void DrawTemperatureForecast()
        {
            DrawEntryHeader("PLMWT2T_TemperatureForecast".Translate(), backgroundColor: Color.magenta);

            var tileId = Find.WorldSelector.selectedTile;

            if (!Find.WorldSelector.AnyObjectOrTileSelected || tileId < 0)
            {
                var labelRect = ListingStandard.GetRect(DefaultElementHeight);
                Widgets.Label(labelRect, "PLMWT2T_TempPickTileOnWorldMap".Translate());
                _selectedTileIdForTemperatureForecast = -1;
                return;
            }

            ListingStandard.LabelDouble($"{"PLMWT2T_TempSelectedTile".Translate()}: ", tileId.ToString());
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
            Widgets.Label(dayLabelRect, $"{"PLMWT2T_QuadrumDay".Translate()} [1, 15]: ");
            Widgets.TextFieldNumeric(dayFieldRect, ref _dayOfQuadrum, ref _dayOfQuadrumString, 1,
                GenDate.DaysPerQuadrum);

            ListingStandard.Gap(6f);

            // quadrum
            var quadrumRect = ListingStandard.GetRect(30f);
            var quadrumButtonRect = quadrumRect.LeftHalf();
            if (Widgets.ButtonText(quadrumButtonRect, "PLMWT2T_SelectQuadrum".Translate()))
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

                var floatMenu = new FloatMenu(floatMenuOptions, "PLMWT2T_SelectQuadrum".Translate());
                Find.WindowStack.Add(floatMenu);
            }
            var quadrumLabelRect = quadrumRect.RightHalf();
            Widgets.Label(quadrumLabelRect, _quadrum.ToString());

            ListingStandard.Gap(6f);

            // year
            var yearSelector = ListingStandard.GetRect(30f);
            var yearLabelRect = yearSelector.LeftPart(0.7f);
            var yearFieldRect = yearSelector.RightPart(0.3f);
            Widgets.Label(yearLabelRect, $"{"ClockYear".Translate()} [{GenDate.DefaultStartingYear}, {GenDate.DefaultStartingYear + 50}]: ");
            Widgets.TextFieldNumeric(yearFieldRect, ref _year, ref _yearString, GenDate.DefaultStartingYear,
                GenDate.DefaultStartingYear + 50);

            // translate day, quadrum and year to ticks
            _dateTicks = WorldData.DateToTicks(_dayOfQuadrum - 1, _quadrum, _year);

            // date display
            var dateNowRect = ListingStandard.GetRect(30f);
            var labelDateLeftRect = dateNowRect.LeftPart(0.20f);
            Widgets.Label(labelDateLeftRect, $"{"ClockDate".Translate()}: ");
            var labelDateRightRect = dateNowRect.RightPart(0.60f);
            var dateString = GenDate.DateReadoutStringAt(_dateTicks,
                Find.WorldGrid.LongLatOf(_selectedTileIdForTemperatureForecast));
            Widgets.Label(labelDateRightRect, dateString);

            Text.Anchor = backupAnchor;

            ListingStandard.GapLine(DefaultGapLineHeight);

            /*
             * Forecast
             */
            if (ListingStandard.ButtonText("PLMWT2T_ViewTemperatureForecast".Translate()))
                ViewTemperatureForecast(_selectedTileIdForTemperatureForecast, _dateTicks);
        }

        public static void ViewTemperatureForecast(int tileId, int dateTicks)
        {
            /*
             * Forecast for twelves of year.
             */
            var tempsForTwelves = TemperatureData.TemperaturesForTwelfth(tileId);
            var twelvesGetters = new List<ColumnData<TemperatureForecastForTwelfth>>
            {
                new ColumnData<TemperatureForecastForTwelfth>("PLTFW_Quadrum".Translate(), "PLTFW_QuadrumToolTip".Translate(),
                    tfft => $"{tfft.Twelfth.GetQuadrum()}"),
                new ColumnData<TemperatureForecastForTwelfth>("PLTFW_Season".Translate(), "PLTFW_SeasonToolTip".Translate(),
                    tfft => $"{tfft.Twelfth.GetQuadrum().GetSeason(tfft.Latitude)}"),
                new ColumnData<TemperatureForecastForTwelfth>("PLTFW_Twelfth".Translate(), "PLTFW_TwelfthToolTip".Translate(),
                    tfft => $"{tfft.Twelfth}"),
                new ColumnData<TemperatureForecastForTwelfth>("PLTFW_AvgTemp".Translate(), "PLTFW_AvgTempToolTip".Translate(),
                    tfft => $"{tfft.AverageTemperatureForTwelfth:F2}")
            };
            var tableViewTempForTwelves =
                new TableView<TemperatureForecastForTwelfth>("PLTFW_ForecastForTwelves".Translate(), tempsForTwelves,
                    twelvesGetters);


            var dateString = GenDate.DateReadoutStringAt(dateTicks, Find.WorldGrid.LongLatOf(tileId));

            /*
             * Forecast for hours of day
             */
            var temperaturesForHoursOfDay =
                TemperatureData.TemperaturesForDay(tileId, dateTicks);
            var temperaturesForHoursGetters = new List<ColumnData<TemperatureForecastForDay>>
            {
                new ColumnData<TemperatureForecastForDay>("PLTFW_Hour".Translate(), "PLTFW_HourToolTip".Translate(), tffd => $"{tffd.Hour}"),
                new ColumnData<TemperatureForecastForDay>("PLTFW_OutdoorTemp".Translate(), "PLTFW_OutdoorTempToolTip".Translate(),
                    tffd => $"{tffd.OutdoorTemperature:F1}"),
                //new ColumnData<TemperatureForecastForDay>("RandomVar", "Daily Random Variation", tffd => $"{tffd.DailyRandomVariation:F1}"),
                new ColumnData<TemperatureForecastForDay>("PLTFW_OffDRV".Translate(), "PLTFW_OffDRVToolTip".Translate(),
                    tffd => $"{tffd.OffsetFromDailyRandomVariation:F1}"),
                new ColumnData<TemperatureForecastForDay>("PLTFW_OffSeason".Translate(), "PLTFW_OffSeasonToolTip".Translate(),
                    tffd => $"{tffd.OffsetFromSeasonCycle:F1}"),
                new ColumnData<TemperatureForecastForDay>("PLTFW_SunEff".Translate(), "PLTFW_SunEffToolTip".Translate(),
                    tffd => $"{tffd.OffsetFromSunCycle:F1}")
            };

            var tableName = string.Format("PLTFW_ForecastForHours".Translate(), GenDate.HoursPerDay, dateString);
            var tableViewTempForDay =
                new TableView<TemperatureForecastForDay>(tableName, temperaturesForHoursOfDay,
                    temperaturesForHoursGetters);

            /*
             * Forecast for days or year
             */
            var tempsForDaysOfYear =
                TemperatureData.TemperaturesForYear(tileId, dateTicks);
            var temperaturesForDaysOfYearGetters = new List<ColumnData<TemperatureForecastForYear>>
            {
                new ColumnData<TemperatureForecastForYear>("PLTFW_Day".Translate(), "PLTFW_DayToolTip".Translate(), tffy => $"{tffy.Day}"),
                new ColumnData<TemperatureForecastForYear>("PLTFW_Min".Translate(), "PLTFW_MinToolTip".Translate(),
                    tffy => $"{tffy.MinTemp:F2}"),
                new ColumnData<TemperatureForecastForYear>("PLTFW_Max".Translate(), "PLTFW_MaxToolTip".Translate(),
                    tffy => $"{tffy.MaxTemp:F2}"),
                new ColumnData<TemperatureForecastForYear>("PLTFW_OffSeason".Translate(), "PLTFW_OffSeasonToolTip".Translate(),
                    tffy => $"{tffy.OffsetFromSeasonCycle:F2}"),
                new ColumnData<TemperatureForecastForYear>("PLTFW_OffDRV".Translate(), "PLTFW_OffDRVToolTip".Translate(),
                    tffy => $"{tffy.OffsetFromDailyRandomVariation:F2}")
            };
            var tableViewTempForYear =
                new TableView<TemperatureForecastForYear>("PLTFW_ForecastForNextYear".Translate(), tempsForDaysOfYear,
                    temperaturesForDaysOfYearGetters);

            /*
             * Window and views
             */
            var temperatureWindow = new TableWindow(tileId, dateTicks, 0.33f);

            temperatureWindow.ClearTables();
            temperatureWindow.AddTable(tableViewTempForDay);
            temperatureWindow.AddTable(tableViewTempForTwelves);
            temperatureWindow.AddTable(tableViewTempForYear);

            Find.WindowStack.Add(temperatureWindow);
        }

        private void DrawAnimalsCanGrazeNowSelection()
        {
            DrawEntryHeader($"{"WorkTagAnimals".Translate().CapitalizeFirst()}",
                backgroundColor: ColorFromFilterType(typeof(TileFilterAnimalsCanGrazeNow)));

            var rect = ListingStandard.GetRect(DefaultElementHeight);
            var tmpCheckState = _gameData.UserData.ChosenAnimalsCanGrazeNowState;
            Core.Gui.Widgets.CheckBoxLabeledMulti(rect, $"{"AnimalsCanGrazeNow".Translate()}: ", ref tmpCheckState);

            _gameData.UserData.ChosenAnimalsCanGrazeNowState = tmpCheckState;
        }

        /// <summary>
        ///     Draw the "Has Cave" selection.
        /// </summary>
        private void DrawCaveSelection()
        {
            DrawEntryHeader("SpecialFeatures".Translate(),
                backgroundColor: ColorFromFilterType(typeof(TileFilterHasCave)));

            var rect = ListingStandard.GetRect(DefaultElementHeight);
            var tmpCheckState = _gameData.UserData.HasCaveState;
            Core.Gui.Widgets.CheckBoxLabeledMulti(rect, $"{"HasCaves".Translate()}:", ref tmpCheckState);

            _gameData.UserData.HasCaveState = tmpCheckState;
        }

        private void DrawGrowingPeriodSelection() // TODO check against the other like coastal filter or movement time
        {
            var label = "OutdoorGrowingPeriod".Translate();
            DrawEntryHeader($"{label} ({"DaysLower".Translate()})",
                backgroundColor: ColorFromFilterType(typeof(TileFilterGrowingPeriods)));

            DrawUsableMinMaxFromRestrictedListItem(_gameData.UserData.GrowingPeriod, label,
                twelfth => twelfth.GrowingDaysString());
        }

        private void DrawRainfallSelection()
        {
            /* Max rainfall is defined as private in RimWorld.Planet.WorldMaterials.RainfallMax (5000)
             However it is possible to have more than 5000 mm of rain... see #33.
             The rainfall for a tile is set in RimWorld.Planet.WorldGenStep_Terrain.GenerateTileFor(int)
             It is calculated from the noiseRainFall which is set in RimWorld.Planet.WorldGenStep_Terrain.SetupRainfallNoise()
             So it is dependent on multiple curves ("modules" in RimWorld). The calculation there is too difficult to follow
             statically. From the issue #33, my guess is it can't more than 10000 but this is just a wild guess.
             */
            const float rainfallMin = 0f;
            const float rainfallMax = 10000f;

            DrawEntryHeader($"{"Rainfall".Translate()} ({"PLMWT2T_RainfallMillimeters".Translate()}) [{rainfallMin}, {rainfallMax}]",
                backgroundColor: ColorFromFilterType(typeof(TileFilterRainFalls)));

            DrawUsableMinMaxNumericField(_gameData.UserData.RainFall, "Rainfall".Translate(), rainfallMin, rainfallMax);
        }

        private void DrawTemperaturesSelection()
        {
            // min and max possible temperatures, in C/F/K (depending on user prefs).
            // note that TemperatureTuning temps are in Celsius.
            var tempMinUnit = GenTemperature.CelsiusTo(TemperatureTuning.MinimumTemperature, Prefs.TemperatureMode);
            var tempMaxUnit = GenTemperature.CelsiusTo(TemperatureTuning.MaximumTemperature, Prefs.TemperatureMode);

            DrawEntryHeader($"{"Temperature".Translate()} (°{Prefs.TemperatureMode.ToStringHuman()}) [{tempMinUnit}, {tempMaxUnit}]",
                backgroundColor: ColorFromFilterType(typeof(TileFilterAverageTemperatures)));

            DrawUsableMinMaxNumericField(_gameData.UserData.AverageTemperature, "AvgTemp".Translate(),
                tempMinUnit, tempMaxUnit);

            ListingStandard.GapLine();

            DrawUsableMinMaxNumericField(_gameData.UserData.MinTemperature, "Minimum Temperature", //TODO: translate
                tempMinUnit, tempMaxUnit);


            ListingStandard.GapLine();

            DrawUsableMinMaxNumericField(_gameData.UserData.MaxTemperature, "Maximum Temperature", // TODO: translate
                tempMinUnit, tempMaxUnit);

        }

        private void DrawMostLeastCharacteristicSelection()
        {
            DrawEntryHeader("PLMWT2T_MostLeastCharacteristics".Translate(),
                backgroundColor: ColorFromFilterType(typeof(TileFilterMostLeastCharacteristic)));

            /*
             * Select Characteristic
             */
            var selectCharacteristic = "PLMWT2T_MostLeastSelectCharacteristic".Translate();

            if (ListingStandard.ButtonText(selectCharacteristic))
            {
                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (var characteristic in Enum.GetValues(typeof(MostLeastCharacteristic)).Cast<MostLeastCharacteristic>())
                {
                    var menuOption = new FloatMenuOption(characteristic.ToString(),
                        delegate { _gameData.UserData.MostLeastItem.Characteristic = characteristic; });
                    floatMenuOptions.Add(menuOption);
                }

                var floatMenu = new FloatMenu(floatMenuOptions, selectCharacteristic);
                Find.WindowStack.Add(floatMenu);
            }

            /*
             * Number of tiles to select.
             */
            ListingStandard.GapLine(DefaultGapLineHeight);

            var tilesNumberRect = ListingStandard.GetRect(DefaultElementHeight);
            var leftRect = tilesNumberRect.LeftPart(0.80f);
            var rightRect = tilesNumberRect.RightPart(0.20f);

            Widgets.Label(leftRect, $"{"PLMWT2T_MostLeastNumberOfTiles".Translate()} [1, 10000]:");
            _numberOfTilesForCharacteristic = _gameData.UserData.MostLeastItem.NumberOfItems;
            Widgets.TextFieldNumeric(rightRect, ref _numberOfTilesForCharacteristic, ref _numberOfTilesForCharacteristicString,
                1, 10000);
            _gameData.UserData.MostLeastItem.NumberOfItems = _numberOfTilesForCharacteristic;

            /*
             * Select Characteristic Type (most / least)
             */

            var selectCharacteristicType = "PLMWT2T_MostLeastSelectCharacteristicType".Translate();

            if (ListingStandard.ButtonText(selectCharacteristicType))
            {
                var floatMenuOptions = new List<FloatMenuOption>();
                foreach (var characteristicType in Enum.GetValues(typeof(MostLeastType)).Cast<MostLeastType>())
                {
                    var menuOption = new FloatMenuOption(characteristicType.ToString(),
                        delegate { _gameData.UserData.MostLeastItem.CharacteristicType = characteristicType; });
                    floatMenuOptions.Add(menuOption);
                }

                var floatMenu = new FloatMenu(floatMenuOptions, selectCharacteristicType);
                Find.WindowStack.Add(floatMenu);
            }

            /*
             * Result label
             */
            string text;
            if (_gameData.UserData.MostLeastItem.Characteristic == MostLeastCharacteristic.None)
            {
                text = string.Format("PLMWT2T_MostLeastPressButtonFirst".Translate(), selectCharacteristic);
            }
            else if (_gameData.UserData.MostLeastItem.CharacteristicType == MostLeastType.None)
            {
                text = string.Format("PLMWT2T_MostLeastNowUseButton".Translate(), selectCharacteristicType);
            }
            else
            {
                var highestLowest = _gameData.UserData.MostLeastItem.CharacteristicType == MostLeastType.Most
                    ? "PLMWT2T_MostLeastHighest".Translate()
                    : "PLMWT2T_MostLeastLowest".Translate();
                var tileString = _gameData.UserData.MostLeastItem.NumberOfItems > 1 ? "PLMW_Tiles".Translate() : "PLMW_Tile".Translate();
                text = string.Format("PLMWT2T_MostLeastSelectingTiles".Translate(),
                    _gameData.UserData.MostLeastItem.NumberOfItems, tileString, highestLowest,
                    _gameData.UserData.MostLeastItem.Characteristic);
            }

            ListingStandard.Label($"{"PLMWT2T_MostLeastResult".Translate()}: {text}", DefaultElementHeight * 2);
        }
    }
}