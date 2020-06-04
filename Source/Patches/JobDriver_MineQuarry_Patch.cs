﻿using System;
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
        public static bool Apply(HarmonyInstance h) => h.PatchPostfix(
            "Quarry.JobDriver_MineQuarry:Mine",
            typeof(JobDriver_MineQuarry_Patch).GetMethod("MinePostfix")
        );

        public static void MinePostfix(ref Toil __result)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                WorkRebalancerMod.Instance.HostileDetected)
                return;

            float percentOfBase = WorkRebalancerMod.Instance.Prof.PercentOfBaseHSKMineQuarry / 100f;
            __result.defaultDuration = (int)(__result.defaultDuration * percentOfBase);
        }
    }
}