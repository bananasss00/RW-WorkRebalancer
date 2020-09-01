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
            return h.PatchPostfix("rjw.Hediff_InsectEgg:Tick", typeof(RJW_Hediff_InsectEgg_Tick_Patch).GetMethod("Postfix"));
        }

        public static void Postfix(ref int ___ageTicks)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                WorkRebalancerMod.Instance.HostileDetected)
                return;

            if (WorkRebalancerMod.Instance.Prof.RjwInsectEggSpeedMult > 1)
            {
                ___ageTicks += WorkRebalancerMod.Instance.Prof.RjwInsectEggSpeedMult - 1; // sub - 1 bcs in orig. Tick() was added 1 tick
            }
        }
    }
}