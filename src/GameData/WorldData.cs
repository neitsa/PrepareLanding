using System.Collections.Generic;
using System.Linq;
using PrepareLanding.Filters;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding.GameData
{
    public class WorldData
    {
        private readonly DefData _defData;

        public readonly Dictionary<BiomeDef, int> NumberOfTilesByBiome = new Dictionary<BiomeDef, int>();

        public List<WorldFeature> WorldFeatures;

        public WorldData(DefData defData)
        {
            _defData = defData;

            PrepareLanding.Instance.EventHandler.WorldGeneratedOrLoaded += ExecuteOnWorldGeneratedOrLoaded;

            // note: each of the xxxData below will take care of reloading its data on a new world. No need to do it here.
#if TEMPERATURE_DATA || WORLD_DATA
            TemperatureData = new TemperatureData(_defData);
            WorldCharacteristics.Add(TemperatureData);

#endif

#if RAINFALL_DATA || WORLD_DATA
            RainfallData = new RainfallData(_defData);
            WorldCharacteristics.Add(RainfallData);
#endif

#if ELEVATION_DATA || WORLD_DATA
            ElevationData = new ElevationData(_defData);
            WorldCharacteristics.Add(ElevationData);
#endif
        }


#if ELEVATION_DATA || WORLD_DATA
        public ElevationData ElevationData { get; private set; }
#endif

#if RAINFALL_DATA || WORLD_DATA
        public RainfallData RainfallData { get; private set; }
#endif

#if TEMPERATURE_DATA || WORLD_DATA
        public TemperatureData TemperatureData { get; private set; }
#endif

        public float WorldCoverage { get; private set; }

        public List<WorldCharacteristicData> WorldCharacteristics { get; } = new List<WorldCharacteristicData>();

        public int WorldSeed { get; private set; }

        public string WorldSeedString { get; private set; }

        private void ExecuteOnWorldGeneratedOrLoaded()
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

            WorldFeatures = Find.WorldFeatures.features.OrderBy(feature => feature.name).ToList();
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

        public WorldCharacteristicData WorldCharacteristicDataByCharacteristic(MostLeastCharacteristic characteristic)
        {
            foreach (var worldCharacteristic in WorldCharacteristics)
                if (worldCharacteristic.Characteristic == characteristic)
                    return worldCharacteristic;

            Log.Error($"[PrepareLanding] asked for '{characteristic}' but couldn't find it...");
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