using System;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace OxygenNowIncluded
{
    [StaticConstructorOnStartup]
    public class OxygenNowIncludedMod : Mod
    {
        public static OxygenNowIncludedMod Instance;
        public static Settings settings;

        public OxygenNowIncludedMod(ModContentPack content) : base(content)
        {
            Instance = this;
            settings = GetSettings<Settings>();

            Log.Message("OxygenNowIncluded initialized");

            var harmony = new Harmony("OxygenNowIncluded.Mod");
            harmony.PatchAll();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
        }

        public override string SettingsCategory()
        {
            return "Save Our Ship 2 - Oxygen Now Included";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.Label("Plant oxygen production (O2 per cell per day): " + settings.plantOxygenPerDay.ToString("F2"));
            settings.plantOxygenPerDay = listing.Slider(settings.plantOxygenPerDay, 0.1f, 20f);

            listing.Label("Ventilation transfer rate (% per oxygen cycle update): " + settings.ventilationTransferRate.ToString("F2"));
            settings.ventilationTransferRate = listing.Slider(settings.ventilationTransferRate, 0.01f, 1f);

            listing.Label("Leak rate per hole (O2 per oxygen cycle update): " + settings.leakRatePerHole.ToString("F2"));
            settings.leakRatePerHole = listing.Slider(settings.leakRatePerHole, 0.1f, 10f);

            listing.Label("Leak rate per vent in vacuum (O2 per oxygen cycle update): " + settings.leakRatePerVentInVacuum.ToString("F2"));
            settings.leakRatePerVentInVacuum = listing.Slider(settings.leakRatePerVentInVacuum, 0.5f, 20f);

            listing.Label("Oxygen consumption per body size (O2 per oxygen cycle update): " + settings.oxygenConsumptionPerBodySize.ToString("F2"));
            settings.oxygenConsumptionPerBodySize = listing.Slider(settings.oxygenConsumptionPerBodySize, 0.1f, 10f);

            listing.Label("Athmosphere degradation rate (O2 per cell per day): " + settings.passiveDegradationRate.ToString("F2"));
            settings.passiveDegradationRate = listing.Slider(settings.passiveDegradationRate, 0f, 10f);

            listing.CheckboxLabeled("SOS2 ship life support overrides plants", ref settings.sos2LifeSupportOverrides);

            listing.Gap();
            listing.Label("Oxygen qualities:");
            listing.Label("Below 10%: Non-Breathable (SOS2 hypoxia)");
            listing.Label("11-90%: Depleted Atmosphere (-10% consciousness, -10% blood filtration)");
            listing.Label("91-110%: Normal Atmosphere (no effect)");
            listing.Label("111-150%: Enriched Atmosphere (+10% consciousness, +15% movement, +10% work, +5% blood filtration)");
			listing.Label("If Hypoxia Resistance stat is 100% or higher, it neutralizes ALL atmospheric effects for pawn.");

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }

    public class Settings : ModSettings
    {
        public float plantOxygenPerDay = 1f;
        public float ventilationTransferRate = 0.1f;
        public float leakRatePerHole = 2f;
        public float leakRatePerVentInVacuum = 5f;
        public float oxygenConsumptionPerBodySize = 2f;
        public float passiveDegradationRate = 0.5f;
        public bool sos2LifeSupportOverrides = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref plantOxygenPerDay, "plantOxygenPerDay", 1f);
            Scribe_Values.Look(ref ventilationTransferRate, "ventilationTransferRate", 0.1f);
            Scribe_Values.Look(ref leakRatePerHole, "leakRatePerHole", 2f);
            Scribe_Values.Look(ref leakRatePerVentInVacuum, "leakRatePerVentInVacuum", 5f);
            Scribe_Values.Look(ref oxygenConsumptionPerBodySize, "oxygenConsumptionPerBodySize", 2f);
            Scribe_Values.Look(ref passiveDegradationRate, "passiveDegradationRate", 0.5f);
            Scribe_Values.Look(ref sos2LifeSupportOverrides, "sos2LifeSupportOverrides", true);
        }
    }
}