using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace PrepareLanding.Patches
{
    [HarmonyPatch(typeof(PageUtility), "StitchedPages")]
    public static class PageUtilityPatch
    {
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void StitchedPagesPostFix(ref Page __result)
        {
            Log.Message("[PrepareLanding] [postfix] StitchedPagesPostFix");

            if (__result == null)
            {
                Log.Message("[PrepareLanding] [postfix] StitchedPagesPostFix: __result is null.");
                return;
            }

            if (TutorSystem.TutorialMode)
            {
                Log.Message("[PrepareLanding] [postfix] StitchedPagesPostFix: Tutorial mode is ON.");
                return;
            }

            // patch for Page_SelectStartingSite to add our own button at the bottom of the screen.
            var dictPageTypes = new Dictionary<Type, Page> { {typeof(Page_SelectStartingSite), new SelectLandingSite()} };
            var dictFoundPagesTypes = new Dictionary<Type, bool>
            {
                {typeof(Page_SelectStartingSite), false},

            };

            // if precise world generation is not disabled, then we'll patch Page_CreateWorldParams with our own class.
            if (!PrepareLanding.Instance.GameOptions.DisablePreciseWorldGenPercentage)
            {
                dictPageTypes.Add(typeof(Page_CreateWorldParams), new CreateWorldParams());
                dictFoundPagesTypes.Add(typeof(Page_CreateWorldParams), false);
            }

            var page = __result;
            while (true)
            {
                var currentPage = page.next;
                if (currentPage == null)
                    break;

                PatchPage(currentPage, dictPageTypes, dictFoundPagesTypes);

                page = currentPage;
            }

            if (!dictFoundPagesTypes.Values.All(v => v))
            {
                Log.Error("[PrepareLanding] [postfix] StitchedPagesPostFix /!\\ Error !!!! Required page not found /!\\");
                foreach (var foundPagesType in dictFoundPagesTypes)
                {
                    if(!foundPagesType.Value)
                        Log.Error($"[PrepareLanding] [postfix] StitchedPagesPostFix; Missing page type: {foundPagesType.Key}");

                }
            }

            Log.Message("[PrepareLanding] [postfix] StitchedPagesPostFix done!");
        }

        private static void PatchPage(Page currentPage, Dictionary<Type, Page> dictTypePages, IDictionary<Type, bool> dictFoundTypePages)
        {
            Type matchingType = null;
            foreach (var key in dictTypePages.Keys)
            {
                if (currentPage.GetType() != key) continue;
                matchingType = key;
                break;
            }

            if (matchingType == null)
                return;

            var replacementPage = dictTypePages[matchingType];

            Log.Message($"[PrepareLanding] [postfix] RimWorld.PageUtility.StitchedPages: Found {matchingType}.");

            // get next & previous of the original page
            var next = currentPage.next;
            var previous = currentPage.prev;

            // unlinking!
            previous.next = replacementPage;
            next.prev = replacementPage;
            replacementPage.prev = previous;
            replacementPage.next = next;

            if (dictFoundTypePages.ContainsKey(matchingType))
            {
                dictFoundTypePages[matchingType] = true;
            }
        }
    }
}