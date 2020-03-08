using System;
using HarmonyLib;
using RimWorld;

namespace PrepareLanding.Patches
{
    [HarmonyPatch(typeof(WorldInterface), "WorldInterfaceOnGUI")]
    public static class PatchWorldInterfaceOnGui
    {
        public static event Action WorldInterfaceOnGui = delegate { };

        [HarmonyPostfix]
        public static void WorldInterfaceOnGuiPostFix()
        {
            WorldInterfaceOnGui.Invoke();
        }
    }

    [HarmonyPatch(typeof(WorldInterface), "WorldInterfaceUpdate")]
    public static class PatchWorldInterfaceUpdate
    {
        public static event Action WorldInterfaceUpdate = delegate { };

        [HarmonyPostfix]
        public static void WorldInterfaceUpdatePostFix()
        {
            WorldInterfaceUpdate.Invoke();
        }
    }
}