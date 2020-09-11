using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkRebalancer.Patches
{
    public class JobDriver_RemoveFloor_Patch
    {
        public static bool Apply(Harmony h) => h.PatchPostfix(
            "RimWorld.JobDriver_RemoveFloor:get_BaseWorkAmount",
            typeof(JobDriver_RemoveFloor_Patch).GetMethod("get_BaseWorkAmount")
        );

        public static void get_BaseWorkAmount(ref int __result)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return;

            float percentOfBase = WorkRebalancerMod.Instance.Prof.PercentOfBaseTerrains / 100f;
            __result = (int)(__result * percentOfBase);
        }
    }
}

/*protected override int BaseWorkAmount => 200;*/