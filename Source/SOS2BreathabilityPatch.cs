using HarmonyLib;
using RimWorld;
using Verse;

namespace OxygenNowIncluded
{
    // The patch modifies the VecHasLS method from SOS2 to consider rooms breathable at above 10% oxygen.
    [HarmonyPatch(typeof(SaveOurShip2.ShipMapComp))]
    [HarmonyPatch("VecHasLS")]
    public static class ShipMapComp_VecHasLS_Patch
    {
        public static void Postfix(SaveOurShip2.ShipMapComp __instance, IntVec3 vec, ref bool __result)
        {
            if (__result) return;

            var tracker = __instance.map.GetComponent<GameComponent_OxygenTracker>();
            if (tracker == null) return;

            var room = vec.GetRoom(__instance.map);
            if (room == null) return;

            float oxygenPercent = tracker.GetOxygenPercentage(room);

            if (oxygenPercent >= 10)
            {
                __result = true;
            }
        }
    }
}