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
            bool sand = h.PatchPrefix("SK.JobDriver_CollectSand:CollectSand", typeof(HSK_CollectJobs_Patch).GetMethod("HSK_CollectJobPrefix"));
            bool clay = h.PatchPrefix("SK.JobDriver_CollectClay:CollectClay", typeof(HSK_CollectJobs_Patch).GetMethod("HSK_CollectJobPrefix"));
            bool peat = h.PatchPrefix("SK.JobDriver_CollectPeat:CollectPeat", typeof(HSK_CollectJobs_Patch).GetMethod("HSK_CollectJobPrefix"));
            bool cstone = h.PatchPrefix("SK.JobDriver_CollectCrushedstone:CrushedStone", typeof(HSK_CollectJobs_Patch).GetMethod("HSK_CollectJobPrefix"));
            return sand && clay && peat && cstone;
        }

        public static void HSK_CollectJobPrefix(ref int ticksToCollect)
        {
            if (WorkRebalancerMod.Instance.RestoreWhenHostileDetected &&
                WorkRebalancerMod.Instance.HostileDetected)
                return;

            float percentOfBase = WorkRebalancerMod.Instance.PercentOfBaseHSKCollectJobs / 100f;
            ticksToCollect = (int)(ticksToCollect * percentOfBase);
        }
    }
}