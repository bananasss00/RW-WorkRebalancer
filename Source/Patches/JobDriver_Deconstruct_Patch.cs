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
    public class JobDriver_Deconstruct_Patch
    {
        public static bool Apply(HarmonyInstance h) => h.PatchPostfix(
            "RimWorld.JobDriver_Deconstruct:get_TotalNeededWork",
            typeof(JobDriver_Deconstruct_Patch).GetMethod("TotalNeededWorkPostfix")
        );

        public static void TotalNeededWorkPostfix(ref float __result)
        {
            if (WorkRebalancerMod.Instance.RestoreWhenHostileDetected &&
                WorkRebalancerMod.Instance.HostileDetected)
                return;

            float percentOfBase = WorkRebalancerMod.Instance.PercentOfBaseThingStats / 100f;
            __result *= percentOfBase;
        }
    }
}