using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace WorkRebalancer.Patches
{
    public class CompScanner_Patch
    {
        public static bool Apply(Harmony h) => h.PatchPrefix(
            "RimWorld.CompScanner:TickDoesFind",
            typeof(CompScanner_Patch).GetMethod("TickDoesFind")
        );

        public static void TickDoesFind(CompScanner __instance, ref float scanSpeed)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return;

            /*
            public void Used(Pawn worker)
		    {
			    if (!this.CanUseNow)
			    {
				    Log.Error("Used while CanUseNow is false.", false);
			    }
			    this.lastScanTick = (float)Find.TickManager.TicksGame;
			    this.lastUserSpeed = 1f;
			    if (this.Props.scanSpeedStat != null)
			    {
				    this.lastUserSpeed = worker.GetStatValue(this.Props.scanSpeedStat, true); // fix 1
			    }
			    this.daysWorkingSinceLastFinding += this.lastUserSpeed / 60000f; // fix 2
			    if (this.TickDoesFind(this.lastUserSpeed))
			    {
				    this.DoFind(worker);
				    this.daysWorkingSinceLastFinding = 0f;
			    }
		    }
             */
            //return Find.TickManager.TicksGame % 59 == 0 && (Rand.MTBEventOccurs(this.Props.scanFindMtbDays / scanSpeed, 60000f, 59f) || (this.Props.scanFindGuaranteedDays > 0f && this.daysWorkingSinceLastFinding >= this.Props.scanFindGuaranteedDays));
            __instance.lastUserSpeed *= WorkRebalancerMod.Instance.Prof.DeepScannerJob; // fix view 1
            __instance.daysWorkingSinceLastFinding += (__instance.lastUserSpeed - scanSpeed) / 60000f; // fix view 2
            scanSpeed = __instance.lastUserSpeed;
        }
    }
}