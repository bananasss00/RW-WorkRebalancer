using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace WorkRebalancer
{
    public static class Utils
    {
        private static bool? _ufActive = null;

        public static bool UF_Active => _ufActive ??= AccessTools.TypeByName("UniversalFermenterSK.RecipeDef_UF") != null;

        [Obsolete]
        public static bool HostileExistsOnMaps()
        {
            var maps = Find.Maps;
            if (maps == null)
                return false;

            foreach (var map in maps)
            {
                foreach (var thing in map.listerThings.AllThings)
                {
                    if (thing.IsHostileThing())
                    {
                        if (WorkRebalancerMod.Instance.DebugLog)
                        {
                            Log.Message($"Hostile detected: {thing.LabelCap}");
                        }
                        return true;
                    }

                    //if (thing is IActiveDropPod dropPod && (dropPod.Contents?.GetDirectlyHeldThings()?.Any ?? false))
                    //{
                    //    foreach (var thing2 in dropPod.Contents.GetDirectlyHeldThings())
                    //    {
                    //        if (thing2.IsHostileThing(false))
                    //        {
                    //            if (WorkRebalancerMod.Instance.DebugLog)
                    //            {
                    //                Log.Message($"Hostile detected(drop pod): {thing.LabelCap}");
                    //            }
                    //            return true;
                    //        }
                    //    }
                    //}
                }
            }

            return false;
        }

        [Obsolete]
        public static bool IsHostileThing(this Thing thing, bool checkFogged = true) // skip hostile buildings and downed pawns
        {
            Pawn p = thing as Pawn;
            return p != null && p.HostileTo(Faction.OfPlayer) && !p.Downed && (!checkFogged || !p.Fogged());
            //return thing.HostileTo(Faction.OfPlayer) && thing.def.building == null && (p == null || !p.Downed) && (!checkFogged || !thing.Fogged());
        }
    }
}