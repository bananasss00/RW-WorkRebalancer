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
    public class HSK_Extractors_Patch
    {
        public static bool Apply(HarmonyInstance h)
        {
            bool extractor = h.PatchPostfix("SK.Building_Extractor:get_MinePerPortionCurrentDifficulty", typeof(HSK_Extractors_Patch).GetMethod("MinePerPortionCurrentDifficulty_Postfix"));
            bool advancedExtractor = h.PatchPostfix("SK.Building_AdvancedExtractor:get_MinePerPortionCurrentDifficulty", typeof(HSK_Extractors_Patch).GetMethod("MinePerPortionCurrentDifficulty_Postfix"));
            return extractor && advancedExtractor;
        }

        public static void MinePerPortionCurrentDifficulty_Postfix(ref float __result)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                WorkRebalancerMod.Instance.HostileDetected)
                return;

            float percentOfBase = WorkRebalancerMod.Instance.Prof.PercentOfBaseHSKExtractorsMine / 100f;
            __result *= percentOfBase;
        }
    }
}