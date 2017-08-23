using System.Linq;
using System.Text;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Core.Gui.Tab;
using PrepareLanding.Filters;
using UnityEngine;
using Verse;

namespace PrepareLanding
{
    public class TabInfo : TabGuiUtility
    {
        private readonly GUIStyle _styleFilterInfo;
        private readonly GUIStyle _styleWorldInfo;

        private readonly GameData.GameData _gameData;
        private Vector2 _scrollPosFilterInfo;

        private Vector2 _scrollPosWorldInfo;

        private string _worldInfo;

        public TabInfo(GameData.GameData gameData, float columnSizePercent = 0.25f) :
            base(columnSizePercent)
        {
            _gameData = gameData;

            // make new text styles
            _styleWorldInfo = new GUIStyle(Text.textAreaReadOnlyStyles[2])
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                richText = true
            };

            _styleFilterInfo = new GUIStyle(Text.textFieldStyles[1])
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                richText = true
            };

            // make sure world info is generated once again when the tile pre-filter has finished its job.
            PrepareLanding.Instance.TileFilter.OnPrefilterDone += RebuildWorldInfo;
        }

        public string WorldInfo => _worldInfo ?? (_worldInfo = BuildWorldInfo());

        public override string Id => "WorldInfo";

        public override string Name => "World Info";

        /// <summary>Gets whether the tab can be draw or not.</summary>
        public override bool CanBeDrawn { get; set; } = true;

        public string BuildWorldInfo()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Planet Name: {Find.World.info.name}");
            stringBuilder.AppendLine($"{"PlanetSeed".Translate()}: {Find.World.info.seedString}");
            stringBuilder.AppendLine(
                $"{"PlanetCoverageShort".Translate()}: {Find.World.info.planetCoverage.ToStringPercent()}");
            stringBuilder.AppendLine($"Total Tiles: {Find.World.grid.TilesCount}");
            if (PrepareLanding.Instance.TileFilter != null)
            {
                if (PrepareLanding.Instance.TileFilter.AllValidTilesReadOnly != null)
                    stringBuilder.AppendLine(
                        $"Settleable Tiles: {PrepareLanding.Instance.TileFilter.AllValidTilesReadOnly.Count}");
                if (PrepareLanding.Instance.TileFilter.AllTilesWithRiver != null)
                    stringBuilder.AppendLine(
                        $"Tiles with Rivers: {PrepareLanding.Instance.TileFilter.AllTilesWithRiver.Count}");
                if (PrepareLanding.Instance.TileFilter.AllTilesWithRoad != null)
                    stringBuilder.AppendLine(
                        $"Tiles with Roads: {PrepareLanding.Instance.TileFilter.AllTilesWithRoad.Count}");

                stringBuilder.AppendLine("Biomes: (number of tiles)");
                var biomes = _gameData.DefData.BiomeDefs;

                //var biomeNames = biomes.Select(biome => biome.LabelCap).ToList();
                //var longestBiomeName = biomeNames.Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur).Length;

                foreach (var biome in biomes)
                {
                    var count = _gameData.WorldData.NumberOfTilesByBiome[biome];
                    //stringBuilder.AppendLine($"    {biome.LabelCap.PadRight(longestBiomeName)} ➠ {count}");
                    stringBuilder.AppendLine($"    {biome.LabelCap} ➠ {count}");
                }

            }

            return stringBuilder.ToString();
        }

        public override void Draw(Rect inRect)
        {
            Begin(inRect);
            DrawWorldInfo();
            NewColumn();
            DrawFilterInfo();
            End();
        }

        protected virtual void DrawFilterInfo()
        {
            DrawEntryHeader("Filter Info", backgroundColor: Color.yellow);

            if (ListingStandard.ButtonText("Clear Info"))
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.Clear();

            ListingStandard.Gap();

            var text = PrepareLanding.Instance.TileFilter.FilterInfoLogger.Text;
            if (text.NullOrEmpty())
                return;

            var maxOuterRectHeight = InRect.height - ListingStandard.CurHeight - 30f;

            ListingStandard.ScrollableTextArea(maxOuterRectHeight, text, ref _scrollPosFilterInfo, _styleFilterInfo,
                16f);
        }

        protected virtual void DrawWorldInfo()
        {
            DrawEntryHeader("World Info", backgroundColor: Color.yellow);

            var maxHeight = InRect.height - ListingStandard.CurHeight - 30f;
            var scrollHeight = Text.CalcHeight(WorldInfo, ListingStandard.ColumnWidth);
            scrollHeight = Mathf.Max(maxHeight, scrollHeight);

            var innerLs = ListingStandard.BeginScrollView(maxHeight, scrollHeight, ref _scrollPosWorldInfo);

            GUI.TextField(innerLs.GetRect(maxHeight), WorldInfo, _styleWorldInfo);

            ListingStandard.EndScrollView(innerLs);
        }

        /// <summary>
        ///     Called when a new world map has been generated.
        /// </summary>
        protected void RebuildWorldInfo()
        {
            _worldInfo = BuildWorldInfo();
        }
    }
}