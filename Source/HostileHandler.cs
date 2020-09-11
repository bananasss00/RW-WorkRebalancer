using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkRebalancer
{
    /// <summary>
    /// Hostile spawn checker
    /// </summary>
    [HarmonyPatch(typeof(GenSpawn), nameof(GenSpawn.Spawn), new [] {typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool)})]
    public static class HostileHandler
    {
        private static HashSet<Pawn> _hostilePawnsCached = new HashSet<Pawn>();
        public static bool HostileDetected { get; private set; }

        [HarmonyPostfix]
        private static void GenSpawn_Spawn(Thing newThing, IntVec3 loc, Map map, Rot4 rot, WipeMode wipeMode, bool respawningAfterLoad)
        {
            HandleNewThing(newThing);
        }

        public static void HandleNewThing(Thing t)
        {
            if (t is Pawn pawn && pawn.IsHostilePawn())
            {
                if (WorkRebalancerMod.Instance.Prof.RestoreWhenHostileDetected && WorkRebalancerMod.Instance.SettingsApplied)
                {
                    WorkRebalancerMod.Instance.ApplySettingsDefaults();

                    if (WorkRebalancerMod.Instance.DebugLog)
                        Log.Message($"[WorkRebalancer] [Spawned] Apply default 100% settings");
                }
                _hostilePawnsCached.Add(pawn);
                HostileDetected = true;
            }
        }

        public static void UpdateHostiles()
        {
            _hostilePawnsCached.RemoveWhere(p => !p.Spawned || p.Destroyed || p.Dead);
            HostileDetected = _hostilePawnsCached.Any(p => p.IsHostilePawn());

            if (WorkRebalancerMod.Instance.DebugLog && HostileDetected)
            {
                Log.Message($"Hostile detected count: {_hostilePawnsCached.Count(p => p.IsHostilePawn())}");
            }
        }

        public static bool IsHostilePawn(this Pawn p)
        {
            return p.HostileTo(Faction.OfPlayer) && !p.Downed && !p.Fogged();
        }
    }
}