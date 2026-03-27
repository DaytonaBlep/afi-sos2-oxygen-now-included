using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace OxygenNowIncluded
{
    public class RoomOxygenData : IExposable
    {
        private Room room;
        private float currentOxygen;
        private int lastVolumeUpdate;
        private int roomVolume;
        private bool hasSOS2LifeSupport;
        private int lastLifeSupportCheck;
        private int shipIndex = -1;
        private int lastProcessTick;

        private const float OXYGEN_PER_CELL = 100f; // 100 oxygen points can be stored on 1 cell
        private const float MAX_OXYGEN_PERCENT = 1.5f; // 100% oxygen points = 150% Brethability

        private static StatDef hypoxiaResistanceStat;
        private static ThingDef shipLifeSupportDef;

        public Room Room => room;
        public float CurrentOxygen => currentOxygen;

        public float OxygenPercentage
        {
            get
            {
                if (roomVolume <= 0) UpdateRoomVolume();
                if (roomVolume <= 0) return 0f;
                float maxOxygen = roomVolume * OXYGEN_PER_CELL;
                return (currentOxygen / maxOxygen) * 100f;
            }
        }

        public RoomOxygenData() { }

        public RoomOxygenData(Room room)
        {
            this.room = room;
            this.currentOxygen = 0;
            UpdateRoomVolume();
            hasSOS2LifeSupport = false;
            lastProcessTick = Find.TickManager.TicksGame;
        }

        public void SetRoom(Room newRoom)
        {
            room = newRoom;
            UpdateRoomVolume();
        }

        public void SetShipIndex(int index)
        {
            shipIndex = index;
        }

        private static StatDef HypoxiaResistanceStat
        {
            get
            {
                if (hypoxiaResistanceStat == null)
                {
                    hypoxiaResistanceStat = DefDatabase<StatDef>.GetNamed("HypoxiaResistance", false);
                    if (hypoxiaResistanceStat == null)
                    {
                        hypoxiaResistanceStat = new StatDef
                        {
                            defName = "HypoxiaResistance",
                            defaultBaseValue = 0f
                        };
                    }
                }
                return hypoxiaResistanceStat;
            }
        }

        private static ThingDef ShipLifeSupportDef
        {
            get
            {
                if (shipLifeSupportDef == null)
                {
                    shipLifeSupportDef = DefDatabase<ThingDef>.GetNamed("Ship_LifeSupport_Small", false);
                    if (shipLifeSupportDef == null)
                    {
                        shipLifeSupportDef = DefDatabase<ThingDef>.GetNamed("Ship_LifeSupport", false);
                    }
                }
                return shipLifeSupportDef;
            }
        }

        private void UpdateRoomVolume()
        {
            if (room?.Cells != null)
            {
                roomVolume = room.Cells.Count();
                lastVolumeUpdate = Find.TickManager.TicksGame;
            }
        }

        public void AddOxygen(float amount)
        {
            if (roomVolume <= 0) UpdateRoomVolume();
            if (roomVolume <= 0) return;

            float maxOxygen = roomVolume * OXYGEN_PER_CELL * MAX_OXYGEN_PERCENT;
            currentOxygen = Mathf.Min(currentOxygen + amount, maxOxygen);
        }

        public void RemoveOxygen(float amount)
        {
            currentOxygen = Mathf.Max(0, currentOxygen - amount);
        }

        public void Update(Map map, GameComponent_OxygenTracker tracker)
        {
            if (room == null) return;

            if (Find.TickManager.TicksGame - lastProcessTick < 120) return;
            lastProcessTick = Find.TickManager.TicksGame;

            if (Find.TickManager.TicksGame - lastVolumeUpdate > 600)
                UpdateRoomVolume();

            if (roomVolume <= 0) return;

            if (Find.TickManager.TicksGame - lastLifeSupportCheck > 600)
            {
                lastLifeSupportCheck = Find.TickManager.TicksGame;
                CheckSOS2LifeSupport(map, tracker);
            }

            if (hasSOS2LifeSupport)
            {
                float normalMaxOxygen = roomVolume * OXYGEN_PER_CELL;
                if (currentOxygen < normalMaxOxygen)
                {
                    currentOxygen = normalMaxOxygen;
                }
                return;
            }

            ApplyPassiveDegradation();
            ApplyLeakThroughHoles(map);
            ApplyLeakThroughVents(map, tracker);
            ApplyOxygenConsumption(map);
            ApplyVentilationEqualization(map, tracker);
            UpdatePawnEffects(map);
        }

        private void ApplyPassiveDegradation()
        {
            if (currentOxygen <= 0) return;

            float degradationRate = OxygenNowIncludedMod.settings.passiveDegradationRate;
            if (degradationRate <= 0) return;

            float degradationAmount = roomVolume * degradationRate * 120 / 60000f;
            degradationAmount = Mathf.Min(degradationAmount, currentOxygen);

            if (degradationAmount > 0.001f)
            {
                RemoveOxygen(degradationAmount);
            }
        }

        private void ApplyLeakThroughHoles(Map map)
        {
            if (currentOxygen <= 0) return;

            int holeCount = 0;
            foreach (var cell in room.Cells)
            {
                if (!cell.Roofed(map))
                {
                    holeCount++;
                }
            }

            if (holeCount > 0)
            {
                float leakRate = OxygenNowIncludedMod.settings.leakRatePerHole;
                float leakAmount = holeCount * leakRate * 120 / 60000f;
                leakAmount = Mathf.Min(leakAmount, currentOxygen);
                RemoveOxygen(leakAmount);
            }
        }

        private void ApplyLeakThroughVents(Map map, GameComponent_OxygenTracker tracker)
        {
            if (currentOxygen <= 0) return;

            int networkId = -1;
            foreach (var ventThing in map.listerThings.AllThings)
            {
                if (ventThing is SaveOurShip2.Building_ShipVent shipVent)
                {
                    var ventRoom = shipVent.ventTo.GetRoom(map);
                    if (ventRoom == room)
                    {
                        var heatCompVent = ventThing.TryGetComp<SaveOurShip2.CompShipHeat>();
                        if (heatCompVent != null && heatCompVent.myNet != null)
                        {
                            networkId = heatCompVent.myNet.GridID;
                            break;
                        }
                    }
                }
            }

            if (networkId == -1) return;

            int ventsInVacuum = 0;
            var roomsInNetwork = new List<Room>();

            foreach (var ventThing in map.listerThings.AllThings)
            {
                if (ventThing is SaveOurShip2.Building_ShipVent shipVent)
                {
                    var heatCompVent = ventThing.TryGetComp<SaveOurShip2.CompShipHeat>();
                    if (heatCompVent != null && heatCompVent.myNet != null && heatCompVent.myNet.GridID == networkId)
                    {
                        Room ventRoom = shipVent.ventTo.GetRoom(map);
                        if (ventRoom != null && !roomsInNetwork.Contains(ventRoom))
                        {
                            roomsInNetwork.Add(ventRoom);
                        }

                        if (SaveOurShip2.ShipInteriorMod2.ExposedToOutside(ventRoom))
                        {
                            ventsInVacuum++;
                        }
                    }
                }
            }

            if (ventsInVacuum == 0) return;

            float totalVolume = 0;
            var roomDataList = new List<RoomOxygenData>();

            foreach (var netRoom in roomsInNetwork)
            {
                var roomData = tracker.GetRoomData(netRoom);
                if (roomData != null && roomData.GetVolume() > 0)
                {
                    totalVolume += roomData.GetVolume();
                    roomDataList.Add(roomData);
                }
            }

            if (totalVolume <= 0) return;

            float leakRate = OxygenNowIncludedMod.settings.leakRatePerVentInVacuum;
            float totalLeak = ventsInVacuum * leakRate * 120 / 60000f;

            foreach (var roomData in roomDataList)
            {
                float roomLeak = totalLeak * (roomData.GetVolume() / totalVolume);
                roomLeak = Mathf.Min(roomLeak, roomData.currentOxygen);

                if (roomLeak > 0.001f)
                {
                    roomData.RemoveOxygen(roomLeak);
                }
            }
        }

        private void ApplyOxygenConsumption(Map map)
        {
            if (currentOxygen <= 0) return;

            float totalConsumption = 0;

            foreach (var pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (pawn.GetRoom() != room) continue;

                float hypoxiaResistance = 0f;
                if (HypoxiaResistanceStat != null)
                {
                    try
                    {
                        hypoxiaResistance = pawn.GetStatValue(HypoxiaResistanceStat);
                    }
                    catch (Exception) { }
                }

                if (hypoxiaResistance >= 0.99f) continue;

                float consumptionRate = OxygenNowIncludedMod.settings.oxygenConsumptionPerBodySize;
                float consumption = pawn.BodySize * consumptionRate * 120 / 60000f;
                totalConsumption += consumption;
            }

            if (totalConsumption > 0)
            {
                totalConsumption = Mathf.Min(totalConsumption, currentOxygen);
                RemoveOxygen(totalConsumption);
            }
        }

        private void ApplyVentilationEqualization(Map map, GameComponent_OxygenTracker tracker)
        {
            if (currentOxygen <= 0) return;

            try
            {
                var networkRooms = new Dictionary<int, List<Room>>();

                foreach (var ventThing in map.listerThings.AllThings)
                {
                    if (ventThing is SaveOurShip2.Building_ShipVent shipVent)
                    {
                        var compPower = ventThing.TryGetComp<CompPowerTrader>();
                        bool isPowered = compPower != null && compPower.PowerOn;

                        var heatCompVent = ventThing.TryGetComp<SaveOurShip2.CompShipHeat>();
                        int networkId = -1;

                        if (heatCompVent != null && heatCompVent.myNet != null)
                        {
                            networkId = heatCompVent.myNet.GridID;
                        }

                        Room ventRoom = shipVent.ventTo.GetRoom(map);

                        if (isPowered && networkId != -1 && ventRoom != null)
                        {
                            if (!networkRooms.ContainsKey(networkId))
                                networkRooms[networkId] = new List<Room>();

                            if (!networkRooms[networkId].Contains(ventRoom))
                                networkRooms[networkId].Add(ventRoom);
                        }
                    }
                }

                if (networkRooms.Count == 0) return;

                float transferRate = OxygenNowIncludedMod.settings.ventilationTransferRate;

                foreach (var network in networkRooms.Values)
                {
                    if (network.Count <= 1) continue;

                    float totalOxygen = 0;
                    float totalVolume = 0;
                    var roomDataList = new List<RoomOxygenData>();

                    foreach (var netRoom in network)
                    {
                        var roomData = tracker.GetRoomData(netRoom);
                        if (roomData != null && roomData.GetVolume() > 0)
                        {
                            totalOxygen += roomData.currentOxygen;
                            totalVolume += roomData.GetVolume();
                            roomDataList.Add(roomData);
                        }
                    }

                    if (totalVolume <= 0) continue;

                    float targetOxygenPerVolume = totalOxygen / totalVolume;

                    foreach (var roomData in roomDataList)
                    {
                        float currentPerVolume = roomData.currentOxygen / roomData.GetVolume();
                        float diff = targetOxygenPerVolume - currentPerVolume;
                        float transfer = diff * roomData.GetVolume() * transferRate;

                        if (Mathf.Abs(transfer) > 0.01f)
                        {
                            roomData.AddOxygen(transfer);
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private void CheckSOS2LifeSupport(Map map, GameComponent_OxygenTracker tracker)
        {
            bool oldState = hasSOS2LifeSupport;
            hasSOS2LifeSupport = false;

            int currentShipIndex = shipIndex;
            if (currentShipIndex == -1)
            {
                var mapComp = map.GetComponent<SaveOurShip2.ShipMapComp>();
                if (mapComp != null && room.Cells.Count() > 0)
                {
                    currentShipIndex = mapComp.ShipIndexOnVec(room.Cells.First());
                    shipIndex = currentShipIndex;
                }
            }

            if (currentShipIndex == -1) return;

            var shipMapComp = map.GetComponent<SaveOurShip2.ShipMapComp>();
            if (shipMapComp != null && shipMapComp.ShipsOnMap.TryGetValue(currentShipIndex, out var ship))
            {
                foreach (var lifeSupport in ship.LifeSupports)
                {
                    if (lifeSupport != null && lifeSupport.active)
                    {
                        hasSOS2LifeSupport = true;
                        break;
                    }
                }
            }

            if (!oldState && hasSOS2LifeSupport)
            {
                float normalMaxOxygen = roomVolume * OXYGEN_PER_CELL;
                currentOxygen = normalMaxOxygen;
            }
            else if (oldState && !hasSOS2LifeSupport)
            {
                float emergencyOxygen = roomVolume * OXYGEN_PER_CELL * 0.15f;
                currentOxygen = Mathf.Min(currentOxygen, emergencyOxygen);
                if (currentOxygen < emergencyOxygen)
                {
                    currentOxygen = emergencyOxygen;
                }
            }
        }

        private void UpdatePawnEffects(Map map)
        {
            if (room?.Cells == null) return;

            float oxygenPercent = OxygenPercentage;

            foreach (var pawn in map.mapPawns.AllHumanlikeSpawned)
            {
                if (pawn.GetRoom() != room) continue;

                if (oxygenPercent >= 10)
                {
                    RemoveSOS2Hypoxia(pawn);
                }

                float hypoxiaResistance = 0f;
                if (HypoxiaResistanceStat != null)
                {
                    try
                    {
                        hypoxiaResistance = pawn.GetStatValue(HypoxiaResistanceStat);
                    }
                    catch (Exception) { }
                }

                if (hypoxiaResistance >= 0.99f)
                    continue;

                HediffDef targetHediff = null;
                float severity = 0.5f;

                if (oxygenPercent < 10)
                {
                    RemoveOxygenHediffs(pawn);
                    continue;
                }
                else if (oxygenPercent >= 11 && oxygenPercent <= 90)
                {
                    targetHediff = DefDatabase<HediffDef>.GetNamed("DepletedAtmosphere");
                    severity = (90f - oxygenPercent) / 79f;
                    severity *= (1f - hypoxiaResistance);
                }
                else if (oxygenPercent >= 111 && oxygenPercent <= 150)
                {
                    targetHediff = DefDatabase<HediffDef>.GetNamed("EnrichedAtmosphere");
                    severity = (oxygenPercent - 110f) / 40f;
                }
                else if (oxygenPercent >= 91 && oxygenPercent <= 110)
                {
                    RemoveOxygenHediffs(pawn);
                    continue;
                }

                if (targetHediff != null && severity > 0.01f)
                {
                    ApplyHediffToPawn(pawn, targetHediff, severity);
                }
                else
                {
                    RemoveOxygenHediffs(pawn);
                }
            }
        }

        private void RemoveSOS2Hypoxia(Pawn pawn)
        {
            var hypoxiaDef = DefDatabase<HediffDef>.GetNamed("SpaceHypoxia", false);
            if (hypoxiaDef != null)
            {
                var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(hypoxiaDef);
                if (hediff != null)
                    pawn.health.RemoveHediff(hediff);
            }
        }

        private void RemoveOxygenHediffs(Pawn pawn)
        {
            var hediffDefs = new[]
            {
                DefDatabase<HediffDef>.GetNamed("DepletedAtmosphere"),
                DefDatabase<HediffDef>.GetNamed("EnrichedAtmosphere")
            };

            foreach (var def in hediffDefs)
            {
                if (def != null)
                {
                    var hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def);
                    if (hediff != null)
                        pawn.health.RemoveHediff(hediff);
                }
            }
        }

        private void ApplyHediffToPawn(Pawn pawn, HediffDef hediffDef, float severity)
        {
            if (hediffDef == null) return;

            var existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef);

            if (existingHediff != null)
            {
                existingHediff.Severity = severity;
            }
            else
            {
                var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                hediff.Severity = severity;
                pawn.health.AddHediff(hediff);
            }

            var otherDefs = new[]
            {
                DefDatabase<HediffDef>.GetNamed("DepletedAtmosphere"),
                DefDatabase<HediffDef>.GetNamed("EnrichedAtmosphere")
            };

            foreach (var def in otherDefs)
            {
                if (def != null && def != hediffDef)
                {
                    var other = pawn.health.hediffSet.GetFirstHediffOfDef(def);
                    if (other != null)
                        pawn.health.RemoveHediff(other);
                }
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref currentOxygen, "currentOxygen", 0f);
            Scribe_Values.Look(ref roomVolume, "roomVolume", 0);
            Scribe_Values.Look(ref lastVolumeUpdate, "lastVolumeUpdate", 0);
            Scribe_Values.Look(ref hasSOS2LifeSupport, "hasSOS2LifeSupport", false);
            Scribe_Values.Look(ref shipIndex, "shipIndex", -1);
        }

        public int GetVolume()
        {
            if (roomVolume <= 0) UpdateRoomVolume();
            return roomVolume;
        }

        public float GetMaxOxygen()
        {
            if (roomVolume <= 0) UpdateRoomVolume();
            return roomVolume * OXYGEN_PER_CELL * MAX_OXYGEN_PERCENT;
        }
    }
}