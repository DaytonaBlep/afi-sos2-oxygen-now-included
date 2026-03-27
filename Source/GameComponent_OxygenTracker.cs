using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace OxygenNowIncluded
{
    public class GameComponent_OxygenTracker : MapComponent
    {
        private Dictionary<Room, RoomOxygenData> oxygenData = new Dictionary<Room, RoomOxygenData>();
        private List<Room> roomsToRemove = new List<Room>();
        private List<Comp_BreathablePlant> allPlants = new List<Comp_BreathablePlant>();
        private int lastPlantUpdateTick;
        private int lastProcessTick;
        private bool isSpaceMap = false;
        private List<int> savedRoomIDs = new List<int>();
        private List<RoomOxygenData> savedRoomData = new List<RoomOxygenData>();

        public GameComponent_OxygenTracker(Map map) : base(map)
        {
            if (map != null)
            {
                isSpaceMap = IsMapSpace(map);
            }
        }

        private bool IsMapSpace(Map map)
        {
            if (map == null) return false;

            if (map.Parent != null)
            {
                string parentTypeName = map.Parent.GetType().Name;
                if (parentTypeName == "WorldObjectOrbitingShip" || parentTypeName == "MoonBase")
                {
                    return true;
                }
            }

            var shipMapComp = map.GetComponent<SaveOurShip2.ShipMapComp>();
            if (shipMapComp != null)
            {
                return true;
            }

            return false;
        }

        private bool IsInSpace()
        {
            return isSpaceMap;
        }

        public void TickComponent()
        {
            if (!IsInSpace() || map == null) return;

            if (Find.TickManager.TicksGame - lastProcessTick < 120) return;
            lastProcessTick = Find.TickManager.TicksGame;

            if (Find.TickManager.TicksGame - lastPlantUpdateTick > 600)
            {
                lastPlantUpdateTick = Find.TickManager.TicksGame;
                UpdatePlantList();
            }

            foreach (var plant in allPlants)
            {
                if (plant != null && plant.parent.Spawned && !plant.parent.Destroyed)
                {
                    float oxygenAmount = plant.CurrentOxygenProduction / 60000f * 120;

                    if (oxygenAmount > 0.001f)
                    {
                        Room room = plant.parent.GetRoom();
                        if (room != null)
                        {
                            RegisterOxygenProduction(room, oxygenAmount);
                        }
                    }
                }
            }

            UpdateAllRooms();
        }

        private void UpdatePlantList()
        {
            allPlants.Clear();
            foreach (var thing in map.listerThings.AllThings)
            {
                if (thing is Plant plant && !plant.Destroyed)
                {
                    var comp = plant.TryGetComp<Comp_BreathablePlant>();
                    if (comp != null)
                    {
                        allPlants.Add(comp);
                    }
                }
            }
        }

        private void UpdateAllRooms()
        {
            if (map == null) return;

            var currentRooms = map.regionGrid.AllRooms;

            roomsToRemove.Clear();
            foreach (var room in oxygenData.Keys)
            {
                if (room == null || !currentRooms.Contains(room))
                    roomsToRemove.Add(room);
            }

            foreach (var room in roomsToRemove)
            {
                oxygenData.Remove(room);
            }

            foreach (var room in currentRooms)
            {
                if (!oxygenData.ContainsKey(room))
                {
                    var newData = new RoomOxygenData(room);

                    if (savedRoomData.Count > 0 && savedRoomIDs.Count > 0)
                    {
                        int roomID = GetRoomID(room);
                        int index = savedRoomIDs.IndexOf(roomID);
                        if (index >= 0 && index < savedRoomData.Count && savedRoomData[index] != null)
                        {
                            newData = savedRoomData[index];
                            newData.SetRoom(room);
                            savedRoomIDs.RemoveAt(index);
                            savedRoomData.RemoveAt(index);
                        }
                    }

                    var mapComp = map.GetComponent<SaveOurShip2.ShipMapComp>();
                    if (mapComp != null && room.Cells.Count() > 0)
                    {
                        int shipIndex = mapComp.ShipIndexOnVec(room.Cells.First());
                        newData.SetShipIndex(shipIndex);
                    }

                    oxygenData[room] = newData;
                }

                oxygenData[room].Update(map, this);
            }
        }

        private int GetRoomID(Room room)
        {
            if (room == null || room.Cells.Count() == 0) return -1;
            var firstCell = room.Cells.First();
            return firstCell.x * 100000 + firstCell.z;
        }

        public void RegisterOxygenProduction(Room room, float oxygenAmount)
        {
            if (room == null) return;

            if (!oxygenData.TryGetValue(room, out var data))
            {
                data = new RoomOxygenData(room);
                oxygenData[room] = data;
            }

            data.AddOxygen(oxygenAmount);
        }

        public float GetOxygenPercentage(Room room)
        {
            if (room != null && oxygenData.TryGetValue(room, out var data))
                return data.OxygenPercentage;
            return 0f;
        }

        public RoomOxygenData GetRoomData(Room room)
        {
            if (room != null && oxygenData.TryGetValue(room, out var data))
                return data;
            return null;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref isSpaceMap, "isSpaceMap", false);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                savedRoomIDs.Clear();
                savedRoomData.Clear();

                foreach (var kvp in oxygenData)
                {
                    if (kvp.Key != null && kvp.Value != null)
                    {
                        int roomID = GetRoomID(kvp.Key);
                        if (roomID >= 0)
                        {
                            savedRoomIDs.Add(roomID);
                            savedRoomData.Add(kvp.Value);
                        }
                    }
                }
            }

            Scribe_Collections.Look(ref savedRoomIDs, "savedRoomIDs", LookMode.Value);
            Scribe_Collections.Look(ref savedRoomData, "savedRoomData", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                oxygenData.Clear();
            }
        }
    }
}