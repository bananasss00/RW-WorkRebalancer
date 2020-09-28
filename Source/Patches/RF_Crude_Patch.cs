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
    public class RF_Crude_Patch
    {
        public static bool Apply(Harmony h)
        {
            return h.PatchPostfix(
                "Rimefeller.CompCrudeCracker:get_ProducedRate",
                typeof(RF_Crude_Patch).GetMethod("ProducedRate")
            )
                   &&
            h.PatchPostfix(
                "Rimefeller.CompCrudeCracker:get_CrudeConsumedPerTick",
                typeof(RF_Crude_Patch).GetMethod("CrudeConsumedPerTick")
            );
        }

        // Rimefeller.CompCrudeCracker:get_ProducedRate
        public static void ProducedRate(ref float __result)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return;
            //return this.Props.ProducedPerSecond * RimefellerMod.Settings.CrudeFuelRatio;
            __result *= WorkRebalancerMod.Instance.Prof.RFCrudeJobMultiplier;
        }

        // Rimefeller.CompCrudeCracker:get_CrudeConsumedPerTick
        public static void CrudeConsumedPerTick(ref float __result)
        {
            if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
                HostileHandler.HostileDetected)
                return;
            __result *= WorkRebalancerMod.Instance.Prof.RFCrudeJobMultiplier;
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