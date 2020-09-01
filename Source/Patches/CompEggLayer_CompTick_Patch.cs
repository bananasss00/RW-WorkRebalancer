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


        public static readonly FieldInfo eggProgress = AccessTools.Field(typeof(RimWorld.CompEggLayer), "eggProgress");
        public static readonly MethodInfo Active = AccessTools.Method("RimWorld.CompEggLayer:get_Active");
        public static readonly MethodInfo ProgressStoppedBecauseUnfertilized = AccessTools.Method("RimWorld.CompEggLayer:get_ProgressStoppedBecauseUnfertilized");

        // original rebuilded
        public static bool CompTick(CompEggLayer __instance)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                WorkRebalancerMod.Instance.HostileDetected)
                return true;

            if ((bool)Active.Invoke(__instance, null))
            {
                float num = 1f / (__instance.Props.eggLayIntervalDays * 60000f);
                Pawn pawn = __instance.parent as Pawn;
                if (pawn != null)
                {
                    num *= PawnUtility.BodyResourceGrowthSpeed(pawn);
                }

                float _eggProgress = (float) eggProgress.GetValue(__instance);
                _eggProgress += num * WorkRebalancerMod.Instance.Prof.EggLayerSpeedMult;
                if (_eggProgress > 1f)
                {
                    _eggProgress = 1f;
                }
                if ((bool)ProgressStoppedBecauseUnfertilized.Invoke(__instance, null))
                {
                    _eggProgress = __instance.Props.eggProgressUnfertilizedMax;
                }
                eggProgress.SetValue(__instance, _eggProgress);
            }

            return false;
        }
    }
}