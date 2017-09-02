﻿using System.Collections.Generic;
using PrepareLanding.Filters;
using RimWorld;
using Verse;

namespace PrepareLanding.GameData
{
    public class WorldData
    {
        private readonly DefData _defData;

        public readonly Dictionary<BiomeDef, int> NumberOfTilesByBiome = new Dictionary<BiomeDef, int>();

        public WorldData(DefData defData)
        {
            _defData = defData;

            PrepareLanding.Instance.OnWorldGenerated += OnWorldGenerated;

            TemperatureData = new TemperatureData(defData);
            RainfallData = new RainfallData(defData);
            ElevationData = new ElevationData(defData);


            WorldFeatures.Add(TemperatureData);
            WorldFeatures.Add(RainfallData);
            WorldFeatures.Add(ElevationData);
        }

        public ElevationData ElevationData { get; }

        public RainfallData RainfallData { get; }

        public TemperatureData TemperatureData { get; }

        public float WorldCoverage { get; private set; }

        public List<WorldFeatureData> WorldFeatures { get; } = new List<WorldFeatureData>();

        public int WorldSeed { get; private set; }

        public string WorldSeedString { get; private set; }

        private void OnWorldGenerated()
        {
            WorldCoverage = Find.World.PlanetCoverage;

            WorldSeedString = Find.World.info.seedString;

            WorldSeed = Find.World.info.Seed;

            NumberOfTilesByBiome.Clear();
            foreach (var biomeDef in _defData.BiomeDefs)
            {
                var count = TileFilterBiomes.NumberOfTilesByBiome(biomeDef);
                NumberOfTilesByBiome.Add(biomeDef, count);
            }
        }

        public bool BiomeHasTiles(BiomeDef biomeDef)
        {
            if (!NumberOfTilesByBiome.ContainsKey(biomeDef))
            {
                Log.Error(
                    $"[PrepareLanding] WorldData.BiomeHasTiles: asking for a biome ({biomeDef.LabelCap}) that doesn't exists.");
                return false;
            }

            return NumberOfTilesByBiome[biomeDef] != 0;
        }

        public WorldFeatureData WorldFeatureDataByFeature(MostLeastFeature feature)
        {
            foreach (var worldFeature in WorldFeatures)
                if (worldFeature.Feature == feature)
                    return worldFeature;

            Log.Error($"[PrepareLanding] asked for '{feature}' but couldn't find it...");
            return null;
        }

        public static int DateToTicks(int quadrumDay, Quadrum quadrum, int year)
        {
            if (quadrumDay >= GenDate.DaysPerQuadrum)
            {
                Log.Error($"[PrepareLanding] Called DateToTicks with wrong quadrumDay: {quadrumDay}.");
                return 0;
            }

            var numDays = (int) quadrum * GenDate.DaysPerQuadrum + quadrumDay;
            return DateToTicks(numDays, year);
        }

        public static int DateToTicks(int dayOfYear, int year, bool yearEllapsedSinceStart = false)
        {
            if (dayOfYear >= GenDate.DaysPerYear)
            {
                Log.Error($"[PrepareLanding] Called DateToTicks with wrong dayOfYear: {dayOfYear}.");
                return 0;
            }

            var dayTicks = dayOfYear * GenDate.TicksPerDay;

            var yearTicks = (year - (yearEllapsedSinceStart ? 0 : GenDate.DefaultStartingYear)) * GenDate.TicksPerYear;
            var result = dayTicks + yearTicks;

            return result;
        }

        public static int NowToTicks(int tileId)
        {
            var day = GenLocalDate.DayOfYear(tileId);
            var year = GenLocalDate.Year(tileId);

            var ticks = DateToTicks(day, year);
            if (ticks == 0)
                ticks = 1;

            return ticks;
        }
    }
}