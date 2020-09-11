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
    public class Breakdowns_Maintenance_Patch
    {
        public const int fullRepairTicks = 2500; // fluffy field const

        public static bool Apply(Harmony h)
        {
            // get maintenance.tickAction lambda method
            MethodInfo method = AccessTools.TypeByName("Fluffy_Breakdowns.JobDriver_Maintenance")?
                .GetNestedTypes(BindingFlags.Instance | BindingFlags.NonPublic)?
                .First((Type t) => t.FullName.Contains("c__DisplayClass3_0"))?
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)?
                .First(m => m.Name.EndsWith("b__0") && m.ReturnType == typeof(void));//AccessTools.Method("Fluffy_Breakdowns.JobDriver_Maintenance:set_Durability");
            if (method == null)
                return false;

            h.Patch(method, transpiler: new HarmonyMethod(typeof(Breakdowns_Maintenance_Patch).GetMethod("Transpiler")));
            return true;
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instr)
        {
            var m = typeof(Breakdowns_Maintenance_Patch).GetMethod("FullRepairTicks");
            foreach (CodeInstruction i in instr)
            {
                yield return i;
                if (i.opcode == OpCodes.Ldc_R4 && (float) i.operand == fullRepairTicks)
                {
                    #if _DEBUG
                    Log.Warning($"[WorkRebalancer] Fluffy_Breakdowns.JobDriver_Maintenance transpiler success!");
                    #endif
                    yield return new CodeInstruction(OpCodes.Call, m);
                }
            }
        }

        public static float FullRepairTicks(float value)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return value;

            float percentOfBase = WorkRebalancerMod.Instance.Prof.PercentOfBaseFluffyBreakdowns / 100f;
            //Log.Warning($"FullRepairTicks {value} => {value * percentOfBase}");
            return value * percentOfBase;
        }
    }
}

/*
private void ResetTicksToPickHit()
{
	float num = this.pawn.GetStatValue(StatDefOf.MiningSpeed, true);
	if (num < 0.6f && this.pawn.Faction != Faction.OfPlayer)
	{
		num = 0.6f;
	}
	this.ticksToPickHit = (int)Math.Round((double)(100f / num));
}
 */