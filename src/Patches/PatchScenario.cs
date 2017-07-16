using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace PrepareLanding.Patches
{
    [HarmonyPatch(typeof(Scenario), "GetFirstConfigPage")]
    public static class PatchScenario
    {
        private static bool _prefixHasExecuted;

        private static Page _resultPage;

        [HarmonyPrefix]
        public static bool GetFirstConfigPagePrefix(Scenario __instance)
        {
            Log.Message("[PrepareLanding] [prefix] Patching RimWorld.Page_SelectLandingSite.GetFirstConfigPage");

            // do not execute the patch if the main instance is null or the mod is not active.
            if (PrepareLanding.Instance == null || !PrepareLanding.Instance.ModIsActive )
            {
                var reason = string.Empty;
                if (PrepareLanding.Instance == null)
                    reason = "No main instance";
                else
                {
                    if (!PrepareLanding.Instance.ModIsActive)
                        reason = "Mod is not active";
                }

                Log.Message($"[PrepareLanding] [prefix] Prefix will not be executed. Reason: {reason}");

                _prefixHasExecuted = false;

                return true;
            }

            // boolean used in postfix to see if prefix was executed or not
            // This allows other mods to disable this mod altogether by having a higher priority.
            _prefixHasExecuted = true;

            // call our own method and save the result. It will be used in the postfix.
            _resultPage = GetFirstConfigPage(__instance);

            // do *not* allow the original method to be called, 
            //    otherwise RimWorld will call the RimWorld.Page_SelectLandingSite class (instead of our own) and we wont be able to add our button...
            return false;
        }

        [HarmonyPostfix]
        public static void GetFirstConfigPagePostFix(ref Page __result)
        {
            Log.Message("[PrepareLanding] [postfix] Patching RimWorld.Page_SelectLandingSite.GetFirstConfigPage");

            if (!_prefixHasExecuted)
            {
                Log.Message(
                    "[PrepareLanding] [postfix] Skipping RimWorld.Page_SelectLandingSite.GetFirstConfigPage PostFix as Prefix was not executed.");
                return;
            }

            // pass the result to harmony (and then to RimWorld)
            __result = _resultPage;
        }


        /// <summary>
        ///     A direct rip of <see cref="RimWorld.Scenario.GetFirstConfigPage" />. Harmony detours this function so we can use
        ///     our <see cref="SelectLandingSite" /> class.
        /// </summary>
        /// <param name="instance">The <see cref="RimWorld.Scenario" /> instance (provided by Harmony).</param>
        /// <returns></returns>
        public static Page GetFirstConfigPage(Scenario instance)
        {
            Log.Message("[PrepareLanding] Executing RimWorld.Scenario.GetFirstConfigPage replacement");

            // access private field "parts" of scenario instance.
            var fi = AccessTools.Field(typeof(Scenario), "parts");
            var parts = (List<ScenPart>) fi.GetValue(instance);
            if (parts == null) // note: this shouldn't happen
            {
                Log.Message("[PrepareLanding] List<ScenPart> parts is null in scenario");
                return null;
            }

            if (TutorSystem.TutorialMode)
                Log.Message("[PrepareLanding] Tutorial mode detected: instantiating RimWorld code rather than our own.");

            var list = new List<Page>
            {
                new Page_SelectStoryteller(),
                new Page_CreateWorldParams(),
                // note: we use our class here, replacing the RimWorld Page_SelectLandingSite class
                //  if not in tutorial mode, otherwise we instantiate the normal RimWorld code.
                !TutorSystem.TutorialMode ? new SelectLandingSite() : new Page_SelectLandingSite()
            };

            foreach (var current in parts.SelectMany(p => p.GetConfigPages()))
                list.Add(current);

            var page = PageUtility.StitchedPages(list);
            if (page == null)
                return null;

            var page2 = page;
            while (page2.next != null)
                page2 = page2.next;

            page2.nextAct = PageUtility.InitGameStart;
            return page;
        }
    }
}