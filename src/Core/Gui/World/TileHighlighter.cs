using System;
using System.Collections.Generic;
using System.ComponentModel;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PrepareLanding.Core.Gui.World
{
    /// <summary>
    ///     Class used to highlight selected tiles on the world map.
    /// </summary>
    public class TileHighlighter : IDisposable
    {
        /// <summary>
        ///     Maximum number of tiles that can be highlighted at once.
        /// </summary>
        public const int MaxHighlightedTiles = 10000;

        /// <summary>
        ///     Default alpha value for highlighted tiles; used when tile blinking is disabled.
        /// </summary>
        public const float DefaultTileHighlightingAlphaValue = 0.5f;

        /// <summary>
        ///     Default blink duration, in seconds, for a full blink cycle (alpha value goes to 0 towards 1 then 0).
        /// </summary>
        public const float DefaultBlinkDuration = 2.0f;

        /// <summary>
        ///     Maximum camera distance where tile label (either 'X' or tile ID) is shown.
        /// </summary>
        private const float MaxDistToCameraToDisplayLabel = 60f;

        /// <summary>
        ///     Default material for highlighted tiles.
        /// </summary>
        private readonly Material _defaultMaterial = new Material(WorldMaterials.SelectedTile);

        /// <summary>
        ///     Mod filter options.
        /// </summary>
        private readonly FilterOptions _filterOptions;

        /// <summary>
        ///     List of currently highlighted tiles.
        /// </summary>
        public readonly List<int> HighlightedTilesIds = new List<int>();

        /// <summary>
        ///     Default tile highlighting color.
        /// </summary>
        private Color _materialHighlightingColor = Color.green;

        /// <summary>
        ///     Tile highlighter Constructor.
        /// </summary>
        /// <param name="filterOptions">Filter Mod options instance.</param>
        public TileHighlighter(FilterOptions filterOptions)
        {
            _filterOptions = filterOptions;
            _defaultMaterial.color = TileHighlightingColor;
            _filterOptions.PropertyChanged += OnOptionChanged;

            PrepareLanding.Instance.EventHandler.WorldAboutToBeGenerated += RemoveAllTiles;
            PrepareLanding.Instance.EventHandler.WorldInterfaceOnGui += HighlightedTileDrawerOnGui;

            BlinkDuration = DefaultBlinkDuration;
        }

        /// <summary>
        ///     Blink duration, in seconds, for a full blink cycle (alpha value goes to 0 towards 1 then 0).
        /// </summary>
        public float BlinkDuration { get; set; }

        /// <summary>
        ///     Length of a single blink "tick". Used to calculate the step for alpha value.
        /// </summary>
        public float BlinkTick => BlinkDuration / 60f;

        /// <summary>
        ///     Get or set the ability to highlight more tiles than possible (default value is hard-coded in the settings).
        /// </summary>
        public bool BypassMaxHighlightedTiles { get; set; }

        /// <summary>
        ///     Get or set the ability to disable tile blinking.
        /// </summary>
        public bool DisableTileBlinking { get; set; }

        /// <summary>
        ///     Get or set the ability to disable tile highlighting altogether.
        /// </summary>
        public bool DisableTileHighlighting { get; set; }

        /// <summary>
        ///     The <see cref="WorldLayer" /> used to highlight tiles.
        /// </summary>
        public WorldLayer HighlightedTilesWorldLayer { get; set; }

        /// <summary>
        ///     Get or set the ability to display the tile ID; if true, the tile id is shown on the world map; if false, an 'X' is
        ///     displayed instead.
        /// </summary>
        public bool ShowDebugTileId { get; set; }

        /// <summary>
        ///     Get or set the tile highlighting color.
        /// </summary>
        public Color TileHighlightingColor
        {
            get { return _materialHighlightingColor; }
            set
            {
                _materialHighlightingColor = value;
                _defaultMaterial.color = _materialHighlightingColor;
            }
        }

        /// <summary>
        ///     Get the position of a tile on the screen.
        /// </summary>
        /// <param name="tileId">The tile for which to get the position.</param>
        /// <returns>The position of the given tile on the screen.</returns>
        public static Vector2 ScreenPos(int tileId)
        {
            var tileCenter = Find.WorldGrid.GetTileCenter(tileId);
            return GenWorldUI.WorldToUIPosition(tileCenter);
        }

        /// <summary>
        ///     Check whether or not a tile is visible to the game camera.
        /// </summary>
        /// <param name="tileId">The tile to check for.</param>
        /// <returns>true if the tile is visible for the camera, false otherwise.</returns>
        public static bool VisibleForCamera(int tileId)
        {
            var rect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
            return rect.Contains(ScreenPos(tileId));
        }

        /// <summary>
        ///     Get the distance of a tile to the game camera.
        /// </summary>
        /// <param name="tileId">The tile to check for.</param>
        /// <returns>Distance from the tile to the camera.</returns>
        public static float DistanceToCamera(int tileId)
        {
            var tileCenter = Find.WorldGrid.GetTileCenter(tileId);
            return Vector3.Distance(Find.WorldCamera.transform.position, tileCenter);
        }

        /// <summary>
        ///     Called by the game engine on each OnGui() pass.
        /// </summary>
        public void HighlightedTileDrawerOnGui()
        {
            if (DisableTileHighlighting)
                return;

            if (HighlightedTilesIds.Count == 0)
                return;

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);

            foreach (var tile in HighlightedTilesIds)
            {
                if (!VisibleForCamera(tile))
                    continue;

                if (!(DistanceToCamera(tile) <= MaxDistToCameraToDisplayLabel))
                    continue;

                var screenPos = ScreenPos(tile);
                var rect = new Rect(screenPos.x - 20f, screenPos.y - 20f, 40f, 40f);
                Verse.Widgets.Label(rect, ShowDebugTileId ? tile.ToString() : "X");
            }

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        /// <summary>
        ///     Highlight a list of tiles on the world map.
        /// </summary>
        /// <param name="tileList">The list of tiles to highlight.</param>
        public void HighlightTileList(List<int> tileList)
        {
            // do not highlight if disabled
            if (DisableTileHighlighting)
            {
                RemoveAllTiles();
                return;
            }

            // (Re)start the tick handler
            PrepareLanding.Instance.GameTicks.StartTicking();

            // do not highlight too many tiles (otherwise the slow down is noticeable)
            if (!BypassMaxHighlightedTiles && tileList.Count > MaxHighlightedTiles)
            {
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.AppendErrorMessage(
                    $"Too many tiles to highlight ({tileList.Count}). Try to add more filters to decrease the actual count.");
                return;
            }

            // add the given tiles to the list of tiles to highlight.
            HighlightedTilesIds.AddRange(tileList);

            // set the highlighted tiles world layer as dirty, forcing a new render.
            Find.World.renderer.SetDirty<WorldLayerHighlightedTiles>();
        }

        /// <summary>
        ///     Clear all highlighted tiles.
        /// </summary>
        public void RemoveAllTiles()
        {
            // clear the list of highlighted tiles
            HighlightedTilesIds.Clear();

            // set the world layer has being dirty, forcing a redraw.
            Find.World.renderer.SetDirty<WorldLayerHighlightedTiles>();

            // Stop the tick handler from ticking. It should alleviate the game engine (from continuously ticking).
            PrepareLanding.Instance.GameTicks.StopTicking();
        }

        /// <summary>
        ///     Called when a tile filter option has changed.
        /// </summary>
        /// <param name="sender">The object that is sending the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnOptionChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_filterOptions.DisableTileBlinking):
                    DisableTileBlinking = _filterOptions.DisableTileBlinking;
                    return;
                case nameof(_filterOptions.DisableTileHighlighting):
                    DisableTileHighlighting = _filterOptions.DisableTileHighlighting;
                    Find.World.renderer.SetDirty<WorldLayerHighlightedTiles>();
                    return;
                case nameof(_filterOptions.BypassMaxHighlightedTiles):
                    BypassMaxHighlightedTiles = _filterOptions.BypassMaxHighlightedTiles;
                    break;
                case nameof(_filterOptions.ShowDebugTileId):
                    ShowDebugTileId = _filterOptions.ShowDebugTileId;
                    break;

                default:
                    return;
            }
        }

        #region IDisposable Support

        private bool _disposedValue; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (_disposedValue)
                return;

            if (disposing)
            {
                // stop the ticking engine.
                PrepareLanding.Instance.GameTicks.StopTicking();

                // un-subscribe to  events.
                PrepareLanding.Instance.EventHandler.WorldInterfaceOnGui -= HighlightedTileDrawerOnGui;
                _filterOptions.PropertyChanged -= OnOptionChanged;
            }

            _disposedValue = true;
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}