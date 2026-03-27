using RimWorld;
using UnityEngine;
using Verse;

namespace OxygenNowIncluded
{
    public class Hediff_OxygenEffect : HediffWithComps
    {
        private float cachedOxygenPercent = -1;
        private int lastUpdateTick = 0;

        public override void Tick()
        {
            base.Tick();

            if (pawn.Map == null) return;

            if (Find.TickManager.TicksGame - lastUpdateTick > 120) // 2 seconds
            {
                lastUpdateTick = Find.TickManager.TicksGame;

                var tracker = pawn.Map.GetComponent<GameComponent_OxygenTracker>();
                if (tracker != null)
                {
                    Room room = pawn.GetRoom();
                    if (room != null)
                    {
                        cachedOxygenPercent = tracker.GetOxygenPercentage(room);
                    }
                }
            }

            if (def.defName == "DepletedAtmosphere")
            {
                if (cachedOxygenPercent >= 0)
                {
                    Severity = Mathf.Clamp01((90f - cachedOxygenPercent) / 79f);
                }
                else
                {
                    Severity = 0.5f;
                }
            }
            else if (def.defName == "EnrichedAtmosphere")
            {
                if (cachedOxygenPercent >= 0)
                {
                    Severity = Mathf.Clamp01((cachedOxygenPercent - 110f) / 40f);
                }
                else
                {
                    Severity = 0.5f;
                }
            }
        }

        public override string Label
        {
            get
            {
                string baseLabel = def.label;

                if (cachedOxygenPercent >= 0)
                {
                    if (def.defName == "DepletedAtmosphere")
                    {
                        return $"{baseLabel} ({cachedOxygenPercent:F0}%)";
                    }
                    else if (def.defName == "EnrichedAtmosphere")
                    {
                        return $"{baseLabel} ({cachedOxygenPercent:F0}%)";
                    }
                }

                return baseLabel;
            }
        }

        public override string TipStringExtra
        {
            get
            {
                string baseTip = base.TipStringExtra;

                if (cachedOxygenPercent >= 0)
                {
                    if (def.defName == "DepletedAtmosphere")
                    {
                        string oxygenInfo = $"\n\nAthmosphere quality: {cachedOxygenPercent:F0}%";
                        return baseTip + oxygenInfo;
                    }
                    else if (def.defName == "EnrichedAtmosphere")
                    {
                        string oxygenInfo = $"\n\nAthmosphere quality: {cachedOxygenPercent:F0}%";
                        return baseTip + oxygenInfo;
                    }
                }

                return baseTip;
            }
        }
    }
}