using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace PrepareLanding.Core.Extensions
{
    public static class RectExtensions
    {
        public static List<Rect> SplitRectWidth(this Rect r, IList<float> divisors)
        {
            var result = new List<Rect>();
            if (divisors.Count == 0)
            {
                result.Add(r);
                return result;
            }

            var sum = divisors.Take(divisors.Count).Sum();
            if (sum > 1.0f)
            {
                Log.Message("[PrepareLanding] SplitRectWidth has a sum > 1");
                result.Clear();
                return result;
            }
            if (sum < 1.0f)
                divisors.Add(1.0f - sum);

            var originalWidth = r.width;
            var previousWidth = 0f;
            var currentX = r.x;
            foreach (var divisor in divisors)
            {
                var currentWidth = originalWidth * divisor;
                currentX += previousWidth;
                var currentRect = new Rect(currentX, r.y, currentWidth, r.height);
                result.Add(currentRect);
                previousWidth = currentWidth;
            }

            return result;
        }

        public static List<Rect> SplitRectWidthEvenly(this Rect r, int splitsNumber)
        {
            if (splitsNumber <= 0)
                return new List<Rect>();

            var splitPct = 1f / splitsNumber;

            var splits = new List<float>();
            for (var i = 0; i < splitsNumber; i++)
                splits.Add(splitPct);

            return SplitRectWidth(r, splits);
        }

        /// <summary>
        ///     Given a containing <see cref="Rect" /> return of list of Rect so all of the Rects in the list are spaced evenly
        ///     from the center of the containing Rect.
        /// </summary>
        /// <param name="r">The containing <see cref="Rect" />.</param>
        /// <param name="y">The height (in <see cref="r" />) at which the items are to be placed.</param>
        /// <param name="numItems">Number of items to draw.</param>
        /// <param name="itemWidth">The width of a single item.</param>
        /// <param name="itemHeight">The height of a single item.</param>
        /// <param name="spaceBetweenItems">The width of the space between each of the items.</param>
        /// <param name="isMinimized">Indicated whether or not the current containing window is resized to a lesser Rect.</param>
        /// <returns>A list of <see cref="Rect" />.</returns>
        public static List<Rect> SpaceEvenlyFromCenter(this Rect r, float y, int numItems, float itemWidth,
            float itemHeight, float spaceBetweenItems, bool isMinimized = false)
        {
            var result = new List<Rect>();
            if (numItems <= 0)
                return result;

            // total width required for all items and the space between them
            var totalWidth = numItems * itemWidth + (numItems - 1) * spaceBetweenItems;

            // total width required by all items can't be greater than the given Rect: output error message, return empty list
            if (totalWidth > r.width && !isMinimized)
            {
                Log.Error("[PrepareLanding] SpaceEvenlyFromCenter: totalWidth is greater than Rect.width");
                return result;
            }

            // total number of spaces 
            var numSpaces = numItems - 1;

            // the x value for the "contained" Rect (virtual Rect containing all item and spaces). Incidentally, the x value for the first item.
            var xValue = (r.width - totalWidth) / 2f;

            for (uint i = 0; i < numItems; ++i)
            {
                var tmpRect = new Rect(xValue, y, itemWidth, itemHeight);
                result.Add(tmpRect);

                // next item x value
                xValue += itemWidth;

                // add space if not the last item
                if (i != numSpaces)
                    xValue += spaceBetweenItems;
            }

            return result;
        }

        public static Rect ContractedByButLeft(this Rect rect, float margin)
        {
            return new Rect(rect.x, rect.y + margin, rect.width - margin, rect.height - margin * 2f);
        }

        public static List<Rect> Split(this Rect rect, int numberOfSplit, float spaceWidth = 0f)
        {
            if (numberOfSplit <= 0)
            {
                Log.Error($"[GoToTile] Rect.Split(): number of split is wrong ({numberOfSplit}).");
                return null;
            }

            var result = new List<Rect>(numberOfSplit);

            if (numberOfSplit == 1)
            {
                result.Add(rect);
                return result;
            }

            var divisor = 1f / numberOfSplit;
            if (rect.width * divisor - spaceWidth < 1f)
            {
                Log.Error($"[GoToTile] Rect.Split(): invalid space width ({spaceWidth}).");
                return null;
            }

            var xVal = rect.x;
            for (var i = 0; i < numberOfSplit; i++)
            {
                var newWidth = rect.width * divisor;
                newWidth -= spaceWidth;
                var newRect = new Rect(xVal, rect.y, newWidth, rect.height);
                result.Add(newRect);

                xVal += newWidth;
                if (i != numberOfSplit - 1)
                    xVal += spaceWidth;
            }

            return result;
        }

        public static List<Rect> SplitBy(this Rect rect, float[] splitsPct, float spaceWidth = 0f)
        {
            var numberOfSplit = splitsPct.Length;
            if (numberOfSplit <= 0)
            {
                Log.Error($"[GoToTile] Rect.Split(): number of split is wrong ({numberOfSplit}).");
                return null;
            }

            var result = new List<Rect>(numberOfSplit);

            if (numberOfSplit == 1)
            {
                result.Add(rect);
                return result;
            }

            // check that the amount of pct is (roughly) equal to 1
            var totalPct = 0f;
            for (var i = 0; i < numberOfSplit; i++)
                totalPct += splitsPct[i];
            if (totalPct - 1 > 1.01f || totalPct - 1f > 0.01f)
            {
                Log.Error($"[GoToTile] Rect.Split(): totalPct is wrong ({totalPct}).");
                return null;
            }

            var xVal = rect.x;
            for (var i = 0; i < numberOfSplit; i++)
            {
                var newWidth = rect.width * splitsPct[i];
                newWidth -= spaceWidth;
                if (newWidth <= 0f)
                {
                    Log.Error($"[GoToTile] Rect.Split(): would result in non existent width :({newWidth}).");
                    return null;
                }

                var newRect = new Rect(xVal, rect.y, newWidth, rect.height);
                result.Add(newRect);

                xVal += newWidth;
                if (i != numberOfSplit - 1)
                    xVal += spaceWidth;
            }

            return result;
        }
    }
}