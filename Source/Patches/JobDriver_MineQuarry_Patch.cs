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
    public class JobDriver_MineQuarry_Patch
    {
        public static bool Apply(HarmonyInstance h)
        {
            Type JobDriver_MineQuarry_Type = AccessTools.TypeByName("Quarry.JobDriver_MineQuarry");
            if (JobDriver_MineQuarry_Type != null)
            {
                var Mine = AccessTools.Method(JobDriver_MineQuarry_Type, "Mine");
                if (Mine != null)
                {
                    h.Patch(Mine, postfix: new HarmonyMethod(AccessTools.Method(typeof(JobDriver_MineQuarry_Patch), "MinePostfix")));
                    return true;
                }
            }
            return false;
        }

        public static void MinePostfix(ref Toil __result)
        {
            if (WorkRebalancerMod.Instance.RestoreWhenHostileDetected.Value &&
                WorkRebalancerMod.Instance.HostileDetected)
                return;

            float percentOfBase = WorkRebalancerMod.Instance.PercentOfBaseHSKMineQuarry.Value / 100f;
            __result.defaultDuration = (int)(__result.defaultDuration * percentOfBase);
        }
    }
}