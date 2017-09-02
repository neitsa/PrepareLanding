using System.Linq;
using System.Text;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Core.Gui.Tab;
using UnityEngine;
using Verse;

namespace PrepareLanding
{
    public class TabInfo : TabGuiUtility
    {
        private readonly GameData.GameData _gameData;
        private readonly GUIStyle _styleFilterInfo;
        private readonly GUIStyle _styleWorldInfo;
        private Vector2 _scrollPosFilterInfo;

        private Vector2 _scrollPosWorldInfo;

        private string _worldInfo;

        public TabInfo(GameData.GameData gameData, float columnSizePercent = 0.25f) :
            base(columnSizePercent)
        {
            _gameData = gameData;

            // make new text styles
            _styleWorldInfo = new GUIStyle(Text.textAreaReadOnlyStyles[1])
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

        /// <summary>Gets whether the tab can be draw or not.</summary>
        public override bool CanBeDrawn { get; set; } = true;

        public override string Id => "WorldInfo";

        public override string Name => "World Info";

        public string WorldInfo => _worldInfo ?? (_worldInfo = BuildWorldInfo());

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

            /*
             * Highest / lowest value for all features.
             */

            foreach (var feature in _gameData.WorldData.WorldFeatures)
            {
                var featureName = feature.FeatureName;
                stringBuilder.AppendLine(featureName);

                var lowestFeatureKvp = feature.WorldTilesFeatures.First();
                var vectorLongLat = Find.WorldGrid.LongLatOf(lowestFeatureKvp.Key);
                stringBuilder.AppendLine(
                    $"\tWorld Lowest {featureName}: {lowestFeatureKvp.Value:F1} {feature.FeatureMeasureUnit}\n\t    ➠[tile: {lowestFeatureKvp.Key}; {vectorLongLat.y.ToStringLatitude()} - {vectorLongLat.x.ToStringLongitude()}]");

                var highestFeatureKvp = feature.WorldTilesFeatures.Last();
                vectorLongLat = Find.WorldGrid.LongLatOf(highestFeatureKvp.Key);
                stringBuilder.AppendLine(
                    $"\tWorld Highest {featureName}: {highestFeatureKvp.Value:F1} {feature.FeatureMeasureUnit}\n\t    ➠[tile: {highestFeatureKvp.Key}; {vectorLongLat.y.ToStringLatitude()} - {vectorLongLat.x.ToStringLongitude()}]");
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

            var maxOuterRectHeight = InRect.height - ListingStandard.CurHeight - 30f;

            ListingStandard.ScrollableTextArea(maxOuterRectHeight, WorldInfo, ref _scrollPosWorldInfo, _styleWorldInfo,
                16f);
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