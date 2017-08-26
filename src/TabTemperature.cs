using System;
using System.Collections.Generic;
using System.Linq;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Core.Gui.Tab;
using UnityEngine;
using Verse;
using Widgets = PrepareLanding.Core.Gui.Widgets;

namespace PrepareLanding
{
    public class TabTemperature : TabGuiUtility
    {
        private readonly GameData.GameData _gameData;

        private int _numberOfTilesForFeature = 1;

        private string _numberOfTilesForFeatureString;

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
            End();
        }

        private void DrawAnimalsCanGrazeNowSelection()
        {
            DrawEntryHeader("Animals", backgroundColor: ColorFromFilterSubjectThingDef("Animals Can Graze Now"));

            var rect = ListingStandard.GetRect(DefaultElementHeight);
            var tmpCheckState = _gameData.UserData.ChosenAnimalsCanGrazeNowState;
            Widgets.CheckBoxLabeledMulti(rect, "Animals Can Graze Now:", ref tmpCheckState);

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
            DrawEntryHeader("Rain Fall (mm)", backgroundColor: ColorFromFilterSubjectThingDef("Rain Falls"));

            DrawUsableMinMaxNumericField(_gameData.UserData.RainFall, "Rain Fall");
        }

        private void DrawTemperaturesSelection()
        {
            DrawEntryHeader("Temperatures (Celsius)",
                backgroundColor: ColorFromFilterSubjectThingDef("Average Temperatures"));

            DrawUsableMinMaxNumericField(_gameData.UserData.AverageTemperature, "Average Temperature",
                TemperatureTuning.MinimumTemperature, TemperatureTuning.MaximumTemperature);
            DrawUsableMinMaxNumericField(_gameData.UserData.WinterTemperature, "Winter Temperature",
                TemperatureTuning.MinimumTemperature, TemperatureTuning.MaximumTemperature);
            DrawUsableMinMaxNumericField(_gameData.UserData.SummerTemperature, "Summer Temperature",
                TemperatureTuning.MinimumTemperature, TemperatureTuning.MaximumTemperature);
        }

        private void DrawMostLeastFeatureSelection()
        {
            DrawEntryHeader("Most / Least", backgroundColor: ColorFromFilterSubjectThingDef("Biomes"));

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

            Verse.Widgets.Label(leftRect, "Number of Tiles:");
            Verse.Widgets.TextFieldNumeric(rightRect, ref _numberOfTilesForFeature, ref _numberOfTilesForFeatureString,
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