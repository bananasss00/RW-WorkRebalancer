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
    // Not implemented disable when hostile detected
    public class Pawn_Tick_Patch
    {
        private static readonly MethodInfo RecalculateLifeStageIndex = AccessTools.Method("Verse.Pawn_AgeTracker:RecalculateLifeStageIndex");

        public static bool Apply(HarmonyInstance h) => h.PatchPostfix(
            "Verse.Pawn:Tick",
            typeof(Pawn_Tick_Patch).GetMethod("multiTick")
        );

        public static void multiTick(Pawn __instance)
        {
            int multiplier = 0; //How much the settings say the pawn's age speed should be multiplied by.

            //Determine multiplier
            if (__instance.RaceProps.Humanlike)
            {
                //It's a humanlike
                if (__instance.ageTracker.AgeBiologicalYears < WorkRebalancerMod.Instance.PawnCutoffAge)
                {
                    //It's before the cutoff age
                    multiplier = WorkRebalancerMod.Instance.PawnSpeedMultBeforeCutoff;
                } else
                {
                    //It's after the cutoff age
                    multiplier = WorkRebalancerMod.Instance.PawnSpeedMultAfterCutoff;
                }
            } else
            {
                //It's an animal
                if (__instance.ageTracker.AgeBiologicalYears < WorkRebalancerMod.Instance.AnimalCutoffAge)
                {
                    //It's before the cutoff age
                    multiplier = WorkRebalancerMod.Instance.AnimalSpeedMultBeforeCutoff;
                }
                else
                {
                    //It's after the cutoff age
                    multiplier = WorkRebalancerMod.Instance.AnimalSpeedMultAfterCutoff;
                }
            }


            //Run extra aging.
            if (multiplier == 0) //Aging disabled, reverse every tick of age increase
            {
                __instance.ageTracker.AgeBiologicalTicks += -1; //This theoretically could cause a birthday every tick if the multiplier is set to 0 on the tick before a birthday. It would be better as a prefix patch that prevents AgeTick from even running, but that's a lot of work for a super edge case.
            }
            else
            {
                int age = __instance.ageTracker.AgeBiologicalYears; // if age changed recalc
                
                for (int additionalTick = 0; additionalTick < multiplier - 1; additionalTick++) //Repeat the same AgeTick method until it hase been done speedMult times this tick
                {
                    __instance.ageTracker.AgeTick();
                }

                if (__instance.ageTracker.AgeBiologicalYears != age)
                    RecalculateLifeStageIndex.Invoke(__instance.ageTracker, null);
            }
        }
    }
}