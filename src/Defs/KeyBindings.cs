using RimWorld;
using UnityEngine;
using Verse;

namespace PrepareLanding.Defs
{
    [DefOf]
    public static class KeyBindings
    {
        public static KeyBindingDef CoordinatesWindow;
    }

    public static class KeysUtils
    {
        public static bool IsControlPressedAndHeld => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || /* MAC OS */ Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
    }
}
