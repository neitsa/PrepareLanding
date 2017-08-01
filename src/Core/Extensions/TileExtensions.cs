using RimWorld.Planet;
using Verse;

namespace PrepareLanding.Extensions
{
    public static class TileExtensions
    {
        /// <summary>
        ///     Given a <see cref="Tile" /> instance returns its index in the world grid.
        /// </summary>
        /// <param name="tile">The tile for which to return the index.</param>
        /// <returns>
        ///     An integer representing the tile index in the world grid or -1 if the tile is not found (or if the world grid
        ///     is not initialized).
        /// </returns>
        public static int TileId(this Tile tile)
        {
            return Find.WorldGrid == null ? Tile.Invalid : Find.WorldGrid.tiles.IndexOf(tile);
        }
    }
}