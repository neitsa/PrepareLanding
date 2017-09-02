using System;
using System.Collections.Generic;
using System.Linq;
using PrepareLanding.Core.Gui.Tab;
using RimWorld;
using UnityEngine;
using Verse;

namespace PrepareLanding.Core.Gui.Window
{
    public class ColumnData<T>
    {
        public ColumnData(string header, string tooltip, Func<T, string> getter)
        {
            HeaderLabel = header;
            ToolTip = tooltip;
            DataGetter = getter;
        }

        public Color? ColumnBackgroundColor { get; set; }

        public Color? ColumnTextColor { get; set; }

        public Func<T, string> DataGetter { get; }
        public string HeaderLabel { get; }

        public string ToolTip { get; }
    }

    public interface ITableView
    {
        string Name { get; }

        int NumColumns { get; }

        int NumRows { get; }

        float TableHeight { get; }

        float TableWidth { get; }

        void DrawTableContent(Rect inRect);
    }

    public class TableView<T> : ITableView
    {
        private const float RowHeight = 20f;

        private const float ColExtraWidth = 8f;

        private const float LineSpace = 3f;

        // _columnsText[columnIndex][rowIndex]
        private readonly List<List<string>> _columnsText;

        private readonly List<float> _columnWidthList;

        private readonly List<string> _tooltips;

        private Vector2 _scrollPosition = Vector2.zero;

        public TableView(string name, IEnumerable<T> dataSources, IEnumerable<ColumnData<T>> getters)
        {
            Name = name;

            var getterList = getters.ToList();
            var dataSourceList = dataSources.ToList();

            var numColums = getterList.Count;
            var numRows = dataSourceList.Count;

            /*
             * ToolTips and headers
             */

            _tooltips = new List<string>(numColums);
            _columnsText = new List<List<string>>(numColums);
            for (var columnIndex = 0; columnIndex < numColums; columnIndex++)
            {
                // add 1 row for the header
                var rows = new List<string>(numRows + 1) {getterList[columnIndex].HeaderLabel};
                _columnsText.Add(rows);

                // column tool tips
                _tooltips.Add(getterList[columnIndex].ToolTip);
            }

            /*
             * Cells text
             */
            for (var rowIndex = 0; rowIndex < numRows; rowIndex++)
            for (var columnIndex = 0; columnIndex < numColums; columnIndex++)
            {
                var text = getterList[columnIndex].DataGetter(dataSourceList[rowIndex]);
                var rows = _columnsText[columnIndex];
                rows.Add(text);
            }

            NumRows = _columnsText[0].Count;
            NumColumns = _columnsText.Count;
            TableHeight = NumRows * RowHeight + NumRows * 2 * LineSpace;

            /*
             * Get width for each column
             */
            _columnWidthList = new List<float>();
            for (var columnIndex = 0; columnIndex < NumColumns; columnIndex++)
            {
                var maxTextWidth = 0f;
                for (var rowIndex = 0; rowIndex < numRows; rowIndex++)
                {
                    var text = _columnsText[columnIndex][rowIndex];
                    var currentTextWidth = Text.CalcSize(text).x;
                    if (currentTextWidth > maxTextWidth)
                        maxTextWidth = currentTextWidth;
                }
                _columnWidthList.Add(maxTextWidth + ColExtraWidth);
            }

            // table width
            TableWidth = _columnWidthList.Sum() + NumColumns * 2 * LineSpace;
        }

        public string Name { get; }

        public int NumRows { get; }

        public int NumColumns { get; }

        public float TableHeight { get; }

        public float TableWidth { get; }

        public void DrawTableContent(Rect inRect)
        {
            Text.Font = GameFont.Tiny;
            inRect.yMax -= 60f;

            var viewRect = new Rect(0f, 0f, inRect.width - 16f, TableHeight + 5f);
            Verse.Widgets.BeginScrollView(inRect, ref _scrollPosition, viewRect);

            /*
             * Draw content
             */
            var xValue = 0f;
            var yValue = 0f;
            for (var columnIndex = 0; columnIndex < NumColumns; columnIndex++)
            {
                yValue = 0; // reset to 0 as we start, for each column, at the top of the table
                xValue = DrawVerticalLine(columnIndex, xValue, TableHeight);

                for (var rowIndex = 0; rowIndex < NumRows; rowIndex++)
                {
                    yValue = DrawHorizontalLine(columnIndex, rowIndex, yValue, TableWidth);

                    var rect = new Rect(xValue, yValue, _columnWidthList[columnIndex], RowHeight);
                    var mouseOverRect = rect;
                    mouseOverRect.x = 0f;
                    mouseOverRect.width = TableWidth;
                    if (Mouse.IsOver(mouseOverRect) || columnIndex % 2 == 0)
                        Verse.Widgets.DrawHighlight(rect);
                    TooltipHandler.TipRegion(rect, _tooltips[columnIndex]);

                    var textAnchor = Text.Anchor;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    var contentColor = GUI.contentColor;
                    if (columnIndex % 2 == 0)
                        GUI.contentColor = Color.magenta;

                    Verse.Widgets.Label(rect, _columnsText[columnIndex][rowIndex]);

                    GUI.contentColor = contentColor;
                    Text.Anchor = textAnchor;

                    yValue += RowHeight;
                }
                xValue += _columnWidthList[columnIndex];
            }
            // last lines
            DrawVerticalLine(NumColumns, xValue, TableHeight);
            DrawHorizontalLine(0, NumRows, yValue, TableWidth);

            Verse.Widgets.EndScrollView();
        }

