using System.Linq;
using System.Text;
using PrepareLanding.Extensions;
using PrepareLanding.Filters;
using PrepareLanding.Gui.Tab;
using UnityEngine;
using Verse;

namespace PrepareLanding
{
    public class TabGuiUtilityInfo : TabGuiUtility
    {
        private readonly GUIStyle _style;
        //private readonly PrepareLandingUserData _userData;

        private Vector2 _scrollPosWorldInfo;

        private string _worldInfo;

        public TabGuiUtilityInfo(PrepareLandingUserData userData, float columnSizePercent = 0.25f) :
            base(columnSizePercent)
        {
            //_userData = userData;

            _style = new GUIStyle(Text.textFieldStyles[1])
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                richText = true
            };
        }

        public string WorldInfo => _worldInfo ?? (_worldInfo = BuildWorldInfo());

        public override string Id => "WorldInfo";

        public override string Name => "World Info";

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

                var allValidTiles = PrepareLanding.Instance.TileFilter.AllValidTilesReadOnly?.ToList();
                if (allValidTiles != null)
                {
                    stringBuilder.AppendLine("Biomes: (number of tiles)");
                    var biomes = PrepareLanding.Instance.UserData.BiomeDefs;
                    foreach (var biome in biomes)
                    {
                        var count = TileFilterBiomes.NumberOfTilesByBiome(biome, allValidTiles);
                        stringBuilder.AppendLine($"    {biome.LabelCap} ➠ {count}");
                    }
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

        protected virtual void DrawWorldInfo()
        {
            DrawEntryHeader("World Info", backgroundColor: Color.yellow);

            var originalFont = Text.Font;
            Text.Font = GameFont.Medium;

            var maxHeight = InRect.height - ListingStandard.CurHeight;
            var scrollHeight = Text.CalcHeight(WorldInfo, ListingStandard.ColumnWidth);
            scrollHeight = Mathf.Max(maxHeight, scrollHeight);

            var innerLs = ListingStandard.BeginScrollView(maxHeight, scrollHeight, ref _scrollPosWorldInfo);

            Widgets.Label(innerLs.GetRect(maxHeight), WorldInfo);

            ListingStandard.EndScrollView(innerLs);

            Text.Font = originalFont;
        }

        protected virtual void DrawFilterInfo()
        {
            DrawEntryHeader("Filter Info", backgroundColor: Color.yellow);

            if (ListingStandard.ButtonText("Clear Info"))
            {
                PrepareLanding.Instance.TileFilter.FilterInfo.Clear();
            }

            ListingStandard.Gap();

            var text = PrepareLanding.Instance.TileFilter.FilterInfoText;
            if (text.NullOrEmpty())
                return;

            var maxHeight = InRect.height - ListingStandard.CurHeight - 30f;
            var scrollHeight = Text.CalcHeight(text, ListingStandard.ColumnWidth);
            scrollHeight = Mathf.Max(maxHeight, scrollHeight);

            var innerLs = ListingStandard.BeginScrollView(maxHeight, scrollHeight, ref _scrollPosWorldInfo);

            GUI.TextField(innerLs.GetRect(maxHeight), text, _style);

            ListingStandard.EndScrollView(innerLs);
        }
    }
}