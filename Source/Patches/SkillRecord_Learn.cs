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
    public class SkillRecord_Learn_Patch
    {
        public static bool Apply(HarmonyInstance h) => h.PatchPrefix(
                "RimWorld.SkillRecord:Learn",
                typeof(SkillRecord_Learn_Patch).GetMethod("LearnPrefix"),
                Priority.First
            );

        public static void LearnPrefix(/*SkillRecord __instance, Pawn ___pawn, */ref float xp, bool direct)
        {
            if (WorkRebalancerMod.Instance.RestoreWhenHostileDetected &&
                WorkRebalancerMod.Instance.HostileDetected)
                return;

            // skip negate skill
            if (xp > 0f && (WorkRebalancerMod.Instance.SkillLearnAllowMax == 0 || xp < WorkRebalancerMod.Instance.SkillLearnAllowMax))
            {
                xp *= WorkRebalancerMod.Instance.SkillLearnMultiplier;
            }
        }
    }
}