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

        private const float MaxDistToCameraToDisplayLabel = 50f;

        private readonly FilterOptions _filterOptions;

        private readonly Material _defaultMaterial = new Material(WorldMaterials.SelectedTile);

        private readonly List<HighlightedTile> _highlightedTiles = new List<HighlightedTile>();

        private Color _materialColor = Color.green;

        public TileHighlighter(FilterOptions filterOptions)
        {
            _filterOptions = filterOptions;
            _defaultMaterial.color = TileColor;
            _filterOptions.PropertyChanged += OnOptionChanged;
        }

        public bool BypassMaxHighlightedTiles { get; set; }

        public bool DisableTileBlinking { get; set; }

        public bool DisableTileHighlighting { get; set; }

        public bool ShowDebugTileId { get; set; }

        public Color TileColor
        {
            get { return _materialColor; }
            set
            {
                _materialColor = value;
                _defaultMaterial.color = _materialColor;
            }
        }

        public void HighlightedTileDrawerOnGui()
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = new Color(1f, 1f, 1f, 0.5f);

            foreach (var tile in _highlightedTiles)
                if (tile.DistanceToCamera <= MaxDistToCameraToDisplayLabel)
                    tile.OnGui();

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public void HighlightedTileDrawerUpdate()
        {
            if (DisableTileHighlighting)
                return;

            for (var i = 0; i < _highlightedTiles.Count; i++)
                _highlightedTiles[i].Draw();
        }

        public void HighlightTile(int tile, Material mat, string text = null)
        {
            var debugTile = new HighlightedTile(tile, mat, text);
            _highlightedTiles.Add(debugTile);
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

            foreach (var tileId in tileList)
                HighlightTile(tileId, _defaultMaterial, ShowDebugTileId ? tileId.ToString() : "X");
        }

        public void RemoveAllTiles()
        {
            _highlightedTiles.Clear();
        }

        internal void HighlightTile(int tile, float colorPct = 0f, string text = null)
        {
            var highlightedTile = new HighlightedTile(tile, colorPct, text);
            _highlightedTiles.Add(highlightedTile);
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
                _highlightedTiles.Clear();

                PrepareLanding.Instance.OnWorldInterfaceOnGui -= HighlightedTileDrawerOnGui;
                PrepareLanding.Instance.OnWorldInterfaceUpdate -= HighlightedTileDrawerUpdate;
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