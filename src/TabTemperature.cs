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
            DrawMostLeastCharacteristicSelection();
            DrawAnimalsCanGrazeNowSelection();
            DrawCaveSelection();
            NewColumn();
            DrawTemperatureForecast();
            DrawFeatureSelection();
            End();
        }

        private void DrawFeatureSelection()
        {
            DrawEntryHeader("World Feature Selection", backgroundColor: Color.magenta);

            var features = _gameData.WorldData.WorldFeatures;

            // "Select" button
            if (ListingStandard.ButtonText("Select World Feature"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();

                // add a dummy 'Any' fake feature type. This sets the chosen feature to null.
                Action actionClick = delegate { _gameData.UserData.WorldFeature = null; };
                // tool-tip when hovering above the 'Any' feature name on the floating menu
                Action mouseOverAction = delegate
                {
                    var mousePos = Event.current.mousePosition;
                    var rect = new Rect(mousePos.x, mousePos.y, DefaultElementHeight, DefaultElementHeight);

                    TooltipHandler.TipRegion(rect, "Any Feature");
                };
                var menuOption = new FloatMenuOption("Any", actionClick, MenuOptionPriority.Default, mouseOverAction);
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
                    // TODO: handle other water bodies, you'll to parse the def name as there are no other ways
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
                var floatMenu = new FloatMenu(floatMenuOptions, "Select Feature Type");

                // add it to the window stack to display it
                Find.WindowStack.Add(floatMenu);
            }

            var currHeightBefore = ListingStandard.CurHeight;

            var rightLabel = _gameData.UserData.WorldFeature != null ? _gameData.UserData.WorldFeature.name : "Any";
            ListingStandard.LabelDouble("World Feature:", rightLabel);

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
                new ColumnData<TemperatureForecastForTwelfth>("Quadrum", "Quadrum of Year",
                    tfft => $"{tfft.Twelfth.GetQuadrum()}"),
                new ColumnData<TemperatureForecastForTwelfth>("Season", "Season of Year",
                    tfft => $"{tfft.Twelfth.GetQuadrum().GetSeason(tfft.Latitude)}"),
                new ColumnData<TemperatureForecastForTwelfth>("Twelfth", "Twelfth of Year",
                    tfft => $"{tfft.Twelfth}"),
                new ColumnData<TemperatureForecastForTwelfth>("Avg. Temp", "Average Temperature for Twelfth",
                    tfft => $"{tfft.AverageTemperatureForTwelfth:F2}")
            };
            var tableViewTempForTwelves =
                new TableView<TemperatureForecastForTwelfth>("Forecast For Twelves", tempsForTwelves,
                    twelvesGetters);


            var dateString = GenDate.DateReadoutStringAt(dateTicks, Find.WorldGrid.LongLatOf(tileId));

            /*
             * Forecast for hours of day
             */
            var temperaturesForHoursOfDay =
                TemperatureData.TemperaturesForDay(tileId, dateTicks);
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
                TemperatureData.TemperaturesForYear(tileId, dateTicks);
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
            var temperatureWindow = new TableWindow(tileId, dateTicks, 0.33f);

            temperatureWindow.ClearTables();
            temperatureWindow.AddTable(tableViewTempForDay);
            temperatureWindow.AddTable(tableViewTempForTwelves);
            temperatureWindow.AddTable(tableViewTempForYear);

            Find.WindowStack.Add(temperatureWindow);
        }

        private void DrawAnimalsCanGrazeNowSelection()
        {
            DrawEntryHeader("Animals", backgroundColor: ColorFromFilterSubjectThingDef("Animals Can Graze Now"));

            var rect = ListingStandard.GetRect(DefaultElementHeight);
            var tmpCheckState = _gameData.UserData.ChosenAnimalsCanGrazeNowState;
            Core.Gui.Widgets.CheckBoxLabeledMulti(rect, "Animals Can Graze Now:", ref tmpCheckState);

            _gameData.UserData.ChosenAnimalsCanGrazeNowState = tmpCheckState;
        }

        /// <summary>
        ///     Draw the "Has Cave" selection.
        /// </summary>
        private void DrawCaveSelection()
        {
            DrawEntryHeader("Special Features", backgroundColor: ColorFromFilterSubjectThingDef("Has Cave"));

            var rect = ListingStandard.GetRect(DefaultElementHeight);
            var tmpCheckState = _gameData.UserData.HasCaveState;
            Core.Gui.Widgets.CheckBoxLabeledMulti(rect, "Has Cave: ", ref tmpCheckState);

            _gameData.UserData.HasCaveState = tmpCheckState;
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

        private void DrawMostLeastCharacteristicSelection()
        {
            DrawEntryHeader("Most / Least Characteristic", backgroundColor: ColorFromFilterSubjectThingDef("Biomes"));

            /*
             * Select Characteristic
             */
            const string selectCharacteristic = "Select Characteristic";

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

            Widgets.Label(leftRect, "Number of Tiles [1, 10000]:");
            _numberOfTilesForCharacteristic = _gameData.UserData.MostLeastItem.NumberOfItems;
            Widgets.TextFieldNumeric(rightRect, ref _numberOfTilesForCharacteristic, ref _numberOfTilesForCharacteristicString,
                1, 10000);
            _gameData.UserData.MostLeastItem.NumberOfItems = _numberOfTilesForCharacteristic;

            /*
             * Select Characteristic Type (most / least)
             */

            const string selectCharacteristicType = "Select Characteristic Type";

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
                text = $"Push \"{selectCharacteristic}\" button first.";
            }
            else if (_gameData.UserData.MostLeastItem.CharacteristicType == MostLeastType.None)
            {
                text = $"Now use the \"{selectCharacteristicType}\" button.";
            }
            else
            {
                var highestLowest = _gameData.UserData.MostLeastItem.CharacteristicType == MostLeastType.Most
                    ? "highest"
                    : "lowest";
                var tiles = _gameData.UserData.MostLeastItem.NumberOfItems > 1 ? "tiles" : "tile";
                text =
                    $"Selecting {_gameData.UserData.MostLeastItem.NumberOfItems} {tiles} with the {highestLowest} {_gameData.UserData.MostLeastItem.Characteristic}";
            }

            ListingStandard.Label($"Result: {text}", DefaultElementHeight * 2);
        }
    }
}