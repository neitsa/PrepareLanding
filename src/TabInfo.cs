using System.Linq;
using System.Text;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Core.Gui;
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
        private int _worldRecordSelectedTileIndex = -1;

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

        public override string Name => "PLMWINF_WorldInfo".Translate();

        private string WorldInfo => _worldInfo ?? (_worldInfo = BuildWorldInfo());

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
            DrawEntryHeader("PLMWINF_WorldRecords".Translate(), backgroundColor: Color.yellow);

            if (_gameData.WorldData.WorldCharacteristics == null || _gameData.WorldData.WorldCharacteristics.Count == 0)
            {
                //Log.Error("[PrepareLanding] TabInfo.BuildWorldRecords: No Info");
                return;
            }

            // default line height
            const float gapLineHeight = 4f;

            // add a gap before the scroll view
            ListingStandard.Gap(gapLineHeight);

            /*
             * Calculate heights
             */

            // height of the scrollable outer Rect (visible portion of the scroll view, not the 'virtual' one)
            var maxScrollViewOuterHeight = InRect.height - ListingStandard.CurHeight - DefaultElementHeight;

            // height of the 'virtual' portion of the scroll view
            var numElements = _gameData.WorldData.WorldCharacteristics.Count * 3; // 1 label + 2 elements  (highest + lowest) = 3
            var scrollableViewHeight = (numElements * DefaultElementHeight) + (_gameData.WorldData.WorldCharacteristics.Count * gapLineHeight);

            /*
             * Scroll view
             */
            var innerLs = ListingStandard.BeginScrollView(maxScrollViewOuterHeight, scrollableViewHeight,
                ref _scrollPosWorldRecords, 16f);

            var selectedTileIndex = 0;
            foreach (var characteristicData in _gameData.WorldData.WorldCharacteristics)
            {
                var characteristicName = characteristicData.CharacteristicName;
                innerLs.Label(RichText.Bold(RichText.Color($"{characteristicName}:", Color.cyan)));

                // there might be no characteristics
                if (characteristicData.WorldTilesCharacteristics.Count == 0)
                {
                    innerLs.Label("No Info [DisableWorldData enabled]");
                    continue;
                }

                /*
                 *   lowest
                 */

                var lowestCharacteristicKvp = characteristicData.WorldTilesCharacteristics.First();

                // we need to follow user preference for temperature.
                var value = characteristicData.Characteristic == MostLeastCharacteristic.Temperature ? 
                    GenTemperature.CelsiusTo(lowestCharacteristicKvp.Value, Prefs.TemperatureMode) : lowestCharacteristicKvp.Value;

                var vectorLongLat = Find.WorldGrid.LongLatOf(lowestCharacteristicKvp.Key);
                var textLowest = $"{"PLMWINF_WorldLowest".Translate()} {characteristicName}: {value:F1} {characteristicData.CharacteristicMeasureUnit} [{lowestCharacteristicKvp.Key}; {vectorLongLat.y.ToStringLatitude()} - {vectorLongLat.x.ToStringLongitude()}]";

                var labelRect = innerLs.GetRect(DefaultElementHeight);
                var selected = selectedTileIndex == _worldRecordSelectedTileIndex;
                if (Core.Gui.Widgets.LabelSelectable(labelRect, textLowest, ref selected, TextAnchor.MiddleLeft))
                {
                    // go to the location of the selected tile
                    _worldRecordSelectedTileIndex = selectedTileIndex;
                    Find.WorldInterface.SelectedTile = lowestCharacteristicKvp.Key;
                    Find.WorldCameraDriver.JumpTo(Find.WorldGrid.GetTileCenter(Find.WorldInterface.SelectedTile));
                }

                selectedTileIndex++;

                /*
                 *   highest
                 */

                var highestCharacteristicKvp = characteristicData.WorldTilesCharacteristics.Last();
                // we need to follow user preference for temperature.
                value = characteristicData.Characteristic == MostLeastCharacteristic.Temperature ?
                    GenTemperature.CelsiusTo(highestCharacteristicKvp.Value, Prefs.TemperatureMode) : highestCharacteristicKvp.Value;

                vectorLongLat = Find.WorldGrid.LongLatOf(highestCharacteristicKvp.Key);
                var textHighest = $"{"PLMWINF_WorldHighest".Translate()} {characteristicName}: {value:F1} {characteristicData.CharacteristicMeasureUnit} [{highestCharacteristicKvp.Key}; {vectorLongLat.y.ToStringLatitude()} - {vectorLongLat.x.ToStringLongitude()}]";

                labelRect = innerLs.GetRect(DefaultElementHeight);
                selected = selectedTileIndex == _worldRecordSelectedTileIndex;
                if (Core.Gui.Widgets.LabelSelectable(labelRect, textHighest, ref selected, TextAnchor.MiddleLeft))
                {
                    // go to the location of the selected tile
                    _worldRecordSelectedTileIndex = selectedTileIndex;
                    Find.WorldInterface.SelectedTile = highestCharacteristicKvp.Key;
                    Find.WorldCameraDriver.JumpTo(Find.WorldGrid.GetTileCenter(Find.WorldInterface.SelectedTile));
                }

                selectedTileIndex++;

                // add a thin line between each label
                innerLs.GapLine(gapLineHeight);
            }

            ListingStandard.EndScrollView(innerLs);
        }

        /// <summary>
        ///     Called when a new world map has been generated.
        /// </summary>
        private void RebuildWorldInfo()
        {
            _worldInfo = BuildWorldInfo();
        }
    }
}