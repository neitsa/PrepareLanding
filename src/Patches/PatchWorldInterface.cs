using System;
using Harmony;
using RimWorld;

namespace PrepareLanding.Patches
{
    [HarmonyPatch(typeof(WorldInterface), "WorldInterfaceOnGUI")]
    public static class PatchWorldInterfaceOnGui
    {
        public static event Action OnWorldInterfaceOnGui = delegate { };

        [HarmonyPostfix]
        public static void WorldInterfaceOnGuiPostFix()
        {
            OnWorldInterfaceOnGui.Invoke();
        }
    }

    [HarmonyPatch(typeof(WorldInterface), "WorldInterfaceUpdate")]
    public static class PatchWorldInterfaceUpdate
    {
        public static event Action OnWorldInterfaceUpdate = delegate { };

        [HarmonyPostfix]
        public static void WorldInterfaceUpdatePostFix()
        {
            OnWorldInterfaceUpdate.Invoke();
        }
    }
}