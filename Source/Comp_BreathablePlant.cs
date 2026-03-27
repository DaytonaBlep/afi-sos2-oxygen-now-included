using RimWorld;
using UnityEngine;
using Verse;

namespace OxygenNowIncluded
{
    public class Comp_BreathablePlant : ThingComp
    {
        public CompProperties_BreathablePlant Props => (CompProperties_BreathablePlant)props;
        private int lastProcessTick;
        private int lastTooltipUpdate;
        private bool isInSpaceCache = false;
        private int lastSpaceCheck;
        private float cachedOxygenPercent = 0f;

        public bool IsProducingOxygen
        {
            get
            {
                if (parent is Plant plant)
                {
                    if (plant.Destroyed)
                        return false;

                    float lightLevel = parent.Map.glowGrid.GroundGlowAt(parent.Position);
                    if (lightLevel < 0.2f)
                        return false;

                    float temp = parent.Position.GetTemperature(parent.Map);
                    if (temp < Props.minTemperature || temp > Props.maxTemperature)
                        return false;

                    return true;
                }
                return false;
            }
        }

        public float CurrentOxygenProduction
        {
            get
            {
                if (!IsProducingOxygen) return 0f;

                float lightLevel = parent.Map.glowGrid.GroundGlowAt(parent.Position);
                float lightFactor = Mathf.Clamp01((lightLevel - 0.2f) / 0.8f);

                return Props.oxygenProductionPerDay * lightFactor;
            }
        }

        private bool IsInSpace()
        {
            if (parent.Map == null) return false;

            if (Find.TickManager.TicksGame - lastSpaceCheck > 600) // 10 seconds
            {
                lastSpaceCheck = Find.TickManager.TicksGame;

                if (parent.Map.Parent != null)
                {
                    string parentTypeName = parent.Map.Parent.GetType().Name;
                    isInSpaceCache = parentTypeName == "WorldObjectOrbitingShip" || parentTypeName == "MoonBase";
                }

                if (!isInSpaceCache)
                {
                    var shipMapComp = parent.Map.GetComponent<SaveOurShip2.ShipMapComp>();
                    isInSpaceCache = shipMapComp != null;
                }
            }
            return isInSpaceCache;
        }

        private float GetRoomOxygenPercentage()
        {
            var tracker = parent.Map?.GetComponent<GameComponent_OxygenTracker>();
            if (tracker == null) return 0f;

            var room = parent.GetRoom();
            if (room == null) return 0f;

            return tracker.GetOxygenPercentage(room);
        }

        private void UpdateCachedOxygen()
        {
            if (Find.TickManager.TicksGame - lastTooltipUpdate < 30) return; // 0.5 seconds
            lastTooltipUpdate = Find.TickManager.TicksGame;
            cachedOxygenPercent = GetRoomOxygenPercentage();
        }

        private string GetOxygenStatusString()
        {
            float oxygenPercent = cachedOxygenPercent;

            if (oxygenPercent < 10)
                return $"Breathability: {oxygenPercent:F0}% - Non-Breathable".Colorize(Color.yellow);
            else if (oxygenPercent >= 11 && oxygenPercent <= 90)
                return $"Breathability: {oxygenPercent:F0}% - Depleted".Colorize(Color.gray);
            else if (oxygenPercent >= 91 && oxygenPercent <= 110)
                return $"Breathability: {oxygenPercent:F0}% - Normal".Colorize(Color.white);
            else if (oxygenPercent >= 111)
                return $"Breathability: {oxygenPercent:F0}% - Enriched".Colorize(Color.cyan);

            return $"Breathability: {oxygenPercent:F0}%";
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!IsInSpace()) return;

            if (Find.TickManager.TicksGame - lastProcessTick < 120) return; // 2 seconds
            lastProcessTick = Find.TickManager.TicksGame;

            if (parent.Spawned)
            {
                float oxygenAmount = CurrentOxygenProduction / 60000f * 120;

                if (oxygenAmount > 0.001f)
                {
                    Room room = parent.GetRoom();
                    if (room != null)
                    {
                        var tracker = parent.Map.GetComponent<GameComponent_OxygenTracker>();
                        if (tracker != null)
                        {
                            tracker.RegisterOxygenProduction(room, oxygenAmount);
                        }
                    }
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!IsInSpace())
            {
                return "";
            }

            UpdateCachedOxygen();

            string result = "";

            if (!IsProducingOxygen)
            {
                string reason = "";
                if (parent is Plant plant)
                {
                    if (plant.Destroyed)
                        reason = "plant is destroyed";
                }

                if (string.IsNullOrEmpty(reason))
                {
                    if (parent.Map.glowGrid.GroundGlowAt(parent.Position) < 0.2f)
                        reason = $"insufficient light ({parent.Map.glowGrid.GroundGlowAt(parent.Position):P0}, need 20%)";
                    else if (parent.Position.GetTemperature(parent.Map) < Props.minTemperature)
                        reason = $"too cold ({parent.Position.GetTemperature(parent.Map):F1}°C, need {Props.minTemperature}°C)";
                    else if (parent.Position.GetTemperature(parent.Map) > Props.maxTemperature)
                        reason = $"too hot ({parent.Position.GetTemperature(parent.Map):F1}°C, need {Props.maxTemperature}°C)";
                    else
                        reason = "unknown reason";
                }

                result = $"Not producing oxygen: {reason}\n";
            }
            else
            {
                float production = CurrentOxygenProduction;
                result = $"Producing oxygen: {production:F1} per day (light: {parent.Map.glowGrid.GroundGlowAt(parent.Position):P0})\n";
            }

            result += GetOxygenStatusString();

            return result;
        }
    }
}