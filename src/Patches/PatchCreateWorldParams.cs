using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace PrepareLanding.Patches
{
    class PatchCreateWorldParams
    {
        // note: this method is retrieved through Reflection. Check RimworldEventHandler.OnDefsLoaded().

        static bool Page_CreateWorldParams_CanDoNext(ref Page_CreateWorldParams __instance, ref bool __result)
        {
            // don't use Precise World Generation if the settings tells to do so.
            if (PrepareLanding.Instance.GameOptions.DisablePreciseWorldGenPercentage.Value) {
                Log.Message("[PrepareLanding] Precise World Generation - skipping due to mod settings.");
                return true;
            }

            /*
             * Call next page for precise world generation.
             */
            Log.Message("[PrepareLanding] Precise World Generation - If you are having trouble generating the world, disable PreciseWorldGeneration in PrepareLanding mod settings.");

            // grab all needed fields from the Page_CreateWorldParams instance.
            var planetCoverage = Traverse.Create(__instance).Field("planetCoverage").GetValue<float>();
            var seedString = Traverse.Create(__instance).Field("seedString").GetValue<string>();
            var rainfall = Traverse.Create(__instance).Field("rainfall").GetValue<OverallRainfall>();
            var temperature = Traverse.Create(__instance).Field("temperature").GetValue<OverallTemperature>();
            var population = Traverse.Create(__instance).Field("population").GetValue<OverallPopulation>();
            var factions = Traverse.Create(__instance).Field("factions").GetValue<List<FactionDef>>();
            var pollution = Traverse.Create(__instance).Field("pollution").GetValue<float>();


            // new page
            var p = new PagePreciseWorldGeneration(planetCoverage, seedString, rainfall, temperature, population, factions, pollution);

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
