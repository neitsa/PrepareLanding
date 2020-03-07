using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PrepareLanding.Patches
{
    [HarmonyPatch(typeof(WorldGenStep_Terrain), "GenerateGridIntoWorld")]
    public static class PatchGenerateGridIntoWorld
    {
        public static readonly List<KeyValuePair<int, Vector3>> TileIdsAndVectors =
            new List<KeyValuePair<int, Vector3>>();

        [HarmonyPostfix]
        public static void GenerateGridIntoWorldPostFix()
        {
            var tilesCount = Find.WorldGrid.TilesCount;

            TileIdsAndVectors.Clear();
            TileIdsAndVectors.Capacity = tilesCount;

            for (var i = 0; i < tilesCount; i++)
            {
                var tileCenter = Find.WorldGrid.GetTileCenter(i);
                var kvp = new KeyValuePair<int, Vector3>(i, tileCenter);
                TileIdsAndVectors.Add(kvp);
            }

            // sort by y axis (latitude)
            TileIdsAndVectors.Sort((x, y) => x.Value.y.CompareTo(y.Value.y));
        }
    }
}
