using Verse;

namespace OxygenNowIncluded
{
    public class OxygenSettingsDef : Def
    {
        public float baseOxygenPerCell = 100f; // 100 oxygen points can be stored on 1 cell
        public float maxOxygenPercent = 1.5f; // 100% oxygen points = 150% Brethability
        public float leakRatePerHole = 0.5f;
        public float leakRatePerVentInVacuum = 0.1f;
        public float oxygenConsumptionPerBodySize = 1.0f;
        public float ventilationTransferRate = 0.05f;
    }
}