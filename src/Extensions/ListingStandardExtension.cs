using UnityEngine;
using Verse;

namespace PrepareLanding.Extensions
{
    /// <summary>
    ///     Extension class for <see cref="Verse.Listing_Standard" />.
    /// </summary>
    public static class ListingStandardExtension
    {
        private static readonly GUIContent TmpGuiContent = new GUIContent();
        /// <summary>
        ///     Start a scroll view inside a <see cref="Listing_Standard" />.
        /// </summary>
        /// <param name="ls"><see cref="Listing_Standard" /> instance.</param>
        /// <param name="outerRectHeight">The containing <see cref="Rect" /> for the scroll view.</param>
        /// <param name="scrollViewHeight">The height of the (virtual) scroll view.</param>
        /// <param name="scrollViewPos">The scroll position.</param>
        /// <param name="widthShrinkage">Value to be removed from the scroll view width.</param>
        /// <remarks>This call must be matched with a call to <see cref="EndScrollView" />.</remarks>
        public static Listing_Standard BeginScrollView(this Listing_Standard ls, float outerRectHeight,
            float scrollViewHeight, ref Vector2 scrollViewPos, float widthShrinkage = 0f)
        {
            var outerRect = ls.GetRect(outerRectHeight);
            var scrollViewRect = new Rect(0, 0, ls.ColumnWidth - widthShrinkage, scrollViewHeight);

            Widgets.BeginScrollView(outerRect, ref scrollViewPos, scrollViewRect);

            var internalLs = new Listing_Standard {ColumnWidth = ls.ColumnWidth - widthShrinkage};
            internalLs.Begin(scrollViewRect);

            return internalLs;
        }

        public static Listing_Standard BeginScrollViewForText(this Listing_Standard ls, float outerRectHeight,
            string text, ref Vector2 scrollViewPos, GUIStyle textStyle, float widthShrinkage = 0f)
        {
            // calculate text height for the scroll view height.
            TmpGuiContent.text = text;
            var textHeight = textStyle.CalcHeight(TmpGuiContent, ls.ColumnWidth) + 10f;
            var scrollViewHeight = Mathf.Max(textHeight, outerRectHeight);

            return BeginScrollView(ls, outerRectHeight, scrollViewHeight, ref scrollViewPos, widthShrinkage);
        }

        /// <summary>
        ///     End a scroll view in a <see cref="Listing_Standard" /> started with <see cref="BeginScrollView" />.
        /// </summary>
        /// <param name="ls"><see cref="Listing_Standard" /> instance.</param>
        /// <param name="internalLs"></param>
        /// <remarks>This call must be matched with a call to <see cref="BeginScrollView" />.</remarks>
        public static void EndScrollView(this Listing_Standard ls, Listing_Standard internalLs)
        {
            internalLs.End();

            Widgets.EndScrollView();
        }

        public static float StartCaptureHeight(this Listing_Standard ls)
        {
            return ls.CurHeight;
        }

        public static Rect EndCaptureHeight(this Listing_Standard ls, float startHeight)
        {
            var r = ls.GetRect(0f);
            r.y = startHeight;
            r.height = ls.CurHeight - startHeight;
            return r;
        }

        public static Rect VirtualRect(this Listing_Standard ls, float height)
        {
            var r = ls.GetRect(0f);
            r.height = height;
            return r;
        }
    }
}