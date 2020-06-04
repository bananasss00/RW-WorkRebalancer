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
    public class CompHatcher_CompTick_Patch
    {
        public static bool Apply(HarmonyInstance h) => h.PatchPrefix(
            "RimWorld.CompHatcher:CompTick",
            typeof(CompHatcher_CompTick_Patch).GetMethod("CompTick")
        );

        public static void ModifyHatcher(object inst)
        {
            var _hatcher = Traverse.Create(inst);
            var _gestateProgress = _hatcher.Field("gestateProgress");
            var _Hatch = _hatcher.Method("Hatch");

            float hatcherDaystoHatch = _hatcher.Field("props").Field("hatcherDaystoHatch").GetValue<float>();
            float num = 1f / (hatcherDaystoHatch * 60000f);
            float gestateProgress = _gestateProgress.GetValue<float>();

            //__instance.gestateProgress += num;
            _gestateProgress.SetValue(gestateProgress + (num * WorkRebalancerMod.Instance.Prof.EggHatchSpeedMult));
                
            if (gestateProgress > 1f)
            {
                _Hatch.GetValue();
            }
        }

        // original rebuilded
        public static bool CompTick(CompHatcher __instance)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                WorkRebalancerMod.Instance.HostileDetected)
                return true;

            if (!__instance.TemperatureDamaged)
            {
                ModifyHatcher(__instance);
            }

            return false;
        }
    }
}