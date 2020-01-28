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
        public static bool Apply(HarmonyInstance h)
        {
            Type SkillRecord_Type = AccessTools.TypeByName("RimWorld.SkillRecord");
            if (SkillRecord_Type != null)
            {
                var Learn = AccessTools.Method(SkillRecord_Type, "Learn");
                if (Learn != null)
                {
                    h.Patch(Learn, prefix: new HarmonyMethod(AccessTools.Method(typeof(SkillRecord_Learn_Patch), "LearnPrefix")) {prioritiy = Priority.First});
                    return true;
                }
            }
            return false;
        }

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