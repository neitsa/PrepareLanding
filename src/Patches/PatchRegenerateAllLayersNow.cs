using Harmony;
using Verse;

namespace PrepareLanding.Patches
{
    [HarmonyPatch(typeof(RimWorld.Planet.WorldRenderer), "RegenerateAllLayersNow")]
    public static class PatchRegenerateAllLayersNow
    {
        [HarmonyPostfix]
        public static void RegenerateAllLayersNowPostFix()
        {
            Log.Message("[PrepareLanding] New world is generated or all layers are regenerated.");
            PrepareLanding.Instance.WorldGenerated();
        }
    }
}