using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Overlays;
using RimWorld;
using UnityEngine;
using Verse;

namespace PrepareLanding.GameData
{
    public class TemperatureForecastForDay
    {
        public TemperatureForecastForDay(int tileId, int ticks, int hour)
        {
            TileId = tileId;
            Ticks = ticks;
            Hour = hour;

            OutdoorTemperature = Find.World.tileTemperatures.OutdoorTemperatureAt(tileId, Ticks);
            OffsetFromSunCycle = GenTemperature.OffsetFromSunCycle(Ticks, TileId);
            OffsetFromDailyRandomVariation = Find.World.tileTemperatures.OffsetFromDailyRandomVariation(TileId,
                Ticks);
            OffsetFromSeasonCycle = GenTemperature.OffsetFromSeasonCycle(Ticks, tileId);

            var tile = Find.WorldGrid[TileId];
            DailyRandomVariation = tile.temperature -
                                   (OffsetFromSeasonCycle + OffsetFromDailyRandomVariation + OffsetFromSunCycle);
        }

        public float DailyRandomVariation { get; }

        public int Hour { get; }

        public float OffsetFromDailyRandomVariation { get; }

        public float OffsetFromSeasonCycle { get; }

        public float OffsetFromSunCycle { get; }

        public float OutdoorTemperature { get; }

        public int Ticks { get; }

        public int TileId { get; }
    }

    public class TemperatureForecastForTwelfth
    {
        public TemperatureForecastForTwelfth(int tileId, Twelfth twelfth)
        {
            Latitude = Find.WorldGrid.LongLatOf(tileId).y;
            Twelfth = twelfth;

            AverageTemperatureForTwelfth = Find.World.tileTemperatures.AverageTemperatureForTwelfth(tileId, twelfth);
        }

        public float AverageTemperatureForTwelfth { get; }

        public float Latitude { get; }

        public Twelfth Twelfth { get; }
    }

    public class TemperatureForecastForYear
    {
        public TemperatureForecastForYear(int tileId, int ticks, int day)
        {
            Day = day;
            /*
             * Get min & max temperatures for the day
             */
            var tempsForHourOfDay = new List<float>(GenDate.HoursPerDay);
            for (var hour = 0; hour < GenDate.HoursPerDay; hour++)
            {
                var hourTicks = ticks + hour * GenDate.TicksPerHour;
                var temp = Find.World.tileTemperatures.OutdoorTemperatureAt(tileId, hourTicks);
                tempsForHourOfDay.Add(temp);
            }

            // get min & max from list of temperatures for the day
            MinTemp = tempsForHourOfDay.Min();
            MaxTemp = tempsForHourOfDay.Max();

            // get number of ticks for the maximum temperature
            var ticksForMaxTemp = ticks + tempsForHourOfDay.IndexOf(MaxTemp) * GenDate.TicksPerHour;

            OffsetFromSeasonCycle = GenTemperature.OffsetFromSeasonCycle(ticksForMaxTemp, tileId);

            OffsetFromDailyRandomVariation =
                Find.World.tileTemperatures.OffsetFromDailyRandomVariation(tileId, ticksForMaxTemp);
        }

        public int Day { get; }

        public float MaxTemp { get; }

        public float MinTemp { get; }

        public float OffsetFromDailyRandomVariation { get; }

        public float OffsetFromSeasonCycle { get; }
    }

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

        public static bool TickManagerHasTickAbs => Find.TickManager.gameStartAbsTick != 0;

        public List<KeyValuePair<int, float>> WorldTilesTemperatures => WorldTilesFeatures;

        protected override float TileFeatureValue(int tileId)
        {
            return Find.World.grid[tileId].temperature;
        }

        private static void PushTickAbs()
        {
            if (!TickManagerHasTickAbs)
                Find.TickManager.gameStartAbsTick = 1;
        }

        private static void PopTickAbs()
        {
            if (Find.TickManager.gameStartAbsTick == 1)
                Find.TickManager.gameStartAbsTick = 0;
        }

        public static List<TemperatureForecastForDay> TemperaturesForDay(int tileId, int ticks)
        {
            if (tileId < 0)
            {
                Log.Error($"[PrepareLanding] TemperaturesForDay: wrong tile id: {tileId}.");
                return null;
            }

            var temperatures = new List<TemperatureForecastForDay>(GenDate.HoursPerDay);

            PushTickAbs();

            try
            {
                var dayTicks = ticks - ticks % GenDate.TicksPerDay;
                for (var i = 0; i < GenDate.HoursPerDay; i++)
                {
                    var absTick = dayTicks + i * GenDate.TicksPerHour;
                    var forecast = new TemperatureForecastForDay(tileId, absTick, i);
                    temperatures.Add(forecast);
                }
            }
            finally
            {
                PopTickAbs();
            }

            return temperatures;
        }

        public static List<TemperatureForecastForTwelfth> TemperaturesForTwelfth(int tileId)
        {
            if (tileId < 0)
            {
                Log.Error($"[PrepareLanding] TemperaturesForTwelfth: wrong tile id: {tileId}.");
                return null;
            }

            var temperatures = new List<TemperatureForecastForTwelfth>(GenDate.TwelfthsPerYear);

            for (var j = 0; j < GenDate.TwelfthsPerYear; j++)
            {
                var forecast = new TemperatureForecastForTwelfth(tileId, (Twelfth) j);
                temperatures.Add(forecast);
            }

            return temperatures;
        }

        public static List<TemperatureForecastForYear> TemperaturesForYear(int tileId, int ticks)
        {
            PushTickAbs();
            var temperatures = new List<TemperatureForecastForYear>(GenDate.DaysPerYear);
            try
            {
                for (var dayIndex = 0; dayIndex < GenDate.DaysPerYear; dayIndex++)
                {
                    var dayTicks = ticks + dayIndex * GenDate.TicksPerDay;
                    var forecast = new TemperatureForecastForYear(tileId, dayTicks, dayIndex);
                    temperatures.Add(forecast);
                }
            }
            finally
            {
                PopTickAbs();
            }

            return temperatures;
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