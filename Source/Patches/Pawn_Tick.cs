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
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class HotSwappableAttribute : Attribute
    {
    }

    [HotSwappableAttribute]
    // Not implemented disable when hostile detected
    public class Pawn_Tick_Patch
    {
        private static Pawn _pawnTickInstance = null;
        public static bool FastPawnsTicks = false;
        public const int UpdateLifeStageIntervalTick = 60000; // 1 day

        public static bool Apply(Harmony h)
        {
            // PawnTicksBooster Bugged!//

            // var hinttick = AccessTools.Method("Verse.Gen:IsHashIntervalTick", new [] {typeof(Thing), typeof(int)});
            // if (hinttick == null)
            //     return false;
            //
            // h.Patch(hinttick, prefix: new HarmonyMethod(typeof(Pawn_Tick_Patch), nameof(IsHashIntervalTick)) { priority = Priority.First});
            //
            // return h.PatchPrefix("Verse.Pawn:Tick", typeof(Pawn_Tick_Patch).GetMethod(nameof(PawnTickPrefix)), Priority.First) &&
            //     h.PatchPostfix("Verse.Pawn:Tick", typeof(Pawn_Tick_Patch).GetMethod(nameof(PawnTickPostfix)), Priority.Last);


            return h.PatchPostfix("Verse.Pawn:Tick", typeof(Pawn_Tick_Patch).GetMethod(nameof(PawnTickPostfix)), Priority.Last);
        }

        /// <summary>
        /// FastPawnsTicks feature
        /// </summary>
        public static void IsHashIntervalTick(Thing t, ref int interval, ref bool __result)
        {
            //return t.HashOffsetTicks() % interval == 0;
            if (_pawnTickInstance == t)
            {
                int mult = WorkRebalancerMod.Instance.Prof.FastPawnsTicksMultiplier;
                if (mult > 0)
                    interval = Math.Max(interval / mult, 1);
            }
        }

        /// <summary>
        /// FastPawnsTicks feature
        /// </summary>
        public static void PawnTickPrefix(Pawn __instance)
        {
            if (!WorkRebalancerMod.Instance.Prof.ShowFastPawnsTicksIcon)
                FastPawnsTicks = false; // if icon disabled, disable this feature too

            if (FastPawnsTicks)
                _pawnTickInstance = __instance; // FastPawnsTicks: boost by modify IsHashIntervalTick 'interval' arg
        }


        /// <summary>
        /// FastPawnsTicks feature + FastAging
        /// </summary>
        /// <param name="__instance"></param>
        public static void PawnTickPostfix(Pawn __instance)
        {
            _pawnTickInstance = null;

            //if (WorkRebalancerMod.Instance.RestoreWhenHostileDetected &&
            //    WorkRebalancerMod.Instance.HostileDetected)
            //    return;

            if (__instance.IsHashIntervalTick(UpdateLifeStageIntervalTick))
            {
                __instance.ageTracker.CalculateInitialGrowth();
                __instance.ageTracker.RecalculateLifeStageIndex();
            }

            var ageTracker = __instance.ageTracker;
            int multiplier; //How much the settings say the pawn's age speed should be multiplied by.
            int biologicalYears = ageTracker.AgeBiologicalYears; // inlined function: __instance.ageTracker.AgeBiologicalYears

            //Determine multiplier
            if (__instance.RaceProps.Humanlike)
            {
                //It's a humanlike
                if (biologicalYears < WorkRebalancerMod.Instance.Prof.PawnCutoffAge)
                {
                    //It's before the cutoff age
                    multiplier = WorkRebalancerMod.Instance.Prof.PawnSpeedMultBeforeCutoff;
                } else
                {
                    //It's after the cutoff age
                    multiplier = WorkRebalancerMod.Instance.Prof.PawnSpeedMultAfterCutoff;
                }
            } else
            {
                //It's an animal
                if (biologicalYears < WorkRebalancerMod.Instance.Prof.AnimalCutoffAge)
                {
                    //It's before the cutoff age
                    multiplier = WorkRebalancerMod.Instance.Prof.AnimalSpeedMultBeforeCutoff;
                }
                else
                {
                    //It's after the cutoff age
                    multiplier = WorkRebalancerMod.Instance.Prof.AnimalSpeedMultAfterCutoff;
                }
            }


            //Run extra aging.
            if (multiplier == 0) //Aging disabled, reverse every tick of age increase
            {
                // mainf increase, postfix decrease = freezy value
                ageTracker.ageBiologicalTicksInt -= 1; //This theoretically could cause a birthday every tick if the multiplier is set to 0 on the tick before a birthday. It would be better as a prefix patch that prevents AgeTick from even running, but that's a lot of work for a super edge case.
            }
            else
            {
                int ageBefore = biologicalYears;

                // same as DebugMakeOlder method
                var ticks = multiplier - 1;
                ageTracker.ageBiologicalTicksInt += ticks; // delayed add additional ticks
                ageTracker.birthAbsTicksInt -= ticks;

                biologicalYears = __instance.ageTracker.AgeBiologicalYears; // inlined function: __instance.ageTracker.AgeBiologicalYears
                for (; ageBefore < biologicalYears; ageBefore++)
                {
                    ageTracker.BirthdayBiological();
                }
            }
        }
    }
}