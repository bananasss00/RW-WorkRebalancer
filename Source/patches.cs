// interface
// -IsEmpty
// -static GetAll (IWorkAmount)

JobDriver_CollectCrushedstone CrushedStone

// quarry //
public static void MinePostfix(ref Toil __result)
{
	__result.defaultDuration = (int)(__result.defaultDuration * 0.01f);
}

Type JobDriver_MineQuarry_Type = AccessTools.TypeByName("Quarry.JobDriver_MineQuarry");
if (JobDriver_MineQuarry_Type != null)
{
	var ResetTicksToPickHit = AccessTools.Method(JobDriver_MineQuarry_Type, "Mine");
	if (ResetTicksToPickHit != null)
	{
		ha.Unpatch(ResetTicksToPickHit, HarmonyPatchType.All, "develww");
		ha.Patch(ResetTicksToPickHit, postfix: new HarmonyMethod(AccessTools.Method(typeof(__Class), "MinePostfix")));
		Log.Warning("Patched!");
	}
}