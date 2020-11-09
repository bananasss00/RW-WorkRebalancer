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
    public class RimatomicsResearch_Patch
    {
        public static bool Apply(Harmony h)
        {
            return h.PatchPrefix("Rimatomics.RimatomicsResearch:ResearchPerformed", typeof(RimatomicsResearch_Patch).GetMethod("ResearchPerformedPrefix"));
        }

        public static void ResearchPerformedPrefix(object step, ref float amount, Pawn researcher, object bench)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return;

            amount *= WorkRebalancerMod.Instance.Prof.RAtomicsResearchMultiplier;
        }
    }
}