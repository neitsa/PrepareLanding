using Harmony;
using RimWorld;
using Verse;

namespace PrepareLanding.Patches
{
    [HarmonyPatch(typeof(PageUtility), "StitchedPages")]
    public static class PageUtilityPatch
    {

        [HarmonyPostfix]
        public static void StitchedPagesPostFix(ref Page __result)
        {
            Log.Message("[PrepareLanding] [postfix] RimWorld.PageUtility.StitchedPages");

            if (__result == null)
            {
                Log.Message("[PrepareLanding] [postfix] __result is null.");
                return;
            }

            if (TutorSystem.TutorialMode)
            {
                Log.Message("[PrepareLanding] [postfix] Tutorial mode is ON.");
                return;
            }

            var foundRequiredPage = false;
            var page = __result;
            while (true)
            {
                var currentPage = page.next;
                if (currentPage == null)
                    break;

                if (currentPage.GetType() == typeof(Page_SelectStartingSite))
                {
                    Log.Message("[PrepareLanding] [postfix] Found Page_SelectLandingSite.");
                    foundRequiredPage = true;

                    // get next & previous of the original page
                    var next = currentPage.next;
                    var previous = currentPage.prev;

                    var newPage = new SelectLandingSite();

                    // unlinking!
                    previous.next = newPage;
                    next.prev = newPage;
                    newPage.prev = previous;
                    newPage.next = next;

                    break;
                }
                page = currentPage;
            }

            if (!foundRequiredPage)
            {
                Log.Error("[PrepareLanding] /!\\ Error !!!! Required page not found /!\\");
                // TODO: disable the mod altogether?
            }

            Log.Message("[PrepareLanding] [postfix] Done!");
        }
    }
}