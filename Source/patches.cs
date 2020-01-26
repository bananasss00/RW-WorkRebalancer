// interface
// -IsEmpty
// -static GetAll (IWorkAmount)

// 100% if danger //



// DECONSTRUCT //
DefDatabase<ThingDef>.AllDefsListForReading.ForEach(x =>
{
	if (x.building?.uninstallWork != null)
	{
		x.building.uninstallWork *= 0.1f;
	}
});

// HSK COLLECT JOBS //
public static void HSK_CollectJob(ref int ticksToCollect)
{
	float percentOfBase = 0.01f;
	ticksToCollect = (int)(ticksToCollect * percentOfBase);
}

Type JobDriver_CollectSand_Type = AccessTools.TypeByName("SK.JobDriver_CollectSand");
Type JobDriver_CollectClay_Type = AccessTools.TypeByName("SK.JobDriver_CollectClay");
Type JobDriver_CollectPeat_Type = AccessTools.TypeByName("SK.JobDriver_CollectPeat");
if (JobDriver_CollectSand_Type != null && JobDriver_CollectPeat_Type != null && JobDriver_CollectClay_Type != null)
{
	var CollectSand = AccessTools.Method(JobDriver_CollectSand_Type, "CollectSand");
	var CollectPeat = AccessTools.Method(JobDriver_CollectPeat_Type, "CollectPeat");
	var CollectClay = AccessTools.Method(JobDriver_CollectClay_Type, "CollectClay");

	if (CollectSand != null && CollectPeat != null && CollectClay != null)
	{
		ha.Unpatch(CollectSand, HarmonyPatchType.All, "develww");
		ha.Unpatch(CollectPeat, HarmonyPatchType.All, "develww");
		ha.Unpatch(CollectClay, HarmonyPatchType.All, "develww");
		ha.Patch(CollectSand, prefix: new HarmonyMethod(AccessTools.Method(typeof(__Class), "HSK_CollectJob")));
		ha.Patch(CollectPeat, prefix: new HarmonyMethod(AccessTools.Method(typeof(__Class), "HSK_CollectJob")));
		ha.Patch(CollectClay, prefix: new HarmonyMethod(AccessTools.Method(typeof(__Class), "HSK_CollectJob")));
		Log.Warning("Patched!");
	}
}

// RIMFELLER DRILL //
public static IEnumerable<Toil> MakeNewToilsRfDrill(object __instance, float drillInTickMultiplier = 1f)
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
		LocalTargetInfo TargetA = Traverse.Create(__instance).Property("TargetA").GetValue<LocalTargetInfo>();
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

		bool flag = compOilDerrick != null;
		if (flag)
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

public static bool MakeNewToilsRfDrillPrefix(object __instance, ref IEnumerable<Toil> __result)
{
	__result = MakeNewToilsRfDrill(__instance, 100f);
	return false;
}

Type JobDriver_SuperviseDrilling_Type = AccessTools.TypeByName("Rimefeller.JobDriver_SuperviseDrilling");
if (JobDriver_SuperviseDrilling_Type != null)
{
	var MakeNewToils = AccessTools.Method(JobDriver_SuperviseDrilling_Type, "MakeNewToils");
	if (MakeNewToils != null)
	{
		ha.Unpatch(MakeNewToils, HarmonyPatchType.All, "develww");
		ha.Patch(MakeNewToils, prefix: new HarmonyMethod(AccessTools.Method(typeof(__Class), "MakeNewToilsRfDrillPrefix")));
		Log.Warning("Patched!");
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


// REPAIR //
public static FieldInfo _ticksToNextRepair = AccessTools.Field(typeof(JobDriver_Repair), "ticksToNextRepair");
public static IEnumerable<Toil> MakeNewToilsRepair(JobDriver_Repair __instance, int repairInTick = 1)
{
	__instance.FailOnDespawnedNullOrForbidden(TargetIndex.A);
	yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
	Toil repair = new Toil();

	Thing TargetThingA = Traverse.Create(__instance).Property("TargetThingA").GetValue<Thing>();

	repair.initAction = () => _ticksToNextRepair.SetValue(__instance, 80f);
	repair.tickAction = () =>
	{
		Pawn actor = repair.actor;
		actor.skills.Learn(SkillDefOf.Construction, 0.05f);
		float statValue = actor.GetStatValue(StatDefOf.ConstructionSpeed);
		float ticksToNextRepair = (float) _ticksToNextRepair.GetValue(__instance) - statValue;
		_ticksToNextRepair.SetValue(__instance, ticksToNextRepair); // this.ticksToNextRepair -= statValue;
		if (ticksToNextRepair <= 0f)
		{
			ticksToNextRepair += 20f;
			_ticksToNextRepair.SetValue(__instance, ticksToNextRepair);

			//TargetThingA.HitPoints++;
			TargetThingA.HitPoints = TargetThingA.HitPoints + repairInTick;
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

public static bool MakeNewToils(JobDriver_Repair __instance, ref IEnumerable<Toil> __result)
{
	__result = MakeNewToilsRepair(__instance, 100);
	return false;
}

Type JobDriver_Repair_Type = AccessTools.TypeByName("RimWorld.JobDriver_Repair");
if (JobDriver_Repair_Type != null)
{
	var MakeNewToils = AccessTools.Method(JobDriver_Repair_Type, "MakeNewToils");
	if (MakeNewToils != null)
	{
		ha.Unpatch(MakeNewToils, HarmonyPatchType.All, "develww");
		ha.Patch(MakeNewToils, prefix: new HarmonyMethod(AccessTools.Method(typeof(__Class), "MakeNewToils")));
		Log.Warning("Patched!");
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