using System.Reflection;
using HarmonyLib;

namespace WorkRebalancer
{
    public static class HarmonyExtensions
    {
        public static bool PatchPrefix(this Harmony h, string typeColonMethodnameFrom, MethodInfo to, int priority = -1)
        {
            MethodInfo m = AccessTools.Method(typeColonMethodnameFrom);
            if (m == null) return false;
            h.Patch(m, prefix: new HarmonyMethod(to) { priority = priority });
            return true;
        }

        public static bool PatchPostfix(this Harmony h, string typeColonMethodnameFrom, MethodInfo to, int priority = -1)
        {
            MethodInfo m = AccessTools.Method(typeColonMethodnameFrom);
            if (m == null) return false;
            h.Patch(m, postfix: new HarmonyMethod(to) { priority = priority });
            return true;
        }
    }
}