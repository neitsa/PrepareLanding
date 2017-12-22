using System;
using PrepareLanding.Core.Extensions;
using PrepareLanding.Filters;
using UnityEngine;
using Verse;

namespace PrepareLanding.Core.Gui.Tab
{
    public abstract class TabGuiUtility : ITabGuiUtilityColumned
    {
        public const float DefaultElementHeight = 30f;
        public const float DefaultGapLineHeight = 6f;
        public const float DefaultGapHeight = 12f;
        public const float DefaultScrollableViewShrinkWidth = 16f;

        public static Color DefaultMenuSectionBgFillColor = Color.magenta;

        private readonly float _columnSizePercent;

        protected TabGuiUtility(float columnSizePercent)
        {
            if (!(Math.Abs(columnSizePercent) < float.Epsilon) && !(columnSizePercent >= 1.0f))
                _columnSizePercent = columnSizePercent;

            ListingStandard = new Listing_Standard();
        }

        public abstract string Id { get; }

        public abstract string Name { get; }

        public abstract bool CanBeDrawn { get; set; }

        public TabRecord TabRecord { get; set; }

        public Listing_Standard ListingStandard { get; protected set; }

        public Rect InRect { get; protected set; }

        public abstract void Draw(Rect inRect);

        protected void Begin(Rect inRect)
        {
            InRect = inRect;

            // set up column size
            ListingStandard.ColumnWidth = inRect.width * _columnSizePercent;

            // begin Rect position
            ListingStandard.Begin(InRect);
        }

        protected void End()
        {
            ListingStandard.End();
        }

        protected void NewColumn(bool drawVerticalSeparator=false)
        {
            ListingStandard.NewColumn();
            if (!drawVerticalSeparator)
                return;

            // draw vertical separator
            var rect = ListingStandard.VirtualRect(InRect.height - DefaultElementHeight - 5f);
            rect.x -= Listing.ColumnSpacing / 2f;
            rect.width = 1f;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
        }

        protected float DrawEntryHeader(string entryLabel, bool useStartingGap = true,
            bool useFollowingGap = false, Color? backgroundColor = null, float colorAlpha = 0.2f)
        {
            if (useStartingGap)
                ListingStandard.Gap();

            var textHeight = Text.CalcHeight(entryLabel, ListingStandard.ColumnWidth);
            var r = ListingStandard.GetRect(0f);
            r.height = textHeight;

            var bgColor = backgroundColor.GetValueOrDefault(DefaultMenuSectionBgFillColor);
            if (backgroundColor != null)
                bgColor.a = colorAlpha;

            Verse.Widgets.DrawBoxSolid(r, bgColor);

            ListingStandard.Label($"{entryLabel}:", DefaultElementHeight);

            ListingStandard.GapLine(DefaultGapLineHeight);

            if (useFollowingGap)
                ListingStandard.Gap();

            return ListingStandard.CurHeight;
        }

        protected void DrawUsableMinMaxNumericField<T>(UsableMinMaxNumericItem<T> numericItem, string label,
            float min = 0f, float max = 1E+09f) where T: struct , IComparable, IConvertible
        {
            var tmpCheckedOn = numericItem.Use;

            ListingStandard.Gap();
            ListingStandard.CheckboxLabeled(label, ref tmpCheckedOn, $"Use Min/Max {label}");
            numericItem.Use = tmpCheckedOn;

            var minValue = numericItem.Min;
            var minValueString = numericItem.MinString;
            var minValueLabelRect = ListingStandard.GetRect(DefaultElementHeight);
            Verse.Widgets.TextFieldNumericLabeled(minValueLabelRect, "Min: ", ref minValue, ref minValueString, min, max);
            numericItem.Min = minValue;
            numericItem.MinString = minValueString;

            var maxValue = numericItem.Max;
            var maxValueString = numericItem.MaxString;
            var maxValueLabelRect = ListingStandard.GetRect(DefaultElementHeight);
            Verse.Widgets.TextFieldNumericLabeled(maxValueLabelRect, "Max: ", ref maxValue, ref maxValueString, min, max);
            numericItem.Max = maxValue;
            numericItem.MaxString = maxValueString;
        }

        protected static Color ColorFromFilterSubjectThingDef(string filterName)
        {
            if (!PrepareLanding.Instance.GameData.UserData.Options.ShowFilterHeaviness)
                return DefaultMenuSectionBgFillColor;

            Color result;
            var heaviness = PrepareLanding.Instance.TileFilter.FilterHeavinessFromFilterSubjectThingDef(filterName);
            switch (heaviness)
            {
                case FilterHeaviness.Unknown:
                    result = DefaultMenuSectionBgFillColor;
                    break;
                case FilterHeaviness.Light:
                    result = Color.green;
                    break;
                case FilterHeaviness.Medium:
                    result = Color.yellow;
                    break;
                case FilterHeaviness.Heavy:
                    result = Color.red;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;
        }
    }
}