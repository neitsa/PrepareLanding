using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Profile;
using Verse.Sound;

namespace PrepareLanding.Patches
{
    /// <summary>
    ///     This class is used in replacement of the <see cref="RimWorld.Page_CreateWorldParams" /> class.
    ///     We already have patched <see cref="RimWorld.PageUtility.StitchedPages" /> method (see
    ///     <see cref="PageUtilityPatch.StitchedPagesPostFix" />) to use our own class rather than the RimWorld one.
    /// </summary>
    public class CreateWorldParams : Page
    {
        private readonly List<int> _fixedCoverages = new List<int>();

        private bool _initialized;

        private int _intCoverage;

        private string _intCoverageString;

        private float _planetCoverage;

        private OverallRainfall _rainfall;

        private string _seedString;

        private OverallTemperature _temperature;

        private OverallPopulation _population;

        public override string PageTitle => "CreateWorld".Translate();

        public override void PreOpen()
        {
            base.PreOpen();
            if (!_initialized)
            {
                Reset();
                _initialized = true;

                _fixedCoverages.Clear();
                _fixedCoverages.Add(5);

                const int start = 10; // 10%
                for (var cov = start; cov <= 100; cov += 10)
                    _fixedCoverages.Add(cov);
            }
        }

        public override void PostOpen()
        {
            base.PostOpen();
            TutorSystem.Notify_Event("PageStart-CreateWorldParams");
        }

        public void Reset()
        {
            _seedString = GenText.RandomSeedString();
            _planetCoverage = Prefs.DevMode && UnityData.isEditor ? 0.05f : 0.3f;
            _intCoverage = Prefs.DevMode && UnityData.isEditor ? 5 : 30;
            _rainfall = OverallRainfall.Normal;
            _temperature = OverallTemperature.Normal;
        }

        public override void DoWindowContents(Rect rect)
        {
            DrawPageTitle(rect);
            GUI.BeginGroup(GetMainRect(rect));
            Text.Font = GameFont.Small;
            var num = 0f;
            Widgets.Label(new Rect(0f, num, 200f, 30f), "WorldSeed".Translate());
            var rect2 = new Rect(200f, num, 200f, 30f);
            _seedString = Widgets.TextField(rect2, _seedString);
            num += 40f;
            var rect3 = new Rect(200f, num, 200f, 30f);
            if (Widgets.ButtonText(rect3, "RandomizeSeed".Translate()))
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                _seedString = GenText.RandomSeedString();
            }

            num += 40f;

            /*
             * Our own planet coverage
             */

            Text.Font = GameFont.Tiny;
            var textAnchorBackup = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0, num, rect3.xMax, 20f), "[Prepare Landing] Precise World Generation %");
            Text.Font = GameFont.Small;
            Text.Anchor = textAnchorBackup;

            num += 20f;
            Widgets.Label(new Rect(0f, num, 200f, 30f), $"{"PlanetCoverage".Translate()} [1,100]%");
            var rect4 = new Rect(200f, num, 200f, 30f);

            TextFieldNumericCoverage(rect4.LeftHalf());
            ButtonCoverage(rect4.RightHalf());

            // end of specific private code

            TooltipHandler.TipRegion(new Rect(0f, num, rect4.xMax, rect4.height), "PlanetCoverageTip".Translate());

            num += 40f;
            Widgets.Label(new Rect(0f, num, 200f, 30f), "PlanetRainfall".Translate());
            var rect5 = new Rect(200f, num, 200f, 30f);
            _rainfall = (OverallRainfall) Mathf.RoundToInt(Widgets.HorizontalSlider(rect5, (float) _rainfall, 0f,
                OverallRainfallUtility.EnumValuesCount - 1, true, "PlanetRainfall_Normal".Translate(),
                "PlanetRainfall_Low".Translate(), "PlanetRainfall_High".Translate(), 1f));

            num += 40f;
            Widgets.Label(new Rect(0f, num, 200f, 30f), "PlanetTemperature".Translate());
            var rect6 = new Rect(200f, num, 200f, 30f);
            _temperature = (OverallTemperature) Mathf.RoundToInt(Widgets.HorizontalSlider(rect6, (float) _temperature,
                0f, OverallTemperatureUtility.EnumValuesCount - 1, true, "PlanetTemperature_Normal".Translate(),
                "PlanetTemperature_Low".Translate(), "PlanetTemperature_High".Translate(), 1f));

            num += 40f;
            Widgets.Label(new Rect(0f, num, 200f, 30f), "PlanetPopulation".Translate());
            var rect7 = new Rect(200f, num, 200f, 30f);
            _population = (OverallPopulation)Mathf.RoundToInt(Widgets.HorizontalSlider(rect7, (float)_population, 0f, 
                (float)(OverallPopulationUtility.EnumValuesCount - 1), true, "PlanetPopulation_Normal".Translate(), 
                "PlanetPopulation_Low".Translate(), "PlanetPopulation_High".Translate(), 1f));

            GUI.EndGroup();
            DoBottomButtons(rect, "WorldGenerate".Translate(), "Reset".Translate(), Reset);
        }

        private void TextFieldNumericCoverage(Rect rect)
        {
            var tmpIntCoverage = _intCoverage;
            _intCoverageString = tmpIntCoverage.ToString();

            Widgets.TextFieldNumeric(rect, ref tmpIntCoverage, ref _intCoverageString, 1, 100);

            if (tmpIntCoverage != _intCoverage)
            {
                _intCoverage = tmpIntCoverage;
                _planetCoverage = _intCoverage / 100f;
                if (_intCoverage >= 95)
                    Messages.Message("MessageMaxPlanetCoveragePerformanceWarning".Translate(),
                        MessageTypeDefOf.CautionInput);
                if (_intCoverage <= 3)
                    Messages.Message("Warning! Small planets may not offer settleable tiles",
                        MessageTypeDefOf.CautionInput);
            }
        }

        private void ButtonCoverage(Rect rect)
        {
            if (Widgets.ButtonText(rect, $"{_intCoverage}%"))
            {
                var floatMenuOptions = new List<FloatMenuOption>();

                foreach (var fixedCoverage in _fixedCoverages)
                {
                    var text = $"{fixedCoverage}%";
                    if (fixedCoverage <= 10)
                        text += " (dev)";
                    var coverage = fixedCoverage;
                    var item = new FloatMenuOption(text, delegate
                    {
                        if (coverage != _intCoverage)
                        {
                            _intCoverage = coverage;
                            _planetCoverage = _intCoverage / 100f;
                            if (_intCoverage >= 95)
                                Messages.Message("MessageMaxPlanetCoveragePerformanceWarning".Translate(),
                                    MessageTypeDefOf.CautionInput);
                        }
                    });
                    floatMenuOptions.Add(item);
                }

                Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
            }
        }

        protected override bool CanDoNext()
        {
            if (!base.CanDoNext()) return false;
            LongEventHandler.QueueLongEvent(delegate
            {
                Find.GameInitData.ResetWorldRelatedMapInitData();

                Current.Game.World =
                    WorldGenerator.GenerateWorld(_planetCoverage, _seedString, _rainfall, _temperature, _population);
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    if (next != null) Find.WindowStack.Add(next);
                    MemoryUtility.UnloadUnusedUnityAssets();
                    Find.World.renderer.RegenerateAllLayersNow();
                    Close();
                });
            }, "GeneratingWorld", true, null);
            return false;
        }
    }
}