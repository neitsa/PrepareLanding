using Verse;

namespace PrepareLanding.GameData
{
    public class WorldData
    {
        private DefData _defData;

        public TemperatureData TemperatureData { get; }

        public string WorldSeedString { get; private set; }

        public float WorldCoverage { get; private set; }

        public int WorldSeed { get; private set; }

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
        }
    }
}
