using System;
using System.Collections.Generic;
using System.Text;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Overlays;
using RimWorld;
using UnityEngine;
using Verse;

namespace PrepareLanding.GameData
{
    public class TemperatureData : WorldFeatureData
    {
        private bool _allowDrawOverlay;


        public TemperatureData(DefData defData) : base(defData)
        {
        }

        public bool AllowDrawOverlay
        {
            get { return _allowDrawOverlay; }
            set
            {
                if (value == _allowDrawOverlay)
                    return;

                _allowDrawOverlay = value;
                Find.World.renderer.SetDirty<WorldLayerTemperature>();
            }
        }

        public override MostLeastFeature Feature => MostLeastFeature.Temperature;

        public override string FeatureMeasureUnit => "°C";

        public Texture2D TemperatureGradientTexure => FeatureGradientTexure;

        public Dictionary<BiomeDef, Dictionary<int, float>> TemperaturesByBiomes => FeatureByBiomes;

        public bool TickManagerHasTickAbs => Find.TickManager.gameStartAbsTick == 0;

        public List<KeyValuePair<int, float>> WorldTilesTemperatures => WorldTilesFeatures;

        protected override float TileFeatureValue(int tileId)
        {
            return Find.World.grid[tileId].temperature;
        }

#if DEBUG
        public void DebugLog()
        {
            if (Biome == null)
                return;

            var sb = new StringBuilder();

            var bigSeparator = "-".Repeat(80);
            var smallSeparator = "-".Repeat(40);

            var biomeMinTemp = MinFeatureByBiome(Biome);
            var biomeMaxTemp = MaxFeatureByBiome(Biome);

            sb.AppendLine(bigSeparator);
            sb.AppendLine(smallSeparator);
            sb.AppendLine("---------- DebugLogTemperature ----------");
            sb.AppendLine(smallSeparator);
            sb.AppendLine($"Biome: {Biome}; biomeMinTemp: {biomeMinTemp}; biomeMaxTemp: {biomeMaxTemp}");

            sb.AppendLine(smallSeparator);
            sb.AppendLine("---------- temperatureQuanta ----------");
            sb.AppendLine(smallSeparator);
            var temperatureQuanta = FeatureQuantaByBiomes[Biome];
            sb.AppendLine($"temperatureQuanta.Count: {temperatureQuanta.Count}");
            for (var i = 0; i < temperatureQuanta.Count; i++)
                sb.AppendLine($"{i}: {temperatureQuanta[i]}");

            sb.AppendLine(smallSeparator);
            sb.AppendLine("---------- By Tile ----------");
            sb.AppendLine(smallSeparator);
            var tilesAndTemps = TemperaturesByBiomes[Biome];
            foreach (var tileId in tilesAndTemps.Keys)
            {
                var tileAverageTemp = tilesAndTemps[tileId];
                try
                {
                    var quantaIndex = FindQuantaIndex(Biome, tileId);
                    var color = ColorSamplesByBiomes[Biome][quantaIndex];
                    var s =
                        $"tileId: {tileId}; tileAverageTemp: {tileAverageTemp}; quantaIndex: {quantaIndex}; color: {color}";
                    sb.AppendLine(s);
                }
                catch (InvalidOperationException)
                {
                    var s = $"[Error: quantaIndex: OOB] tileId: {tileId}; tileAverageTemp: {tileAverageTemp}";
                    sb.AppendLine(s);
                }
            }
            sb.AppendLine(bigSeparator);

            Log.Message(sb.ToString());
        }
#endif
    }
}