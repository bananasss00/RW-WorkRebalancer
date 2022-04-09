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
  //   public class UniversalFermenterSK_DoTicks_Patch
  //   {
  //       public static bool Apply(Harmony h)
  //       {
  //           return h.PatchPrefix("UniversalFermenterSK.Building_UF:DoTicks", typeof(UniversalFermenterSK_DoTicks_Patch).GetMethod("Prefix"));
  //       }
  //
  //       public static void Prefix(ref int ticks)
		// {
  //           if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected &&
  //               HostileHandler.HostileDetected)
  //               return;
  //
		// 	int extraTicks = WorkRebalancerMod.Instance.Prof.UniversalFermenterSKAddX;
  //           if (extraTicks == 0)
  //               return;
  //
		// 	ticks += extraTicks * ticks; // tick, rare, long
		// }
  //   }
}