        private float DrawVerticalLine(int columnIndex, float xValue, float tableHeight)
        {
            if (columnIndex != 0)
                xValue += LineSpace;

            // vertical line
            Verse.Widgets.DrawLineVertical(xValue, 0, tableHeight);

            xValue += LineSpace;

            return xValue;
        }

        private float DrawHorizontalLine(int columnIndex, int rowIndex, float yValue, float tableWidth)
        {
            if (rowIndex != 0)
                yValue += LineSpace;

            if (columnIndex == 0)
                Verse.Widgets.DrawLineHorizontal(0, yValue, tableWidth);

            yValue += LineSpace;

            return yValue;
        }
    }

    public class TableWindow : Verse.Window
    {
        private readonly int _dateTicks;

        private readonly List<ITableView> _tables = new List<ITableView>();

        private readonly int _tileId;
        protected readonly Listing_Standard ListingStandard;

        public TableWindow(int tileId, int dateTicks, float columnSizePct = 1.0f)
        {
            _tileId = tileId;
            _dateTicks = dateTicks;

            doCloseX = true;
            doCloseButton = true;

            ColumnSizePerCent = columnSizePct;

            ListingStandard = new Listing_Standard();
        }

        public float ColumnSizePerCent { get; }

        public override Vector2 InitialSize => new Vector2(1024f, 768f);

        public void AddTable(ITableView table)
        {
            _tables.Add(table);
        }

        public void ClearTables()
        {
            _tables.Clear();
        }

        private void Begin(Rect inRect, bool useColumnSizePct = true)
        {
            //InRect = inRect;

            // set up column size
            ListingStandard.ColumnWidth = inRect.width * (useColumnSizePct ? ColumnSizePerCent : 1f);

            // begin Rect position
            ListingStandard.Begin(inRect);
        }

        private void End()
        {
            ListingStandard.End();
        }

        public override void DoWindowContents(Rect inRect)
        {
            var previousRect = inRect;
            for (var tableViewIndex = 0; tableViewIndex < _tables.Count; tableViewIndex++)
            {
                var rectTable = new Rect(inRect);
                if (tableViewIndex > 0)
                    rectTable.x = _tables[tableViewIndex - 1].TableWidth + previousRect.x + 25f;
                rectTable.width = _tables[tableViewIndex].TableWidth + 20f;

                Begin(rectTable, false);
                DrawEntryHeader(_tables[tableViewIndex].Name, false, true, Color.magenta);
                rectTable.yMin = ListingStandard.CurHeight;
                End();

                _tables[tableViewIndex].DrawTableContent(rectTable);

                // right under the small table
                if (tableViewIndex == 1)
                {
                    rectTable.y += _tables[tableViewIndex].TableHeight;
                    Begin(rectTable, false);
                    DrawTileInfo();
                    End();
                }

                previousRect = rectTable;
            }
        }

        private void DrawTileInfo()
        {
            DrawEntryHeader("Tile Specs", backgroundColor: Color.magenta);

            var vectorLongLat = Find.WorldGrid.LongLatOf(_tileId);
            var latitude = vectorLongLat.y;
            var longitude = vectorLongLat.x;

            ListingStandard.Label($"Date: {GenDate.DateReadoutStringAt(_dateTicks, vectorLongLat)}");
            ListingStandard.Label($"Tile ID: {_tileId}");
            ListingStandard.Label(
                $"Latitude - Longitude: {latitude.ToStringLatitude()} - {longitude.ToStringLongitude()}");
            ListingStandard.Label($"Equatorial distance: {Find.WorldGrid.DistanceFromEquatorNormalized(_tileId)}");
            ListingStandard.Label($"Tile average temperature: {Find.World.grid[_tileId].temperature} °C");
            ListingStandard.Label($"Seasonal shift amplitude: {GenTemperature.SeasonalShiftAmplitudeAt(_tileId)} °C");
        }

        protected virtual void DrawEntryHeader(string entryLabel, bool useStartingGap = true,
            bool useFollowingGap = false, Color? backgroundColor = null, float colorAlpha = 0.2f)
        {
            if (useStartingGap)
                ListingStandard.Gap(12f);

            var textHeight = Text.CalcHeight(entryLabel, ListingStandard.ColumnWidth);
            var r = ListingStandard.GetRect(0f);
            r.height = textHeight;

            var bgColor = backgroundColor.GetValueOrDefault(TabGuiUtility.MenuSectionBgFillColor);
            if (backgroundColor != null)
                bgColor.a = colorAlpha;

            Verse.Widgets.DrawBoxSolid(r, bgColor);

            ListingStandard.Label($"{entryLabel}", 30f);

            ListingStandard.GapLine(6f);

            if (useFollowingGap)
                ListingStandard.Gap();
        }
    }
}