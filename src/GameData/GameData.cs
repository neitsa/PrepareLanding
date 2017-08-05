namespace PrepareLanding.GameData
{
    public class GameData
    {
        public DefData DefData { get; }

        public WorldData WorldData { get; }

        public GameData(FilterOptions filterOptions)
        {
            DefData = new DefData(filterOptions);
            WorldData = new WorldData();
        }
    }
}
