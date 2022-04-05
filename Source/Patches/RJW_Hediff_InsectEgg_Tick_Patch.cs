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
    public class RJW_Hediff_InsectEgg_Tick_Patch
    {
        public static bool Apply(Harmony h)
        {
            return h.PatchPrefix("rjw.Hediff_InsectEgg:set_GestationProgress", typeof(RJW_Hediff_InsectEgg_Tick_Patch).GetMethod("Prefix"));
        }

        public static bool Prefix(HediffWithComps __instance, ref float ___Gestation, ref float ___p_start_tick, ref float ___p_end_tick)
        {
            // Tick(): this.GestationProgress = (1f + (float)Find.TickManager.TicksGame - this.p_start_tick) / (this.p_end_tick - this.p_start_tick);

            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return true;

            float curTick = Find.TickManager.TicksGame;
            float delta = ___p_end_tick - ___p_start_tick;
            float newEndTick = ___p_start_tick + (delta / WorkRebalancerMod.Instance.Prof.RjwInsectEggSpeedMult);
            ___Gestation = (1f + curTick - ___p_start_tick) / (newEndTick - ___p_start_tick);
            return false;
        }
    }
}