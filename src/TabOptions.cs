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
        public override string Id => "PLOPT_Options".Translate();

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
            DrawEntryHeader("PLOPT_FilterOptions".Translate(), backgroundColor: Color.cyan);

            var allowLiveFiltering = _gameData.UserData.Options.AllowLiveFiltering;
            ListingStandard.CheckboxLabeled("PLOPT_AllowLiveFiltering".Translate(), ref allowLiveFiltering,
                "PLOPT_AllowLiveFilteringToolTip".Translate());
            _gameData.UserData.Options.AllowLiveFiltering = allowLiveFiltering;

            var allowImpassableHilliness = _gameData.UserData.Options.AllowImpassableHilliness;
            ListingStandard.CheckboxLabeled("PLOPT_AllowImpassableTiles".Translate(), ref allowImpassableHilliness,
                "PLOPT_AllowImpassableTilesToolTip".Translate());
            _gameData.UserData.Options.AllowImpassableHilliness = allowImpassableHilliness;

            var disablePreFilterCheck = _gameData.UserData.Options.DisablePreFilterCheck;
            ListingStandard.CheckboxLabeled("PLOPT_DisablePreFilterCheck".Translate(), ref disablePreFilterCheck,
                "PLOPT_DisablePreFilterCheckToolTip".Translate());
            _gameData.UserData.Options.DisablePreFilterCheck = disablePreFilterCheck;

            var viewPartialOffNoSelect = _gameData.UserData.Options.ViewPartialOffNoSelect;
            ListingStandard.CheckboxLabeled("PLOPT_ViewPartialOffNoSelect".Translate(), ref viewPartialOffNoSelect,
                "PLOPT_ViewPartialOffNoSelectToolTip".Translate());
            _gameData.UserData.Options.ViewPartialOffNoSelect = viewPartialOffNoSelect;

            var resetAllFieldsOnNewGeneratedWorld = _gameData.UserData.Options.ResetAllFieldsOnNewGeneratedWorld;
            ListingStandard.CheckboxLabeled("PLOPT_ResetAllFiltersOnNewWorld".Translate(), ref resetAllFieldsOnNewGeneratedWorld,
                "PLOPT_ResetAllFiltersOnNewWorldToolTip".Translate());
            _gameData.UserData.Options.ResetAllFieldsOnNewGeneratedWorld = resetAllFieldsOnNewGeneratedWorld;

            var showFilterHeaviness = _gameData.UserData.Options.ShowFilterHeaviness;
            ListingStandard.CheckboxLabeled("PLOPT_ShowFilterHeaviness".Translate(), ref showFilterHeaviness,
                "PLOPT_ShowFilterHeavinessToolTip".Translate());
            _gameData.UserData.Options.ShowFilterHeaviness = showFilterHeaviness;

            var allowInvalidTilesForNewSettlement = _gameData.UserData.Options.AllowInvalidTilesForNewSettlement;
            ListingStandard.CheckboxLabeled("PLOPT_AllowInvalidTilesForNewSettlement".Translate(),
                ref allowInvalidTilesForNewSettlement, "PLOPT_AllowInvalidTilesForNewSettlementToolTip".Translate());
            _gameData.UserData.Options.AllowInvalidTilesForNewSettlement = allowInvalidTilesForNewSettlement;
        }

        private void DrawTileHighlighterOptions()
        {
            DrawEntryHeader("PLOPT_TileHighlighterOptions".Translate(), backgroundColor: Color.cyan);

            var disableTileHighligthing = _gameData.UserData.Options.DisableTileHighlighting;
            ListingStandard.CheckboxLabeled("PLOPT_DisableTileHighlighting".Translate(), ref disableTileHighligthing,
                "PLOPT_DisableTileHighlightingToolTip".Translate());
            _gameData.UserData.Options.DisableTileHighlighting = disableTileHighligthing;

            var disableTileBlinking = _gameData.UserData.Options.DisableTileBlinking;
            ListingStandard.CheckboxLabeled("PLOPT_DisableTileBlinking".Translate(), ref disableTileBlinking,
                "PLOPT_DisableTileBlinkingToolTip".Translate());
            _gameData.UserData.Options.DisableTileBlinking = disableTileBlinking;

            // allow to show the debug tile ID on the highlighted tile
            var showDebugTileId = _gameData.UserData.Options.ShowDebugTileId;
            ListingStandard.CheckboxLabeled("PLOPT_ShowDebugTileId".Translate(), ref showDebugTileId,
                "PLOPT_ShowDebugTileIdToolTip".Translate());
            _gameData.UserData.Options.ShowDebugTileId = showDebugTileId;

            var bypassMaxHighlightedTiles = _gameData.UserData.Options.BypassMaxHighlightedTiles;
            var msgBypassTileHighlighterMaximumToolTip = string.Format(
                "PLOPT_BypassTileHighlighterMaximumToolTip".Translate(), TileHighlighter.MaxHighlightedTiles);
            ListingStandard.CheckboxLabeled("PLOPT_BypassTileHighlighterMaximum".Translate(), ref bypassMaxHighlightedTiles,
                msgBypassTileHighlighterMaximumToolTip);
            _gameData.UserData.Options.BypassMaxHighlightedTiles = bypassMaxHighlightedTiles;
        }
    }
}