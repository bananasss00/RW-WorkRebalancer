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
    public class CompEggLayer_CompTick_Patch
    {
        public static bool Apply(Harmony h) => h.PatchPrefix(
            "RimWorld.CompEggLayer:CompTick",
            typeof(CompEggLayer_CompTick_Patch).GetMethod("CompTick")
        );

        // original rebuilded
        public static bool CompTick(CompEggLayer __instance)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return true;

            if (__instance.Active)
            {
                float num = 1f / (__instance.Props.eggLayIntervalDays * 60000f);
                Pawn pawn = __instance.parent as Pawn;
                if (pawn != null)
                {
                    num *= PawnUtility.BodyResourceGrowthSpeed(pawn);
                }

                ref float eggProgress = ref __instance.eggProgress;
                eggProgress += num * WorkRebalancerMod.Instance.Prof.EggLayerSpeedMult;
                if (eggProgress > 1f)
                {
                    eggProgress = 1f;
                }
                if (__instance.ProgressStoppedBecauseUnfertilized)
                {
                    eggProgress = __instance.Props.eggProgressUnfertilizedMax;
                }
            }

            return false;
        }
    }
}