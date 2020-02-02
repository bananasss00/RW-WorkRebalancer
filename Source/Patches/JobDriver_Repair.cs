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
    public class JobDriver_Repair_Patch
    {
        private static readonly FieldInfo TicksToNextRepair = AccessTools.Field(typeof(JobDriver_Repair), "ticksToNextRepair");

        public static bool Apply(HarmonyInstance h) => h.PatchPrefix(
            "RimWorld.JobDriver_Repair:MakeNewToils",
            typeof(JobDriver_Repair_Patch).GetMethod("MakeNewToilsPrefix")
        );

        public static bool MakeNewToilsPrefix(JobDriver_Repair __instance, ref IEnumerable<Toil> __result)
        {
            if (WorkRebalancerMod.Instance.RestoreWhenHostileDetected &&
                WorkRebalancerMod.Instance.HostileDetected)
                return true;

            __result = MakeNewToils(__instance, WorkRebalancerMod.Instance.RepairJobAddX);
            return false;
        }

        public static IEnumerable<Toil> MakeNewToils(JobDriver_Repair __instance, int repairInTick = 1)
        {
	        __instance.FailOnDespawnedNullOrForbidden(TargetIndex.A);

	        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
	        
            Toil repair = new Toil();
	        Thing TargetThingA = Traverse.Create(__instance).Property("TargetThingA").GetValue<Thing>();

	        repair.initAction = () => TicksToNextRepair.SetValue(__instance, 80f);

	        repair.tickAction = () =>
	        {
		        Pawn actor = repair.actor;
		        actor.skills.Learn(SkillDefOf.Construction, 0.05f);
		        float statValue = actor.GetStatValue(StatDefOf.ConstructionSpeed);
		        float ticksToNextRepair = (float) TicksToNextRepair.GetValue(__instance) - statValue;
		        TicksToNextRepair.SetValue(__instance, ticksToNextRepair); // this.ticksToNextRepair -= statValue;
		        if (ticksToNextRepair <= 0f)
		        {
			        ticksToNextRepair += 20f;
			        TicksToNextRepair.SetValue(__instance, ticksToNextRepair);

			        //TargetThingA.HitPoints++;
			        TargetThingA.HitPoints += repairInTick;
			        TargetThingA.HitPoints = Mathf.Min(TargetThingA.HitPoints, TargetThingA.MaxHitPoints);

			        Map Map = Traverse.Create(__instance).Property("Map").GetValue<Map>();
			        Map.listerBuildingsRepairable.Notify_BuildingRepaired((Building) TargetThingA);
			        if (TargetThingA.HitPoints == TargetThingA.MaxHitPoints)
			        {
				        actor.records.Increment(RecordDefOf.ThingsRepaired);
				        actor.jobs.EndCurrentJob(JobCondition.Succeeded);
			        }
		        }
	        };
	        repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
	        repair.WithEffect(TargetThingA.def.repairEffect, TargetIndex.A);
	        repair.defaultCompleteMode = ToilCompleteMode.Never;
	        repair.activeSkill = (() => SkillDefOf.Construction);
	        yield return repair;
        }
    }
}

/*protected override IEnumerable<Toil> MakeNewToils()
{
	this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
	yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
	Toil repair = new Toil();
	repair.initAction = delegate()
	{
		this.ticksToNextRepair = 80f;
	};
	repair.tickAction = delegate()
	{
		Pawn actor = repair.actor;
		actor.skills.Learn(SkillDefOf.Construction, 0.05f, false);
		float statValue = actor.GetStatValue(StatDefOf.ConstructionSpeed, true);
		this.ticksToNextRepair -= statValue;
		if (this.ticksToNextRepair <= 0f)
		{
			this.ticksToNextRepair += 20f;
			this.TargetThingA.HitPoints++;
			this.TargetThingA.HitPoints = Mathf.Min(this.TargetThingA.HitPoints, this.TargetThingA.MaxHitPoints);
			this.Map.listerBuildingsRepairable.Notify_BuildingRepaired((Building)this.TargetThingA);
			if (this.TargetThingA.HitPoints == this.TargetThingA.MaxHitPoints)
			{
				actor.records.Increment(RecordDefOf.ThingsRepaired);
				actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
			}
		}
	};
	repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
	repair.WithEffect(base.TargetThingA.def.repairEffect, TargetIndex.A);
	repair.defaultCompleteMode = ToilCompleteMode.Never;
	repair.activeSkill = (() => SkillDefOf.Construction);
	yield return repair;
	yield break;
}*/