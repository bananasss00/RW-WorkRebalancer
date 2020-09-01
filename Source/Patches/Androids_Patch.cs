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
    public class Androids_Patch
    {
        public static bool Apply(Harmony h)
        {
            bool print = h.PatchPrefix("Androids.Building_AndroidPrinter:Tick", typeof(Androids_Patch).GetMethod("PreTickPrint"));
            bool craft = h.PatchPrefix("Androids.Building_PawnCrafter:Tick", typeof(Androids_Patch).GetMethod("PreTickCraft"));
            return print && craft;
        }

        public static void PreTickPrint(ref int ___printingTicksLeft, ref int ___nextResourceTick)
        {
            PrintTick(ref ___printingTicksLeft, ref ___nextResourceTick);
        }

        public static void PreTickCraft(ref int ___craftingTicksLeft, ref int ___nextResourceTick)
        {
            PrintTick(ref ___craftingTicksLeft, ref ___nextResourceTick);
        }

        private static void PrintTick(ref int printTick, ref int resTick)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                WorkRebalancerMod.Instance.HostileDetected)
                return;

            int sub = WorkRebalancerMod.Instance.Prof.AndroidsCraftAddX - 1; // pseudo multiplier, sub 1 original tick
            if (sub > 0)
            {
                printTick -= sub;
                resTick -= sub;
            }
        }
    }
}