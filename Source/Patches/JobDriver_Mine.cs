using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkRebalancer.Patches
{
    public class JobDriver_Mine_Patch
    {
        private static readonly FieldInfo TicksToNextRepair = AccessTools.Field(typeof(JobDriver_Repair), "ticksToNextRepair");

        public static bool Apply(HarmonyInstance h) => h.PatchPostfix(
            "RimWorld.JobDriver_Mine:ResetTicksToPickHit",
            typeof(JobDriver_Mine_Patch).GetMethod("ResetTicksToPickHitPostfix")
        );

        public static void ResetTicksToPickHitPostfix(ref int ___ticksToPickHit)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                WorkRebalancerMod.Instance.HostileDetected)
                return;

            float percentOfBase = WorkRebalancerMod.Instance.Prof.PercentOfBaseMineJob / 100f;
            //Log.Warning($"Mine {___ticksToPickHit} => {(int)(___ticksToPickHit * percentOfBase)}");
            ___ticksToPickHit = (int)(___ticksToPickHit * percentOfBase);
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