using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace WorkRebalancer
{
    // better perfomance, but not instant detect hostile pawn
    public static class HostileHandler
    {
        public static bool HostileDetected { get; private set; }

        public static void UpdateHostiles()
        {
            var hostile = AnyHostileActive();
            HostileDetected = hostile != null;

            if (WorkRebalancerMod.Instance.DebugLog && HostileDetected)
            {
                Log.Message($"Hostile detected: {hostile.LabelCap}");
            }
        }

        public static Pawn AnyHostileActive()
        {
            var maps = Find.Maps;

            if (maps != null)
                foreach (var map in maps)
                    if (HostileHandler.AnyHostileActiveThreatTo(map, Faction.OfPlayer, out var threat, false))
                        if (threat.Thing is Pawn p)
                            return p;

            return null;
        }

        // this method not exist in RW 2723. Use local copy
        public static bool AnyHostileActiveThreatTo(Map map, Faction faction, out IAttackTarget threat, bool countDormantPawnsAsHostile = false)
        {
            foreach (IAttackTarget attackTarget in map.attackTargetsCache.TargetsHostileToFaction(faction))
            {
                if (GenHostility.IsActiveThreatTo(attackTarget, faction))
                {
                    threat = attackTarget;
                    return true;
                }
                Pawn pawn;
                if (countDormantPawnsAsHostile && attackTarget.Thing.HostileTo(faction) && !attackTarget.Thing.Fogged() && !attackTarget.ThreatDisabled(null) && (pawn = (attackTarget.Thing as Pawn)) != null)
                {
                    CompCanBeDormant comp = pawn.GetComp<CompCanBeDormant>();
                    if (comp != null && !comp.Awake)
                    {
                        threat = attackTarget;
                        return true;
                    }
                }
            }
            threat = null;
            return false;
        }
    }

    /// <summary>
    /// Hostile spawn checker, instant detect. Hooked spawn things
    /// </summary>
    //[HarmonyPatch(typeof(GenSpawn), nameof(GenSpawn.Spawn), new [] {typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool)})]
    public static class HostileHandler_SpawnCheck
    {
        private const int UPDATE_HOSTILE_LONG = 2;
        private static int longUpdateCounter;

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

            if (!HostileDetected/* && longUpdateCounter % UPDATE_HOSTILE_LONG == 0*/)
                UpdateHostilesLong();

            longUpdateCounter++;

            HostileDetected = _hostilePawnsCached.Any(p => p.IsHostilePawn());

            if (WorkRebalancerMod.Instance.DebugLog && HostileDetected)
            {
                Log.Message($"Hostile detected count: {_hostilePawnsCached.Count(p => p.IsHostilePawn())}. List count: {_hostilePawnsCached.Count}");
            }
        }

        public static void UpdateHostilesLong()
        {
            var maps = Find.Maps;

            if (maps == null)
                return;

            foreach (var map in maps)
                if (HostileHandler.AnyHostileActiveThreatTo(map, Faction.OfPlayer, out var threat))
                    if (threat.Thing is Pawn p)
                        _hostilePawnsCached.Add(p);
        }

        public static bool IsHostilePawn(this Pawn p) => p.HostileTo(Faction.OfPlayer) && !p.Downed && !p.Fogged();
    }
}