using System.Collections.Generic;
using PrepareLanding.Extensions;
using PrepareLanding.Gui.Tab;
using UnityEngine;
using Verse;
using Widgets = PrepareLanding.Gui.Widgets;

namespace PrepareLanding
{
    public class TabGuiUtilityTemperature : TabGuiUtility
    {
        private readonly PrepareLandingUserData _userData;

        public TabGuiUtilityTemperature(PrepareLandingUserData userData, float columnSizePercent = 0.25f) :
            base(columnSizePercent)
        {
            _userData = userData;
        }

        public override string Id => Name;

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

            // "Animals Can Graze Now" relies on game ticks as VirtualPlantsUtility.EnvironmentAllowsEatingVirtualPlantsNowAt calls
            //   GenTemperature.GetTemperatureAtTile which calls GenTemperature.GetTemperatureFromSeasonAtTile
            //  This last function takes GenTicks.TicksAbs as argument but the results are not consistent between calls...
            // All in all: better not calling it during the "select landing site" page.
            if (GenScene.InPlayScene)
                DrawAnimalsCanGrazeNowSelection();
            End();
        }

        protected void DrawTemperaturesSelection()
        {
            DrawEntryHeader("Temperatures (Celsius)", backgroundColor: ColorFromFilterSubjectThingDef("Average Temperatures"));

            DrawUsableMinMaxNumericField(_userData.AverageTemperature, "Average Temperature",
                TemperatureTuning.MinimumTemperature, TemperatureTuning.MaximumTemperature);
            DrawUsableMinMaxNumericField(_userData.WinterTemperature, "Winter Temperature",
                TemperatureTuning.MinimumTemperature, TemperatureTuning.MaximumTemperature);
            DrawUsableMinMaxNumericField(_userData.SummerTemperature, "Summer Temperature",
                TemperatureTuning.MinimumTemperature, TemperatureTuning.MaximumTemperature);
        }

        protected void DrawGrowingPeriodSelection()
        {
            const string label = "Growing Period";
            DrawEntryHeader($"{label} (days)", backgroundColor: ColorFromFilterSubjectThingDef("Growing Periods"));

            var boundField = _userData.GrowingPeriod;

            var tmpCheckedOn = boundField.Use;

            ListingStandard.Gap(DefaultGapHeight);
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

        protected virtual void DrawRainfallSelection()
        {
            DrawEntryHeader("Rain Fall (mm)", backgroundColor: ColorFromFilterSubjectThingDef("Rain Falls"));

            DrawUsableMinMaxNumericField(_userData.RainFall, "Rain Fall");
        }

        protected virtual void DrawAnimalsCanGrazeNowSelection()
        {
            DrawEntryHeader("Animals", backgroundColor: ColorFromFilterSubjectThingDef("Animals Can Graze Now"));

            var rect = ListingStandard.GetRect(DefaultElementHeight);
            var tmpCheckState = _userData.ChosenAnimalsCanGrazeNowState;
            Widgets.CheckBoxLabeledMulti(rect, "Animals Can Graze Now:", ref tmpCheckState);

            _userData.ChosenAnimalsCanGrazeNowState = tmpCheckState;
        }
    }
}