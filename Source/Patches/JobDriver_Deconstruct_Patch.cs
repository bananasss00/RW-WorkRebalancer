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
    public class JobDriver_Deconstruct_Patch
    {
        public static bool Apply(HarmonyInstance h)
        {
            Type JobDriver_Deconstruct = AccessTools.TypeByName("RimWorld.JobDriver_Deconstruct");
            if (JobDriver_Deconstruct != null)
            {
                var TotalNeededWork = AccessTools.Property(JobDriver_Deconstruct, "TotalNeededWork").GetGetMethod();
                if (TotalNeededWork != null)
                {
                    h.Patch(TotalNeededWork, postfix: new HarmonyMethod(AccessTools.Method(typeof(JobDriver_Deconstruct_Patch), "TotalNeededWorkPostfix")));
                    return true;
                }
            }
            return false;
        }

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