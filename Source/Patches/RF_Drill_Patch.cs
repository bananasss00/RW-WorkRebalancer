﻿using System;
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
    public class RF_Drill_Patch
    {
        public static bool Apply(Harmony h) => h.PatchPrefix(
            "Rimefeller.JobDriver_SuperviseDrilling:MakeNewToils",
            typeof(RF_Drill_Patch).GetMethod("MakeNewToilsPrefix")
        );

        public static bool MakeNewToilsPrefix(object __instance, ref IEnumerable<Toil> __result)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return true;

            __result = MakeNewToils(__instance, WorkRebalancerMod.Instance.Prof.RFDrillJobMultiplier);
            return false;
        }

        public static IEnumerable<Toil> MakeNewToils(object __instance, float drillInTickMultiplier = 1f)
        {
	        JobDriver jd = (JobDriver) __instance;
	        jd.EndOnDespawnedOrNull(TargetIndex.A);
	        Toil toil = Toils_Misc.FindRandomAdjacentReachableCell(TargetIndex.A, TargetIndex.B);
	        yield return toil;
	        yield return Toils_Reserve.Reserve(TargetIndex.B);
	        yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);

	        Toil supervise = new Toil();
	        supervise.tickAction = delegate()
	        {
		        LocalTargetInfo TargetA = jd.TargetA;
		        jd.pawn.rotationTracker.FaceCell(TargetA.Thing.OccupiedRect().ClosestCellTo(jd.pawn.Position));
		        Pawn actor = supervise.actor;
		        actor.skills.Learn(SkillDefOf.Construction, 0.275f);
		        float statValue = actor.GetStatValue(StatDefOf.ConstructionSpeed);

		        //CompOilDerrick compOilDerrick = TargetA.Thing.TryGetComp<CompOilDerrick>();
		        object compOilDerrick = null;
		        { // getting compOilDerrick
			        ThingWithComps thingWithComps = TargetA.Thing as ThingWithComps;
			        if (thingWithComps != null)
			        {
				        var found = thingWithComps.AllComps?.Where(x => x.GetType().ToString().StartsWith("CompOilDerrick")).FirstOrDefault();
				        if (found != null)
				        {
					        compOilDerrick = found;
				        }
			        }
		        }

		        if (compOilDerrick != null)
		        {
			        bool flag2 = !Traverse.Create(compOilDerrick).Property("WorkingNow").GetValue<bool>();
			        if (flag2)
			        {
				        jd.EndJobWith(JobCondition.Incompletable);
			        }

			        // compOilDerrick.Drill(statValue);
			        Traverse.Create(compOilDerrick).Method("Drill", statValue * drillInTickMultiplier).GetValue();
			        bool isDilled = Traverse.Create(compOilDerrick).Property("IsDilled").GetValue<bool>();
			        if (isDilled)
			        {
				        jd.EndJobWith(JobCondition.Succeeded);
			        }
		        }
	        };
	        supervise.handlingFacing = true;
	        supervise.defaultCompleteMode = ToilCompleteMode.Never;
	        supervise.activeSkill = (() => SkillDefOf.Construction);
	        yield return supervise;
        }
    }
}

/*protected override IEnumerable<Toil> MakeNewToils()
{
	this.EndOnDespawnedOrNull(TargetIndex.A, JobCondition.Incompletable);
	Toil toil = Toils_Misc.FindRandomAdjacentReachableCell(TargetIndex.A, TargetIndex.B);
	yield return toil;
	yield return Toils_Reserve.Reserve(TargetIndex.B, 1, -1, null);
	yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
	Toil supervise = new Toil();
	supervise.tickAction = delegate()
	{
		this.pawn.rotationTracker.FaceCell(this.TargetA.Thing.OccupiedRect().ClosestCellTo(this.pawn.Position));
		Pawn actor = supervise.actor;
		actor.skills.Learn(SkillDefOf.Construction, 0.275f, false);
		float statValue = actor.GetStatValue(StatDefOf.ConstructionSpeed, true);
		CompOilDerrick compOilDerrick = this.TargetA.Thing.TryGetComp<CompOilDerrick>();
		bool flag = compOilDerrick != null;
		if (flag)
		{
			bool flag2 = !compOilDerrick.WorkingNow;
			if (flag2)
			{
				this.EndJobWith(JobCondition.Incompletable);
			}
			compOilDerrick.Drill(statValue);
			bool isDilled = compOilDerrick.IsDilled;
			if (isDilled)
			{
				this.EndJobWith(JobCondition.Succeeded);
			}
		}
	};
	supervise.handlingFacing = true;
	supervise.defaultCompleteMode = ToilCompleteMode.Never;
	supervise.activeSkill = (() => SkillDefOf.Construction);
	yield return supervise;
	yield break;
}*/