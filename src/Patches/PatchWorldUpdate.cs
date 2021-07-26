using HarmonyLib;
using PrepareLanding.Core.Gui.Window;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace PrepareLanding.Patches
{
    [HarmonyPatch(typeof(World), "WorldUpdate")]
    class PatchWorldUpdate
    {
        [HarmonyPostfix]
        public static void WorldUpdatePostFix()
        {
            // Log.Message("[PrepareLanding] WorldUpdate postfix.");

            if (Input.GetKeyDown(PrepareLanding.Instance.GameOptions.PrepareLandingHotKey.Value))
            {
                Log.Message("[PrepareLanding] Shortcut key pressed.");

                // don't add a new window if the window is already there
                if (PrepareLanding.Instance.MainWindow == null)
                    PrepareLanding.Instance.MainWindow = new MainWindow(PrepareLanding.Instance.GameData);

                PrepareLanding.Instance.MainWindow.Show();
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // make sure the prepare landing window (or its minimized window) is closed when user presses escape.
                if (Find.WindowStack.IsOpen<MainWindow>() || Find.WindowStack.IsOpen<MinimizedWindow>())
                {
                    if (PrepareLanding.Instance.MainWindow != null)
                    {
                        Log.Message("[PrepareLanding] Escape: Force closing.");
                        PrepareLanding.Instance.MainWindow.ForceClose();

                        // force clearing the filtered tile list. We still keep the filters as is.
                        PrepareLanding.Instance.TileFilter.ClearMatchingTiles();
                    }
                }
            }
        }
    }
}
