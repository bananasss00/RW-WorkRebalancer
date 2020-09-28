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
    public class RF_Refinery_Patch
    {
        public static bool Apply(Harmony h) => h.PatchPostfix(
            "Rimefeller.CompRefinery:get_FuelConsumedPerTick",
            typeof(RF_Refinery_Patch).GetMethod("FuelConsumedPerTick")
        );

        // Rimefeller.CompRefinery:get_FuelConsumedPerTick
        public static void FuelConsumedPerTick(ref float __result)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return;
            //bool flag = this.HighPowerMode && this.Buffer < this.Props.BufferSize && this.pipeNet.TotalFuel > 0.0;
            //float result;
            //if (flag)
            //{
            //    result = this.Props.ConsumeRate / 60f;
            //}
            //else
            //{
            //    result = 0f;
            //}
            //return result;
            __result *= WorkRebalancerMod.Instance.Prof.RFRefineryJobMultiplier;
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