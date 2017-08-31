using System;
using System.Collections.Generic;
using System.Linq;
using PrepareLanding.Core.Extensions;
using UnityEngine;
using Verse;

namespace PrepareLanding.Core.Gui.Window
{
    public class ColumnData<T>
    {
        public string HeaderLabel { get; }

        public string ToolTip { get; }

        public Func<T, string> DataGetter { get; }

        public Color? ColumnTextColor { get; set; }

        public Color? ColumnBackgroundColor { get; set; }

        public ColumnData(string header, string tooltip, Func<T, string> getter)
        {
            HeaderLabel = header;
            ToolTip = tooltip;
            DataGetter = getter;
        }
    }

    public interface ITableView
    {
        void DrawTableContent(Rect inRect, Listing_Standard ls);
    }

    public class TableView<T> : ITableView
    {
        private const float RowHeight = 20f;

        private const float ColExtraWidth = 8f;

        private const float LineSpace = 3f;

        private Vector2 _scrollPosition = Vector2.zero;

        // _columnsText[columnIndex][rowIndex]
        private readonly List<List<string>> _columnsText;

        private readonly List<string> _tooltips;

        private readonly List<float> _columnWidthList;

        public TableView(IEnumerable<T> dataSources, IEnumerable<ColumnData<T>> getters)
        {
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
            {
                for (var columnIndex = 0; columnIndex < numColums; columnIndex++)
                {
                    var text = getterList[columnIndex].DataGetter(dataSourceList[rowIndex]);
                    var rows = _columnsText[columnIndex];
                    rows.Add(text);
                }
            }

            NumRows = _columnsText[0].Count;
            NumColumns = _columnsText.Count;
            TableHeight = (NumRows * RowHeight) + (NumRows * 2 * LineSpace);

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
                    {
                        maxTextWidth = currentTextWidth;
                    }
                }
                _columnWidthList.Add(maxTextWidth + ColExtraWidth);
            }

            // table width
            TableWidth = _columnWidthList.Sum() + (NumColumns * 2 * LineSpace);
        }

        public int NumRows { get;  }

        public int NumColumns { get; }

        public float TableHeight { get; }

        public float TableWidth { get; }

        public void DrawTableContent(Rect inRect, Listing_Standard ls)
        {
            Text.Font = GameFont.Tiny;
            inRect.yMax -= 60f;

            Listing_Standard innerLs = null;
            var viewRect = new Rect(0f, 0f, inRect.width - 16f, TableHeight);
            if (ls == null)
                Verse.Widgets.BeginScrollView(inRect, ref _scrollPosition, viewRect);
            else
                innerLs = ls.BeginScrollView(inRect.height - 60f, TableHeight, ref _scrollPosition, 16f);

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
                    mouseOverRect.xMin -= 999f;
                    mouseOverRect.xMax += 999f;
                    if (Mouse.IsOver(mouseOverRect) || columnIndex % 2 == 0)
                    {
                        Verse.Widgets.DrawHighlight(rect);
                    }
                    TooltipHandler.TipRegion(rect, _tooltips[columnIndex]);

                    var textAnchor = Text.Anchor;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    var contentColor = GUI.contentColor;
                    if(columnIndex%2 == 0)
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

            if(ls == null)
                Verse.Widgets.EndScrollView();
            else
                ls.EndScrollView(innerLs);
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

            if(columnIndex == 0)
            {
                // horizontal line (whole table)
                Verse.Widgets.DrawLineHorizontal(0, yValue, tableWidth);
            }

            yValue += LineSpace;

            return yValue;
        }
    }

    public class TableWindow : Verse.Window
    {
        protected readonly Listing_Standard ListingStandard;

        public float ColumnSizePerCent { get; }

        public override Vector2 InitialSize => new Vector2(1024f, 768f);

        public Rect InRect { get; protected set; }

        private readonly List<ITableView> _tables = new List<ITableView>();

        public TableWindow(float columnSizePct = 1.0f)
        {
            doCloseX = true;
            doCloseButton = true;

            ColumnSizePerCent = columnSizePct;

            ListingStandard = new Listing_Standard();
        }

        public void Begin(Rect inRect)
        {
            InRect = inRect;

            // set up column size
            ListingStandard.ColumnWidth = inRect.width * ColumnSizePerCent;

            // begin Rect position
            ListingStandard.Begin(InRect);
        }

        public void End()
        {
            ListingStandard.End();
        }

        public void AddTable(ITableView table)
        {
            _tables.Add(table);
        }

        public void ClearTables()
        {
            _tables.Clear();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Begin(inRect);

            var r1 = ListingStandard.GetRect(inRect.height);
            _tables[1].DrawTableContent(r1, null);

            ListingStandard.NewColumn();

            var r2 = ListingStandard.GetRect(inRect.height);
            _tables[0].DrawTableContent(r2, null);

            ListingStandard.NewColumn();

            var r3 = ListingStandard.GetRect(inRect.height);
            _tables[2].DrawTableContent(r3, null);

            End();
        }
    }
}
