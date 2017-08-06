using PrepareLanding.Presets;

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

        /// <summary>
        ///     User choices on the GUI God Mode tab are kept in this instance.
        /// </summary>
        public GodModeData GodModeData { get; }

        /// <summary>
        ///     Used to load / save filters and options.
        /// </summary>
        public PresetManager PresetManager { get; }

        public GameData(FilterOptions filterOptions)
        {
            // Definitions (Def) from game. Won't change on a single game; might change between games by using other mods.
            DefData = new DefData(filterOptions);

            // holds user choices from the "god mode" tab.
            GodModeData = new GodModeData(DefData);

            // holds user filter choices on the GUI.
            UserData = new UserData(filterOptions);

            // data specific to a single generated world.
            WorldData = new WorldData();

            // Preset manager (load and save presets).
            PresetManager = new PresetManager(this);
        }
    }
}
