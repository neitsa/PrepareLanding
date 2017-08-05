namespace PrepareLanding.GameData
{
    public class GameData
    {
        /// <summary>
        ///     Holds definitions (see <see cref="Verse.Def"/>) from game.
        /// </summary>
        public DefData DefData { get; }

        /// <summary>
        ///     Data specific to a single generated world.
        /// </summary>
        public WorldData WorldData { get; }

        /// <summary>
        ///     User choices on the GUI are kept in this instance.
        /// </summary>
        public UserData UserData { get; }

        public GameData(FilterOptions filterOptions)
        {
            // Definitions (Def) from game. Won't change on a single game; might change between games by using other mods.
            DefData = new DefData(filterOptions);

            // holds user filter choices on the GUI.
            UserData = new UserData(filterOptions);

            // data specific to a single generated world.
            WorldData = new WorldData();
        }
    }
}
