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
    public class HSK_CollectJobs_Patch
    {
        public static bool Apply(HarmonyInstance h)
        {
            Type JobDriver_CollectSand_Type = AccessTools.TypeByName("SK.JobDriver_CollectSand");
            Type JobDriver_CollectClay_Type = AccessTools.TypeByName("SK.JobDriver_CollectClay");
            Type JobDriver_CollectPeat_Type = AccessTools.TypeByName("SK.JobDriver_CollectPeat");
            if (JobDriver_CollectSand_Type != null && JobDriver_CollectPeat_Type != null && JobDriver_CollectClay_Type != null)
            {
                var CollectSand = AccessTools.Method(JobDriver_CollectSand_Type, "CollectSand");
                var CollectPeat = AccessTools.Method(JobDriver_CollectPeat_Type, "CollectPeat");
                var CollectClay = AccessTools.Method(JobDriver_CollectClay_Type, "CollectClay");
                if (CollectSand != null && CollectPeat != null && CollectClay != null)
                {
                    h.Patch(CollectSand, prefix: new HarmonyMethod(AccessTools.Method(typeof(HSK_CollectJobs_Patch), "HSK_CollectJobPrefix")));
                    h.Patch(CollectPeat, prefix: new HarmonyMethod(AccessTools.Method(typeof(HSK_CollectJobs_Patch), "HSK_CollectJobPrefix")));
                    h.Patch(CollectClay, prefix: new HarmonyMethod(AccessTools.Method(typeof(HSK_CollectJobs_Patch), "HSK_CollectJobPrefix")));
                    return true;
                }
            }
            return false;
        }

        public static void HSK_CollectJobPrefix(ref int ticksToCollect)
        {
            if (WorkRebalancerMod.Instance.RestoreWhenHostileDetected.Value &&
                WorkRebalancerMod.Instance.HostileDetected)
                return;

            float percentOfBase = WorkRebalancerMod.Instance.PercentOfBaseHSKCollectJobs.Value / 100f;
            ticksToCollect = (int)(ticksToCollect * percentOfBase);
        }
    }
}