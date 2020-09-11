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
    public class Arachnophobia_CompMultiHatcher_CompTick_Patch
    {
        public static bool Apply(Harmony h) => h.PatchPrefix(
            "Arachnophobia.CompMultiHatcher:CompTick",
            typeof(Arachnophobia_CompMultiHatcher_CompTick_Patch).GetMethod("CompTick")
        );

        // original rebuilded
        public static bool CompTick(object __instance)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return true;

            CompHatcher_CompTick_Patch.ModifyHatcher(__instance);
            return false;
        }
    }
}