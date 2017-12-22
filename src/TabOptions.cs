using PrepareLanding.Core.Gui.Tab;
using PrepareLanding.Core.Gui.World;
using UnityEngine;
using Verse;

namespace PrepareLanding
{
    public class TabOptions : TabGuiUtility
    {
        // game data
        private readonly GameData.GameData _gameData;

        public TabOptions(GameData.GameData gameData, float columnSizePercent = 0.25f) :
            base(columnSizePercent)
        {
            _gameData = gameData;
        }

        /// <summary>Gets whether the tab can be draw or not.</summary>
        public override bool CanBeDrawn { get; set; } = true;

        /// <summary>A unique identifier for the Tab.</summary>
        public override string Id => "Options";

        /// <summary>The name of the tab (that is actually displayed at its top).</summary>
        public override string Name => Id;

        /// <summary>Draw the content of the tab.</summary>
        /// <param name="inRect">The <see cref="T:UnityEngine.Rect" /> in which to draw the tab content.</param>
        public override void Draw(Rect inRect)
        {
            Begin(inRect);
            DrawFilterOptions();
            DrawTileHighlighterOptions();
            End();
        }

        private void DrawFilterOptions()
        {
            DrawEntryHeader("Filter Options", backgroundColor: Color.cyan);

            var allowLiveFiltering = _gameData.UserData.Options.AllowLiveFiltering;
            ListingStandard.CheckboxLabeled("Allow Live Filtering", ref allowLiveFiltering,
                "[Warning: CPU heavy] Allow filtering without pressing the \"Filter\" button.");
            _gameData.UserData.Options.AllowLiveFiltering = allowLiveFiltering;

            var allowImpassableHilliness = _gameData.UserData.Options.AllowImpassableHilliness;
            ListingStandard.CheckboxLabeled("Allow Impassable Tiles", ref allowImpassableHilliness,
                "Allow selection and filtering of impassable tiles.");
            _gameData.UserData.Options.AllowImpassableHilliness = allowImpassableHilliness;

            var disablePreFilterCheck = _gameData.UserData.Options.DisablePreFilterCheck;
            ListingStandard.CheckboxLabeled("Disable PreFilter Check", ref disablePreFilterCheck,
                "Disable the check where Biomes and Terrains must be selected with a world coverage >= 50%.");
            _gameData.UserData.Options.DisablePreFilterCheck = disablePreFilterCheck;

            var viewPartialOffNoSelect = _gameData.UserData.Options.ViewPartialOffNoSelect;
            ListingStandard.CheckboxLabeled("View PartialOffNoSelect", ref viewPartialOffNoSelect,
                "Allow to view the PartialOffNoSelect on the GUI for grouped three state items.");
            _gameData.UserData.Options.ViewPartialOffNoSelect = viewPartialOffNoSelect;

            var resetAllFieldsOnNewGeneratedWorld = _gameData.UserData.Options.ResetAllFieldsOnNewGeneratedWorld;
            ListingStandard.CheckboxLabeled("Reset all filters on new world", ref resetAllFieldsOnNewGeneratedWorld,
                "If ON, all filters are reset to their default state on a new generated world, otherwise the filters are kept in their previous state.");
            _gameData.UserData.Options.ResetAllFieldsOnNewGeneratedWorld = resetAllFieldsOnNewGeneratedWorld;

            var showFilterHeaviness = _gameData.UserData.Options.ShowFilterHeaviness;
            ListingStandard.CheckboxLabeled("Show Filter Heaviness", ref showFilterHeaviness,
                "Show filter heaviness (possible filter CPU calculation heaviness) on filter header in the GUI.");
            _gameData.UserData.Options.ShowFilterHeaviness = showFilterHeaviness;

            var allowInvalidTilesForNewSettlement = _gameData.UserData.Options.AllowInvalidTilesForNewSettlement;
            ListingStandard.CheckboxLabeled("Allow Invalid Tiles for New Settlement",
                ref allowInvalidTilesForNewSettlement,
                "If on, this prevents a last pass that would have removed tiles deemed as not valid for a new settlement.");
            _gameData.UserData.Options.AllowInvalidTilesForNewSettlement = allowInvalidTilesForNewSettlement;

            // coordinates window
            GoToCoordinatesOption();
        }

        private void GoToCoordinatesOption()
        {
            if (!ListingStandard.ButtonText("Open Coordinates Window"))
                return;

            if (Coordinates.MainWindow.IsInWindowStack)
                return;

            if (!Coordinates.MainWindow.CanBeDisplayed)
                return;

            var coordinatesWindow = new Coordinates.MainWindow();
            Find.WindowStack.Add(coordinatesWindow);
        }

        private void DrawTileHighlighterOptions()
        {
            DrawEntryHeader("Tile Highlighter Options", backgroundColor: Color.cyan);

            var disableTileHighligthing = _gameData.UserData.Options.DisableTileHighlighting;
            ListingStandard.CheckboxLabeled("Disable Tile Highlighting", ref disableTileHighligthing,
                "Disable tile highlighting altogether.");
            _gameData.UserData.Options.DisableTileHighlighting = disableTileHighligthing;

            var disableTileBlinking = _gameData.UserData.Options.DisableTileBlinking;
            ListingStandard.CheckboxLabeled("Disable Tile Blinking", ref disableTileBlinking,
                "Disable tile blinking (\"breathing\") for filtered tiles on the world map.");
            _gameData.UserData.Options.DisableTileBlinking = disableTileBlinking;

            // allow to show the debug tile ID on the highlighted tile
            var showDebugTileId = _gameData.UserData.Options.ShowDebugTileId;
            ListingStandard.CheckboxLabeled("Show Debug Tile ID", ref showDebugTileId,
                "Show the Debug Tile ID for the highlighted tiles.");
            _gameData.UserData.Options.ShowDebugTileId = showDebugTileId;

            var bypassMaxHighlightedTiles = _gameData.UserData.Options.BypassMaxHighlightedTiles;
            ListingStandard.CheckboxLabeled("Bypass TileHighlighter Maximum", ref bypassMaxHighlightedTiles,
                $"Allow highlighting more than {TileHighlighter.MaxHighlightedTiles} tiles.");
            _gameData.UserData.Options.BypassMaxHighlightedTiles = bypassMaxHighlightedTiles;
        }
    }
}