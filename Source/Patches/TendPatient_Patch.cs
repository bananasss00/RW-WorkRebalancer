using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkRebalancer.Patches
{
    public class TendPatient_Patch
    {
        public const int vanillaValue = 600; // vanilla multiplier value
        private static bool result = false;

        public static bool Apply(Harmony h)
        {
            // in dnSpy it's function MakeNewToils, but this real place!
            var makeNewToils = typeof(JobDriver_TendPatient)
                    .GetNestedTypes(AccessTools.all)?
                    .FirstOrDefault(t => t.Name.Equals("<MakeNewToils>d__17"))?
                    .GetMethods(AccessTools.all)
                    .FirstOrDefault(m => m.Name.Equals("MoveNext"));

            if (makeNewToils == null)
                return false;

            h.Patch(makeNewToils, transpiler: new HarmonyMethod(typeof(TendPatient_Patch), nameof(Transpiler)));
            return result;
        }

        public static float GetTendTicks()
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return vanillaValue;

            return vanillaValue * (WorkRebalancerMod.Instance.Prof.PercentOfBaseTendPatient / 100f);
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
        {
            var getTendTicks = typeof(TendPatient_Patch).GetMethod(nameof(GetTendTicks));
            foreach (var instruction in instr)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 600)
                {
                    yield return new CodeInstruction(OpCodes.Call, getTendTicks);
                    result = true;
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}

/*
protected override IEnumerable<Toil> MakeNewToils()
{
	....
	Toil toil = Toils_General.Wait((int)(1f / this.pawn.GetStatValue(StatDefOf.MedicalTendSpeed, true) * 600f), 
    ....
}
 */