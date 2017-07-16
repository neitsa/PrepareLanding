using PrepareLanding.Gui.Tab;
using PrepareLanding.Gui.World;
using UnityEngine;

namespace PrepareLanding
{
    public class TabGuiUtilityOptions : TabGuiUtility
    {
        private readonly PrepareLandingUserData _userData;
        private bool _allowLiveFiltering;
        private bool _allowImpassableHilliness;
        private bool _bypassMaxHighlightedTiles;
        private bool _showDebugTileId;

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
            ListingStandard.CheckboxLabeled("Allow Live Filtering", ref _allowLiveFiltering,
                "[Warning: CPU heavy] Allow filtering without pressing the \"Filter\" button.");
            _userData.AllowLiveFiltering = _allowLiveFiltering;

            //TODO: allow unimplemented biomes

            ListingStandard.CheckboxLabeled("Allow Impassable Tiles", ref _allowImpassableHilliness,
                "Allow selection and filtering of impassable tiles.");
            _userData.AllowImpassableHilliness = _allowImpassableHilliness;

            //TODO: allow saving / reading the set of currently applied filters

            // allow to show the debug tile ID on the highlighted tile (instead of 'X')
            ListingStandard.CheckboxLabeled("Show Debug Tile ID", ref _showDebugTileId,
                "Show the Debug Tile ID (instead of 'X') for the highlighted tiles.");
            PrepareLanding.Instance.TileHighlighter.ShowDebugTileId = _showDebugTileId;

            ListingStandard.CheckboxLabeled("Bypass TileHighlighter Maximum", ref _bypassMaxHighlightedTiles,
                $"Allow highlighting more than {TileHighlighter.MaxHighlightedTiles} tiles.");
            PrepareLanding.Instance.TileHighlighter.BypassMaxHighlightedTiles = _bypassMaxHighlightedTiles;
        }
    }
}