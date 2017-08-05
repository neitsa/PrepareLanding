using System;
using System.Collections.Generic;
using System.ComponentModel;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PrepareLanding.Core.Gui.World
{
    public class TileHighlighter : IDisposable
    {
        /// <summary>
        ///     Maximum number of tiles that can be highlighted at once.
        /// </summary>
        public const int MaxHighlightedTiles = 10000;

        public const float DefaultTileHighlightingAlphaValue = 0.5f;

        public const float DefaultBlinkDuration = 2.0f;

        private const float MaxDistToCameraToDisplayLabel = 60f;

        private readonly FilterOptions _filterOptions;

        private readonly Material _defaultMaterial = new Material(WorldMaterials.SelectedTile);

        public List<int> HighlightedTilesIds = new List<int>();

        private Color _materialHighlightingColor = Color.green;

        public WorldLayer HighlightedTilesWorldLayer { get; set; }

        public float BlinkDuration { get; set; }

        public float BlinkTick => BlinkDuration / 60f;

        public TileHighlighter(FilterOptions filterOptions)
        {
            _filterOptions = filterOptions;
            _defaultMaterial.color = TileHighlightingColor;
            _filterOptions.PropertyChanged += OnOptionChanged;

            PrepareLanding.Instance.OnWorldInterfaceOnGui += HighlightedTileDrawerOnGui;

            BlinkDuration = DefaultBlinkDuration;
        }

        public bool BypassMaxHighlightedTiles { get; set; }

        public bool DisableTileBlinking { get; set; }

        public bool DisableTileHighlighting { get; set; }

        public bool ShowDebugTileId { get; set; }

        public Color TileHighlightingColor
        {
            get { return _materialHighlightingColor; }
            set
            {
                _materialHighlightingColor = value;
                _defaultMaterial.color = _materialHighlightingColor;
            }
        }

        public static Vector2 ScreenPos(int tileId)
        {
            var tileCenter = Find.WorldGrid.GetTileCenter(tileId);
            return GenWorldUI.WorldToUIPosition(tileCenter);
        }

        public static bool VisibleForCamera(int tileId)
        {
            var rect = new Rect(0f, 0f, UI.screenWidth, UI.screenHeight);
            return rect.Contains(ScreenPos(tileId));
        }

        public static float DistanceToCamera(int tileId)
        {
            var tileCenter = Find.WorldGrid.GetTileCenter(tileId);
            return Vector3.Distance(Find.WorldCamera.transform.position, tileCenter);
        }

        public void HighlightedTileDrawerOnGui()
        {
            if (DisableTileHighlighting)
                return;

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);

            foreach (var tile in HighlightedTilesIds)
            {
                if(!VisibleForCamera(tile))
                    continue;

                if (!(DistanceToCamera(tile) <= MaxDistToCameraToDisplayLabel))
                    continue;

                var screenPos = ScreenPos(tile);
                var rect = new Rect(screenPos.x - 20f, screenPos.y - 20f, 40f, 40f);
                Verse.Widgets.Label(rect, ShowDebugTileId ? tile.ToString(): "X");
            }

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public void HighlightTileList(List<int> tileList)
        {
            // do not highlight if disabled
            if (DisableTileHighlighting)
            {
                RemoveAllTiles();
                return;
            }

            // do not highlight too many tiles (otherwise the slow down is noticeable)
            if (!BypassMaxHighlightedTiles && (tileList.Count > MaxHighlightedTiles))
            {
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.AppendErrorMessage(
                    $"Too many tiles to highlight ({tileList.Count}). Try to add more filters to decrease the actual count.");
                return;
            }

            HighlightedTilesIds.AddRange(tileList);

            Find.World.renderer.SetDirty<WorldLayerHighlightedTiles>();
        }

        public void RemoveAllTiles()
        {
            HighlightedTilesIds.Clear();
            Find.World.renderer.SetDirty<WorldLayerHighlightedTiles>();
        }

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

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue)
                return;

            if (disposing)
            {

                PrepareLanding.Instance.OnWorldInterfaceOnGui -= HighlightedTileDrawerOnGui;
                //PrepareLanding.Instance.OnWorldInterfaceUpdate -= HighlightedTileDrawerUpdate;
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