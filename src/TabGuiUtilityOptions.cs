using PrepareLanding.Gui.Tab;
using PrepareLanding.Gui.World;
using UnityEngine;

namespace PrepareLanding
{
    public class TabGuiUtilityOptions : TabGuiUtility
    {
        private readonly PrepareLandingUserData _userData;

        public TabGuiUtilityOptions(PrepareLandingUserData userData, float columnSizePercent = 0.25f) :
            base(columnSizePercent)
        {
            _userData = userData;
        }

        /// <summary>A unique identifier for the Tab.</summary>
        public override string Id => "Options";

        /// <summary>The name of the tab (that is actually displayed at its top).</summary>
        public override string Name => Id;

        /// <summary>Draw the content of the tab.</summary>
        /// <param name="inRect">The <see cref="T:UnityEngine.Rect" /> in which to draw the tab content.</param>
        public override void Draw(Rect inRect)
        {
            Begin(inRect);
            DrawOptions();
            End();
        }

        protected virtual void DrawOptions()
        {
            var allowLiveFiltering = _userData.Options.AllowLiveFiltering;
            ListingStandard.CheckboxLabeled("Allow Live Filtering", ref allowLiveFiltering,
                "[Warning: CPU heavy] Allow filtering without pressing the \"Filter\" button.");
            _userData.Options.AllowLiveFiltering = allowLiveFiltering;

            //TODO: allow unimplemented biomes

            var allowImpassableHilliness = _userData.Options.AllowImpassableHilliness;
            ListingStandard.CheckboxLabeled("Allow Impassable Tiles", ref allowImpassableHilliness,
                "Allow selection and filtering of impassable tiles.");
            _userData.Options.AllowImpassableHilliness = allowImpassableHilliness;

            //TODO: allow saving / reading the set of currently applied filters

            // allow to show the debug tile ID on the highlighted tile (instead of 'X')
            var showDebugTileId = _userData.Options.ShowDebugTileId;
            ListingStandard.CheckboxLabeled("Show Debug Tile ID", ref showDebugTileId,
                "Show the Debug Tile ID (instead of 'X') for the highlighted tiles.");
            _userData.Options.ShowDebugTileId = showDebugTileId;

            var bypassMaxHighlightedTiles = _userData.Options.BypassMaxHighlightedTiles;
            ListingStandard.CheckboxLabeled("Bypass TileHighlighter Maximum", ref bypassMaxHighlightedTiles,
                $"Allow highlighting more than {TileHighlighter.MaxHighlightedTiles} tiles.");
            _userData.Options.BypassMaxHighlightedTiles = bypassMaxHighlightedTiles;

            var disablePreFilterCheck = _userData.Options.DisablePreFilterCheck;
            ListingStandard.CheckboxLabeled("Disable PreFilter Check", ref disablePreFilterCheck,
                "Disable the check where Biomes and Terrains must be selected with a world coverage >= 50%.");
            _userData.Options.DisablePreFilterCheck = disablePreFilterCheck;

            var disableTileBlinking = _userData.Options.DisableTileBlinking;
            ListingStandard.CheckboxLabeled("Disable Tile Blinking", ref disableTileBlinking,
                "Disable tile blinking (\"breathing\") for filtered tiles on the world map.");
            _userData.Options.DisableTileBlinking = disableTileBlinking;

            var showFilterHeaviness = _userData.Options.ShowFilterHeaviness;
            ListingStandard.CheckboxLabeled("Show Filter Heaviness", ref showFilterHeaviness,
                "Show filter heaviness (possible filter CPU calculation heaviness) on filter header in the GUI.");
            _userData.Options.ShowFilterHeaviness = showFilterHeaviness;

        }
    }
}