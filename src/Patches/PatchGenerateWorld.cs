using System;
using HarmonyLib;
using RimWorld.Planet;

namespace PrepareLanding.Patches
{
    [HarmonyPatch(typeof(WorldGenerator), "GenerateWorld")]
    public static class PatchGenerateWorld
    {
        public static event Action WorldAboutToBeGenerated = delegate { };

        [HarmonyPrefix]
        public static bool GenerateWorldPrefix()
        {
            WorldAboutToBeGenerated?.Invoke();

            return true;
        }

        public static event Action WorldGenerated = delegate { };

        [HarmonyPostfix]
        public static void GenerateWorldPostFix()
        {
            WorldGenerated?.Invoke();
        }
    }
}