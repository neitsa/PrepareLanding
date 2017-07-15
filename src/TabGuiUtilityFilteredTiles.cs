using System;
using System.Text;
using PrepareLanding.Extensions;
using PrepareLanding.Gui.Tab;
using UnityEngine;
using Verse;

namespace PrepareLanding
{
    public class TabGuiUtilityFilteredTiles : TabGuiUtility
    {
        private Vector2 _scrollPosMatchingTiles;

        private int _selectedTileIndex = -1;

        public TabGuiUtilityFilteredTiles(float columnSizePercent) : base(columnSizePercent)
        {
        }

        /// <summary>A unique identifier for the Tab.</summary>
        public override string Id => "FilteredTiles";

        /// <summary>The name of the tab (that is actually displayed at its top).</summary>
        public override string Name => "Filtered Tiles";

        /// <summary>Draw the content of the tab.</summary>
        /// <param name="inRect">The <see cref="T:UnityEngine.Rect" /> in which to draw the tab content.</param>
        public override void Draw(Rect inRect)
        {
            Begin(inRect);
            DrawFilteredTiles();
            End();
        }

        protected void DrawFilteredTiles()
        {
            DrawEntryHeader("Filtered Tiles", backgroundColor: Color.yellow);

            if (ListingStandard.ButtonText("Clear Filtered Tiles"))
            {
                PrepareLanding.Instance.TileFilter.ClearMatchingTiles();
            }

            ListingStandard.Gap();

            var matchingTiles = PrepareLanding.Instance.TileFilter.AllMatchingTiles;
            var matchingTilesCount = matchingTiles.Count;

            if (matchingTilesCount == 0)
                return;

            var maxHeight = InRect.height - ListingStandard.CurHeight - 30f;
            var scrollHeight = matchingTilesCount * DefaultElementHeight;
            scrollHeight = Mathf.Max(maxHeight, scrollHeight);

            var innerLs = ListingStandard.BeginScrollView(maxHeight, scrollHeight, ref _scrollPosMatchingTiles);

            var stringBuilder = new StringBuilder();

            var maxCount = Math.Min(matchingTilesCount, 20);
            for (var i = 0; i < maxCount; i++)
            {
                var selectedTileId = matchingTiles[i];
                var selectedTile = Find.World.grid[selectedTileId];

                var vector = Find.WorldGrid.LongLatOf(selectedTileId);
                stringBuilder.Append($"{i}: {vector.y.ToStringLatitude()} {vector.x.ToStringLongitude()} - {selectedTile.biome.LabelCap} ; {selectedTileId}");

                var labelRect = innerLs.GetRect(DefaultElementHeight);

                var selected = i == _selectedTileIndex;
                if (Gui.Widgets.LabelSelectable(labelRect, stringBuilder.ToString(), ref selected))
                {
                    _selectedTileIndex = i;
                    Find.WorldInterface.SelectedTile = selectedTileId;
                    Find.WorldCameraDriver.JumpTo(Find.WorldGrid.GetTileCenter(Find.WorldInterface.SelectedTile));
                }

                innerLs.GapLine(6f);

                stringBuilder.Length = 0;
            }

            ListingStandard.EndScrollView(innerLs);
        }
    }
}
