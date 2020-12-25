
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
    public class JobDriver_WearApparel_Patch
    {
        public static bool Apply(Harmony h) => h.PatchPostfix("RimWorld.JobDriver_Wear:Notify_Starting", typeof(JobDriver_WearApparel_Patch).GetMethod("Notify_Starting")) &&
                                               h.PatchPostfix("RimWorld.JobDriver_RemoveApparel:Notify_Starting", typeof(JobDriver_WearApparel_Patch).GetMethod("Notify_Starting"));

        public static void Notify_Starting(JobDriver_Wear __instance)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return;

            float percentOfBase = WorkRebalancerMod.Instance.Prof.PercentOfBaseWearApparel / 100f;
            __instance.duration = (int)(__instance.duration * percentOfBase);
        }
    }
}