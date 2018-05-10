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
        private Vector2 _scrollPosWorldRecords;

        private string _worldInfo;
        private string _worldRecords;

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

        public override string Name => "PLMWINF_WorldInfo".Translate();

        private string WorldInfo => _worldInfo ?? (_worldInfo = BuildWorldInfo());

        private string WolrdRecords => _worldRecords ?? (_worldRecords = BuildWorldRecords());

        private string BuildWorldInfo()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"{"PLMWINF_PlanetName".Translate()}: {Find.World.info.name}");
            stringBuilder.AppendLine($"{"PlanetSeed".Translate()}: {Find.World.info.seedString}");
            stringBuilder.AppendLine(
                $"{"PlanetCoverageShort".Translate()}: {Find.World.info.planetCoverage.ToStringPercent()}");
            stringBuilder.AppendLine($"{"PLMWINF_TotalTiles".Translate()}: {Find.World.grid.TilesCount}");

            // if nothing filtered yet, then bail out now.
            if (PrepareLanding.Instance.TileFilter == null)
                return stringBuilder.ToString();

            if (PrepareLanding.Instance.TileFilter.AllValidTilesReadOnly != null)
                stringBuilder.AppendLine(
                    $"{"PLMWINF_SettleableTiles".Translate()}: {PrepareLanding.Instance.TileFilter.AllValidTilesReadOnly.Count}");
            if (PrepareLanding.Instance.TileFilter.AllTilesWithRiver != null)
                stringBuilder.AppendLine(
                    $"{"PLMWINF_TilesWithRivers".Translate()}: {PrepareLanding.Instance.TileFilter.AllTilesWithRiver.Count}");
            if (PrepareLanding.Instance.TileFilter.AllTilesWithRoad != null)
                stringBuilder.AppendLine(
                    $"{"PLMWINF_TilesWithRoads".Translate()}: {PrepareLanding.Instance.TileFilter.AllTilesWithRoad.Count}");

            stringBuilder.AppendLine($"{"PLMWINF_Biomes".Translate()}:");
            var biomes = _gameData.DefData.BiomeDefs;

            foreach (var biome in biomes)
            {
                stringBuilder.AppendLine($"    * {biome.LabelCap}");
                var count = _gameData.WorldData.NumberOfTilesByBiome[biome];
                stringBuilder.AppendLine($"        - {"PLMWINF_NumberOfTiles".Translate()} ➠ {count}");
                stringBuilder.AppendLine($"        - {"AverageDiseaseFrequency".Translate()} ➠ {(RimWorld.GenDate.DaysPerYear / biome.diseaseMtbDays):F1} {"PerYear".Translate()}");
            }

            return stringBuilder.ToString();
        }

        private string BuildWorldRecords()
        {
            /*
             * Highest / lowest value for all characteristics.
             */
             var stringBuilder = new StringBuilder();

            foreach (var characteristicData in _gameData.WorldData.WorldCharacteristics)
            {
                var characteristicName = characteristicData.CharacteristicName;
                stringBuilder.AppendLine(characteristicName);

                var lowestCharacteristicKvp = characteristicData.WorldTilesCharacteristics.First();
                var vectorLongLat = Find.WorldGrid.LongLatOf(lowestCharacteristicKvp.Key);
                stringBuilder.AppendLine(
                    $"\t{"PLMWINF_WorldLowest".Translate()} {characteristicName}: {lowestCharacteristicKvp.Value:F1} {characteristicData.CharacteristicMeasureUnit}\n\t    ➠ [tile: {lowestCharacteristicKvp.Key}; {vectorLongLat.y.ToStringLatitude()} - {vectorLongLat.x.ToStringLongitude()}]");

                var highestCharacteristicKvp = characteristicData.WorldTilesCharacteristics.Last();
                vectorLongLat = Find.WorldGrid.LongLatOf(highestCharacteristicKvp.Key);
                stringBuilder.AppendLine(
                    $"\t{"PLMWINF_WorldHighest".Translate()} {characteristicName}: {highestCharacteristicKvp.Value:F1} {characteristicData.CharacteristicMeasureUnit}\n\t    ➠ [tile: {highestCharacteristicKvp.Key}; {vectorLongLat.y.ToStringLatitude()} - {vectorLongLat.x.ToStringLongitude()}]");
            }

            return stringBuilder.ToString();
        }

        public override void Draw(Rect inRect)
        {
            Begin(inRect);
            DrawWorldInfo();
            DrawWorldRecord();
            NewColumn();
            DrawFilterInfo();
            End();
        }

        private void DrawFilterInfo()
        {
            DrawEntryHeader("PLMWINF_FilterInfo".Translate(), backgroundColor: Color.yellow);

            if (ListingStandard.ButtonText("PLMWINF_ClearInfo".Translate()))
                PrepareLanding.Instance.TileFilter.FilterInfoLogger.Clear();

            ListingStandard.Gap();

            var text = PrepareLanding.Instance.TileFilter.FilterInfoLogger.Text;
            if (text.NullOrEmpty())
                return;

            var maxOuterRectHeight = InRect.height - ListingStandard.CurHeight - DefaultElementHeight;

            ListingStandard.ScrollableTextArea(maxOuterRectHeight, text, ref _scrollPosFilterInfo, _styleFilterInfo,
                DefaultScrollableViewShrinkWidth);
        }

        private void DrawWorldInfo()
        {
            var remSpace = DrawEntryHeader("PLMWINF_WorldInfo".Translate(), backgroundColor: Color.yellow);

            var maxOuterRectHeight = (InRect.height - MainWindow.SpaceForBottomButtons - remSpace) / 2;

            ListingStandard.ScrollableTextArea(maxOuterRectHeight, WorldInfo, ref _scrollPosWorldInfo, _styleWorldInfo,
                DefaultScrollableViewShrinkWidth);
        }

        private void DrawWorldRecord()
        {
            var currHeight = DrawEntryHeader("PLMWINF_WorldRecords".Translate(), backgroundColor: Color.yellow);

            var maxOuterRectHeight = currHeight - MainWindow.SpaceForBottomButtons - DefaultElementHeight;

            ListingStandard.ScrollableTextArea(maxOuterRectHeight, WolrdRecords, ref _scrollPosWorldRecords, _styleWorldInfo,
                DefaultScrollableViewShrinkWidth);
        }

        /// <summary>
        ///     Called when a new world map has been generated.
        /// </summary>
        private void RebuildWorldInfo()
        {
            _worldInfo = BuildWorldInfo();
            _worldRecords = BuildWorldRecords();
        }
    }
}