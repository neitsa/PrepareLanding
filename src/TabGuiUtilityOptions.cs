using PrepareLanding.Gui.Tab;
using UnityEngine;

namespace PrepareLanding
{
    public class TabGuiUtilityOptions : TabGuiUtility
    {
        private bool _allowLiveFiltering;
        private bool _showDebugTileId;

        private readonly PrepareLandingUserData _userData;

        public TabGuiUtilityOptions(PrepareLandingUserData userData, float columnSizePercent = 0.25f) : base(columnSizePercent)
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
            //TODO: reset all filters to their default state

            ListingStandard.CheckboxLabeled("Allow Live Filtering", ref _allowLiveFiltering, "Allow filtering without pressing the \"Filter\" button.");
            _userData.AllowLiveFiltering = _allowLiveFiltering;

            //TODO: allow unimplemented biomes

            //TODO: allow selection of impassable tiles

            //TODO: allow saving / reading the set of currently applied filters

            // allow to show the debug tile ID on the highlighted tile (instead of 'X')
            ListingStandard.CheckboxLabeled("Show Debug Tile ID", ref _showDebugTileId, "Show the Debug Tile ID (instead of 'X') for the highlighted tiles.");
            PrepareLanding.Instance.TileDrawer.ShowDebugTileId = _showDebugTileId;
        }
    }
}
