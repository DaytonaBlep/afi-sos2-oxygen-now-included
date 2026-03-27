using Verse;

namespace OxygenNowIncluded
{
    public class CompProperties_BreathablePlant : CompProperties
    {
        public float oxygenProductionPerDay = 10f;
        public float minLightLevel = 0.2f;
        public float minTemperature = 12f;
        public float maxTemperature = 42f;

        public CompProperties_BreathablePlant()
        {
            this.compClass = typeof(Comp_BreathablePlant);
        }
    }
}