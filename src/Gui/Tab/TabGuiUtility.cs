﻿using System;
using UnityEngine;
using Verse;

namespace PrepareLanding.Gui.Tab
{
    public abstract class TabGuiUtility : ITabGuiUtilityColumned
    {
        public const float DefaultElementHeight = 30f;
        public const float DefaultGapLineHeight = 6f;
        public const float DefaultGapHeight = 12f;

        private static ColorInt _windowBgFillColorInt = new ColorInt(21, 25, 29);
        public static Color WindowBgFillColor = _windowBgFillColorInt.ToColor;

        private static ColorInt _menuSectionBgFillColor = new ColorInt(42, 43, 44);
        public static Color MenuSectionBgFillColor = _menuSectionBgFillColor.ToColor;

        protected float ColumnSizePercent;

        public abstract string Id { get; }

        public abstract string Name { get;  }

        public TabRecord TabRecord { get; set; }

        public Listing_Standard ListingStandard { get; protected set; }
        public Rect InRect { get; protected set; }

        public abstract void Draw(Rect inRect);

        protected TabGuiUtility(float columnSizePercent)
        {
            if (!(Math.Abs(columnSizePercent) < float.Epsilon) && !(columnSizePercent >= 1.0f))
                ColumnSizePercent = columnSizePercent;

            ListingStandard = new Listing_Standard();
        }

        public void Begin(Rect inRect)
        {
            InRect = inRect;

            // set up column size
            ListingStandard.ColumnWidth = inRect.width * ColumnSizePercent;

            // begin Rect position
            ListingStandard.Begin(InRect);
        }

        public void End()
        {
            ListingStandard.End();
        }

        public void NewColumn()
        {
            ListingStandard.NewColumn();
        }

        protected virtual void DrawEntryHeader(string entryLabel, bool useStartingGap = true, bool useFollowingGap = false, Color? backgroundColor = null)
        {
            if (useStartingGap)
                ListingStandard.Gap(DefaultGapHeight);

            var textHeight = Text.CalcHeight(entryLabel, ListingStandard.ColumnWidth);
            var r = ListingStandard.GetRect(0f);
            r.height = textHeight;

            var bgColor = backgroundColor.GetValueOrDefault(MenuSectionBgFillColor);
            if(backgroundColor != null)
                bgColor.a = 0.20f;

            Verse.Widgets.DrawBoxSolid(r, bgColor);

            ListingStandard.Label($"{entryLabel}:", DefaultElementHeight);

            ListingStandard.GapLine(DefaultGapLineHeight);

            if (useFollowingGap)
                ListingStandard.Gap(DefaultGapHeight);
        }

        protected virtual void DrawUsableMinMaxNumericField<T>(UsableMinMaxNumericItem<T> numericItem, string label, float min = 0f, float max = 1E+09f)
            where T : struct
        {
            var tmpCheckedOn = numericItem.Use;

            ListingStandard.Gap(DefaultGapHeight);
            ListingStandard.CheckboxLabeled(label, ref tmpCheckedOn, $"Use Min/Max {label}");
            numericItem.Use = tmpCheckedOn;

            var minValue = numericItem.Min;
            var minValueString = numericItem.MinString;
            var minValueLabelRect = ListingStandard.GetRect(DefaultElementHeight);
            Widgets.TextFieldNumericLabeled(minValueLabelRect, "Min: ", ref minValue, ref minValueString, min, max);
            numericItem.Min = minValue;
            numericItem.MinString = minValueString;

            var maxValue = numericItem.Max;
            var maxValueString = numericItem.MaxString;
            var maxValueLabelRect = ListingStandard.GetRect(DefaultElementHeight);
            Widgets.TextFieldNumericLabeled(maxValueLabelRect, "Max: ", ref maxValue, ref maxValueString, min, max);
            numericItem.Max = maxValue;
            numericItem.MaxString = maxValueString;
        }

    }
}