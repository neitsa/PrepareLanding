using System.Collections.Generic;
using PrepareLanding.Filters;
using RimWorld;
using Verse;

namespace PrepareLanding.GameData
{
    public class WorldData
    {
        private readonly DefData _defData;

        public TemperatureData TemperatureData { get; }

        public string WorldSeedString { get; private set; }

        public float WorldCoverage { get; private set; }

        public int WorldSeed { get; private set; }

        public Dictionary<BiomeDef, int> NumberOfTilesByBiome = new Dictionary<BiomeDef, int>();

        public WorldData(DefData defData)
        {
            _defData = defData;

            PrepareLanding.Instance.OnWorldGenerated += OnWorldGenerated;

            TemperatureData = new TemperatureData(defData);
        }

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
    }
}
