using HugsLib;
using PrepareLanding.Core;
using PrepareLanding.Core.Gui.World;
using PrepareLanding.Presets;
using Verse;

namespace PrepareLanding
{
    /// <summary>
    ///     Main Mod class.
    /// </summary>
    public class PrepareLanding : ModBase
    {
        /// <summary>
        ///     Main mod class constructor. Sets up the static instance.
        /// </summary>
        public PrepareLanding()
        {
            Logger.Message("In constructor.");
            if (Instance == null)
                Instance = this;

            // initialize events
            EventHandler = new RimWorldEventHandler();

            // instance used to keep track of (or override) game ticks.
            GameTicks = new GameTicks();

            // Holds various mod options (shown on the 'option' tab on the GUI).
            var filterOptions = new FilterOptions();

            GameData = new GameData.GameData(filterOptions);

            TileFilter = new WorldTileFilter(GameData.UserData);

            // instantiate the main window now
            MainWindow = new MainWindow(GameData);

            // instantiate the tile highlighter
            TileHighlighter = new TileHighlighter(filterOptions);
        }

        /// <summary>
        ///     Main static instance, holding references to useful class instances.
        /// </summary>
        public static PrepareLanding Instance { get; private set; }

        /// <summary>
        ///     Instance used to keep track of (or override) game ticks.
        /// </summary>
        public GameTicks GameTicks { get; private set; }

        /// <summary>
        ///     The filtering class instance used to filter tiles on the world map.
        /// </summary>
        public WorldTileFilter TileFilter { get; private set; }

        /// <summary>
        ///     Allow highlighting filtered tiles on the world map.
        /// </summary>
        public TileHighlighter TileHighlighter { get; private set; }

        /// <summary>
        ///     Holds various game data: some are game, user, or world specific.
        /// </summary>
        public GameData.GameData GameData { get; private set; }

        /// <summary>
        ///     The main GUI window instance.
        /// </summary>
        public MainWindow MainWindow { get; set; }

        //TODO see if this can be set to a "private set" rather than a public one

        /// <inheritdoc />
        public override string ModIdentifier => "PrepareLanding";

        /// <summary>
        ///     The full path of the mod folder.
        /// </summary>
        public string ModFolder => ModContentPack.RootDir;

        /// <summary>
        ///      Instance used to control all useful events from RimWorld.
        /// </summary>
        public readonly RimWorldEventHandler EventHandler;

        /// <summary>
        ///     Set the main instance to null.
        /// </summary>
        public static void RemoveInstance()
        {
            Instance = null;
        }

        /// <inheritdoc />
        /// <summary>
        ///     Called during mod initialization.
        /// </summary>
        public override void Initialize()
        {
            Logger.Message("Initializing.");

            var presetManager = new PresetManager(GameData);
            GameData.PresetManager = presetManager;
        }

        /// <inheritdoc />
        public override void DefsLoaded()
        {
            // do not go further if this mod is not active. This will mostly prevent all other classes from running.
            if (!ModIsActive)
            {
                Log.Message("[PrepareLanding] DefsLoaded: Mod is not active, bailing out.");
                return;
            }

            // alert subscribers that definitions have been loaded.
            EventHandler.OnDefsLoaded();
        }

        /// <inheritdoc />
        public override void WorldLoaded()
        {
            if (!ModIsActive)
                return;

            Log.Message("[PrepareLanding] WorldLoaded (from save).");

            EventHandler.OnWorldLoaded();
        }
    }
}