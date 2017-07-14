using UnityEngine;
using Verse;

namespace PrepareLanding.Gui.Tab
{
    public interface ITabGuiUtility
    {
        /// <summary>
        ///     A unique identifier for the Tab.
        /// </summary>
        string Id { get; }

        /// <summary>
        ///     The name of the tab (that is actually displayed at its top).
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     The <see cref="TabRecord" /> that describes the tab.
        /// </summary>
        TabRecord TabRecord { get; set; }

        /// <summary>
        ///     Draw the content of the tab.
        /// </summary>
        /// <param name="inRect">The <see cref="Rect" /> in which to draw the tab content.</param>
        void Draw(Rect inRect);
    }

    public interface ITabGuiUtilityColumned : ITabGuiUtility
    {
        /// <summary>
        ///     The <see cref="Listing_Standard" /> used to build the column inside a tab.
        /// </summary>
        Listing_Standard ListingStandard { get; }

        /// <summary>
        ///     The <see cref="Rect" /> used by the tab.
        /// </summary>
        Rect InRect { get; }
    }
}