﻿using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkRebalancer.Patches
{
    public class RJW_Hediff_BasePregnancy_Tick_Patch
    {
        public static bool Apply(Harmony h)
        {
            return h.PatchPrefix("rjw.Hediff_BasePregnancy:Tick", typeof(RJW_Hediff_BasePregnancy_Tick_Patch).GetMethod("Prefix"))
                && h.PatchPostfix("rjw.Hediff_BasePregnancy:Tick", typeof(RJW_Hediff_BasePregnancy_Tick_Patch).GetMethod("Postfix"));
        }

        public static void Prefix(ref float ___progress_per_tick, ref float __state)
        {
            __state = ___progress_per_tick; // backup;

            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return;

            ___progress_per_tick *= WorkRebalancerMod.Instance.Prof.RjwPregnancySpeedMult;
        }

        public static void Postfix(ref float ___progress_per_tick, float __state)
        {
            ___progress_per_tick = __state; // restore
        }
    }
}