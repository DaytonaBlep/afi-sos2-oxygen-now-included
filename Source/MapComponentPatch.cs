using HarmonyLib;
using Verse;

namespace OxygenNowIncluded
{
    [HarmonyPatch(typeof(Map))]
    [HarmonyPatch("MapPostTick")]
    public static class Map_MapPostTick_Patch
    {
        public static void Postfix(Map __instance)
        {
            var tracker = __instance.GetComponent<GameComponent_OxygenTracker>();
            if (tracker != null)
            {
                tracker.TickComponent();
            }
        }
    }

    [HarmonyPatch(typeof(Map))]
    [HarmonyPatch("FinalizeInit")]
    public static class Map_FinalizeInit_Patch
    {
        public static void Postfix(Map __instance)
        {
            if (__instance.GetComponent<GameComponent_OxygenTracker>() == null)
            {
                __instance.components.Add(new GameComponent_OxygenTracker(__instance));
            }
        }
    }
}