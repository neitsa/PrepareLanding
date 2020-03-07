using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding.Patches
{
    /// <summary>
    /// Patch to be able to replace the natural stone types in a tile.
    /// </summary>
    [HarmonyPatch(typeof(World), "NaturalRockTypesIn")]
    public static class PatchNaturalRockTypesIn
    {
        /// <summary>
        /// Dictionary where key is tile ID and value is a lit of stone def (which are ThingDef).
        /// </summary>
        private static readonly Dictionary<int, List<ThingDef>> TileStonesReplacements =
            new Dictionary<int, List<ThingDef>>();

        [HarmonyPostfix]
        public static void NaturalRockTypesInPostFix(ref IEnumerable<ThingDef> __result, int tile)
        {
            // not allowed while playing (colony started); only during world map selection.
            if (Current.ProgramState == ProgramState.Playing)
                return;

            if (!TileStonesReplacements.ContainsKey(tile))
                return;

            __result = TileStonesReplacements[tile].AsEnumerable();
        }

        /// <summary>
        /// Add a tile to have its naturally occurring stone types replaced.
        /// </summary>
        /// <param name="tile">The tile id for which to change its naturally occurring stone types.</param>
        /// <param name="stoneDefs">The list of the new stone types for the given tile.</param>
        public static void AddTileForStoneReplacement(int tile, List<ThingDef> stoneDefs)
        {
            // just check we have a valid tile
            if (tile < 0)
            {
                Log.Error($"[PrepareLanding] AddTileForStoneReplacement: passed invalid tile ({tile}).");
                return;
            }

            // either 2 or 3 stone types are allowed.
            var stoneCount = stoneDefs.Count;
            if (stoneCount < 2 || stoneCount > 3)
            {
                Log.Error(
                    $"[PrepareLanding] AddTileForStoneReplacement: passed the wrong number of ThingDef ({stoneCount}).");
                return;
            }

            // just check that we have the right ThingDef (it must describe a stone)
            foreach (var stoneDef in stoneDefs)
            {
                if (stoneDef.category == ThingCategory.Building && stoneDef.building.isNaturalRock &&
                    !stoneDef.building.isResourceRock)
                    continue;

                Log.Message(
                    $"[PrepareLanding] Tried to pass a ThingDef that is not a stone... (name: {stoneDef.LabelCap}; cat: {stoneDef.category}; isNaturalRock: {stoneDef.building.isNaturalRock}; isResourceRock: {stoneDef.building.isResourceRock}).");
                return;
            }

            // setup new stone types.
            if (TileStonesReplacements.ContainsKey(tile))
                TileStonesReplacements[tile] = stoneDefs;
            else
                TileStonesReplacements.Add(tile, stoneDefs);
        }

        /// <summary>
        /// Clear the stone type replacements. Note that when stone types are replaced, the previous ones can be retrieved.
        /// </summary>
        public static void ClearStoneReplacements()
        {
            TileStonesReplacements.Clear();
        }
    }
}