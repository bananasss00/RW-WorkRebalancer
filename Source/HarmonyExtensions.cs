using System.Reflection;
using Harmony;

namespace WorkRebalancer
{
    public static class HarmonyExtensions
    {
        public static bool PatchPrefix(this HarmonyInstance h, string typeColonMethodnameFrom, MethodInfo to, int priority = -1)
        {
            MethodInfo m = AccessTools.Method(typeColonMethodnameFrom);
            if (m == null) return false;
            h.Patch(m, prefix: new HarmonyMethod(to) { prioritiy = priority });
            return true;
        }

        public static bool PatchPostfix(this HarmonyInstance h, string typeColonMethodnameFrom, MethodInfo to, int priority = -1)
        {
            MethodInfo m = AccessTools.Method(typeColonMethodnameFrom);
            if (m == null) return false;
            h.Patch(m, postfix: new HarmonyMethod(to) { prioritiy = priority });
            return true;
        }
    }
}