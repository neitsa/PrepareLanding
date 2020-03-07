using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace PrepareLanding.Patches
{
    [HarmonyPatch(typeof(Page_CreateWorldParams), "DoWindowContents", typeof(Rect))]
    public static class PatchPageCreateWorldParams
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            Log.Message("[PL] Transpiler for Page_CreateWorldParams.DoWindowContents");

            var countInstr = 0;

            var codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldc_R4 && codes[i].operand.Equals(40f))
                {
                    countInstr++;
                    Log.Message($"[PL] Code Found at {i:x}; op: {codes[i].operand}; op as F: {(float)codes[i].operand}");

                    if (countInstr == 2)
                    {
                       
                    }

                }
            }
            return codes.AsEnumerable();
        }

        public static void AddOpcodesForLabel(List<CodeInstruction> codes, int instructionNumber)
        {
            var newCode = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldloca_S, 2), // Loads the address of the Rect3 local variable.

            };


            codes.InsertRange(instructionNumber, newCode);
        }

        public static void AddLabel(Rect r)
        {

        }

    }
}