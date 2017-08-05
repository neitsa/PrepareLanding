namespace PrepareLanding.GameProps
{
    public class GameProperties
    {
        public DefProperties DefProperties { get; }

        public WorldProperties WorldProperties { get; }


        public GameProperties(FilterOptions filterOptions)
        {
            DefProperties = new DefProperties(filterOptions);
            WorldProperties = new WorldProperties();
        }
    }
}
