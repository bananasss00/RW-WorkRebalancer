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
    public class Hediff_Pregnant_Patch
    {
        public static bool Apply(Harmony h)
        {
            bool pref = h.PatchPrefix("Verse.Hediff_Pregnant:set_GestationProgress", typeof(Hediff_Pregnant_Patch).GetMethod("set_GestationProgressPrefix"));
            bool post = h.PatchPostfix("Verse.Hediff_Pregnant:set_GestationProgress", typeof(Hediff_Pregnant_Patch).GetMethod("set_GestationProgressPostfix"));
            return pref && post;
        }

        public static void set_GestationProgressPrefix(Hediff_Pregnant __instance, float value, ref float __state)
        {
            __state = __instance.Severity;
        }

        public static void set_GestationProgressPostfix(Hediff_Pregnant __instance, float value, float __state)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return;

            float delta = __instance.Severity - __state;
            __instance.Severity = __state + (delta * WorkRebalancerMod.Instance.Prof.PregnancySpeedMult);
        }
    }
}