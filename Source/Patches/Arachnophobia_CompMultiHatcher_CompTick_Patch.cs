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
    public class Arachnophobia_CompMultiHatcher_CompTick_Patch
    {
        public static bool Apply(HarmonyInstance h) => h.PatchPrefix(
            "Arachnophobia.CompMultiHatcher:CompTick",
            typeof(Arachnophobia_CompMultiHatcher_CompTick_Patch).GetMethod("CompTick")
        );

        public static bool CompTick(object __instance)
        {
            CompHatcher_CompTick_Patch.ModifyHatcher(__instance);
            return false;
        }
    }
}