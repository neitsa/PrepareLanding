using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding.Patches
{
    [HarmonyPatch(typeof(Page_CreateWorldParams), "CanDoNext")]
    class PatchCreateWorldParams
    {
        [HarmonyPrefix]
        static bool Page_CreateWorldParams_CanDoNext(ref Page_CreateWorldParams __instance, ref bool __result)
        {
            // don't use Precise World Generation if the settings tells to do so.
            if (PrepareLanding.Instance.GameOptions.DisablePreciseWorldGenPercentage.Value)
                return true;

            /*
             * Call next page for precise world generation.
             */
            Log.Message("[PrepareLanding] Precise World Generation - If you have trouble generating the world, disable PreciseWorldGeneration in PrepareLanding mod settings.");

            // grab all needed fields from the Page_CreateWorldParams instance.
            var seedString = Traverse.Create(__instance).Field("seedString").GetValue<string>();
            var planetCoverage = Traverse.Create(__instance).Field("planetCoverage").GetValue<float>();
            var rainfall = Traverse.Create(__instance).Field("rainfall").GetValue<OverallRainfall>();
            var temperature = Traverse.Create(__instance).Field("temperature").GetValue<OverallTemperature>();
            var population = Traverse.Create(__instance).Field("population").GetValue<OverallPopulation>();
            
            // new page
            var p = new PagePreciseWorldGeneration(planetCoverage, seedString, rainfall, temperature, population);

            // set up correct prev and next.
            var originalNextPage = Traverse.Create(__instance).Field("next").GetValue<Page>();
            p.prev = __instance;
            p.next = originalNextPage;
            p.next.prev = p;
            Traverse.Create(__instance).Field("next").SetValue(p);

            // return value for CanDoNext()
            __result = true;

            // prevent call to original function.
            return false;
        }
    }
}
