using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Profile;

namespace PrepareLanding.Patches
{
    class PagePreciseWorldGeneration: Page
    {
        private readonly List<int> _fixedCoverages = new List<int>();

        private bool _initialized;

        private int _intCoverage;

        private string _intCoverageString;

        private float _planetCoverage;

        private readonly OverallRainfall _rainfall;

        private readonly string _seedString;

        private readonly OverallTemperature _temperature;

        private readonly OverallPopulation _population;

        private readonly List<FactionDef> _factions;

        private readonly float _pollution;

        public override string PageTitle => "Precise World Generation";

        public PagePreciseWorldGeneration(float planetCoverage, string seedString, OverallRainfall rainFall, OverallTemperature temperature, OverallPopulation population, List<FactionDef> factions, float pollution)
        {
            _planetCoverage = planetCoverage;
            _seedString = seedString;
            _rainfall = rainFall;
            _temperature = temperature;
            _population = population;
            _factions = factions;
            _pollution = pollution;
        }

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

        public override void DoWindowContents(Rect rect)
        {
            DrawPageTitle(rect);
            GUI.BeginGroup(GetMainRect(rect));
            Text.Font = GameFont.Small;
            var num = 0f;
            num += 40f;

            var rect3 = new Rect(200f, num, 200f, 30f);

            // Our own planet coverage

            Text.Font = GameFont.Small;
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

            TooltipHandler.TipRegion(new Rect(0f, num, rect4.xMax, rect4.height), "PlanetCoverageTip".Translate());

            GUI.EndGroup();
            DoBottomButtons(rect, "WorldGenerate".Translate(), "Reset".Translate(), Reset);
        }

        private void Reset()
        {
            _intCoverage = (int)(_planetCoverage * 100.0f);
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
            if (!base.CanDoNext())
            {
                return false;
            }
            LongEventHandler.QueueLongEvent(delegate
            {
                Find.GameInitData.ResetWorldRelatedMapInitData();
                Current.Game.World = WorldGenerator.GenerateWorld(_planetCoverage, _seedString, _rainfall, _temperature, _population, _factions, _pollution);
                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    if (next != null)
                    {
                        Find.WindowStack.Add(next);
                    }
                    MemoryUtility.UnloadUnusedUnityAssets();
                    Find.World.renderer.RegenerateAllLayersNow();
                    Close();
                });
            }, "GeneratingWorld", true, null);
            return false;
        }
    }
}
