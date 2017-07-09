using System;
using Harmony;

namespace PrepareLanding.Patches
{
    [HarmonyPatch(typeof(RimWorld.WorldInterface), "WorldInterfaceOnGUI")]
    public static class PatchWorldInterfaceOnGui
    {
        public static event Action OnWorldInterfaceOnGui = delegate { };

        [HarmonyPostfix]
        public static void WorldInterfaceOnGuiPostFix()
        {
            OnWorldInterfaceOnGui.Invoke();
        }
    }

    [HarmonyPatch(typeof(RimWorld.WorldInterface), "WorldInterfaceUpdate")]
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
