using RimWorld;
using Verse;

namespace WorkRebalancer
{
    public class Utils
    {
        public static bool HostileExistsOnMaps()
        {
            var maps = Find.Maps;
            if (maps == null)
                return false;

            foreach (var map in maps)
            {
                foreach (var thing in map.listerThings.AllThings)
                {
                    Pawn p = thing as Pawn;
                    if (thing.HostileTo(Faction.OfPlayer) && thing.def.building == null && (p == null || !p.Downed)) // skip hostile buildings and downed pawns
                    {
                        if (WorkRebalancerMod.Instance.DebugLog)
                        {
                            Log.Message($"Hostile detected: {thing.LabelCap}");
                        }
                        return true;
                    }
                }
            }

            return false;
        }
    }
}