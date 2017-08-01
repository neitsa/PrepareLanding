using System.Collections.Generic;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Core.Gui.Tab;
using PrepareLanding.Gui.World;
using UnityEngine;
using Verse;

namespace PrepareLanding
{
    public class TabOptions : TabGuiUtility
    {
        // split percentage for the 3 elements of the "go to tile" entry.
        private readonly List<float> _goToTileSplitPct = new List<float> {0.5f, 0.35f, 0.15f};
        // hold user choices
        private readonly UserData _userData;
        // default tile number (for "go to tile").
        private int _tileNumber;
        // tile number, as string (for "go to tile")
        private string _tileNumberString = string.Empty;

        public TabOptions(UserData userData, float columnSizePercent = 0.25f) :
            base(columnSizePercent)
        {
            _userData = userData;
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
            DrawOptions();
            End();
        }

        protected virtual void DrawOptions()
        {
            DrawEntryHeader("Filter Options", backgroundColor: Color.cyan);

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

            var disableTileHighligthing = _userData.Options.DisableTileHighlighting;
            ListingStandard.CheckboxLabeled("Disable Tile Highligthing", ref disableTileHighligthing,
                "Disable tile highlighting altogether.");
            _userData.Options.DisableTileHighlighting = disableTileHighligthing;

            var disableTileBlinking = _userData.Options.DisableTileBlinking;
            ListingStandard.CheckboxLabeled("Disable Tile Blinking", ref disableTileBlinking,
                "Disable tile blinking (\"breathing\") for filtered tiles on the world map.");
            _userData.Options.DisableTileBlinking = disableTileBlinking;

            var showFilterHeaviness = _userData.Options.ShowFilterHeaviness;
            ListingStandard.CheckboxLabeled("Show Filter Heaviness", ref showFilterHeaviness,
                "Show filter heaviness (possible filter CPU calculation heaviness) on filter header in the GUI.");
            _userData.Options.ShowFilterHeaviness = showFilterHeaviness;

            var allowInvalidTilesForNewSettlement = _userData.Options.AllowInvalidTilesForNewSettlement;
            ListingStandard.CheckboxLabeled("Allow Invalid Tiles for New Settlement",
                ref allowInvalidTilesForNewSettlement,
                "If on, this prevents a last pass that would have removed tiles deemed as not valid for a new settlement.");
            _userData.Options.AllowInvalidTilesForNewSettlement = allowInvalidTilesForNewSettlement;

            var goToTileOptionRectSpace = ListingStandard.GetRect(30f);
            var rects = goToTileOptionRectSpace.SplitRectWidth(_goToTileSplitPct);
            Widgets.Label(rects[0], "Go to Tile:");
            Widgets.TextFieldNumeric(rects[1], ref _tileNumber, ref _tileNumberString, -1, 300000);
            if (Widgets.ButtonText(rects[2], "Go!"))
            { 
                if ((_tileNumber < 0) || (_tileNumber >= Find.WorldGrid.TilesCount))
                {
                    Messages.Message($"Out of Range: {_tileNumber}; Range: [0, {Find.WorldGrid.TilesCount}).",
                        MessageSound.RejectInput);
                }
                else
                {
                    Find.WorldInterface.SelectedTile = _tileNumber;
                    Find.WorldCameraDriver.JumpTo(Find.WorldGrid.GetTileCenter(Find.WorldInterface.SelectedTile));
                }
            }
        }
    }
}