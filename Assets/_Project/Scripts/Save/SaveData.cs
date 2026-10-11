using System;

namespace NocturneAnnex.Save
{
    /// <summary>Everything persisted for the single save slot. Plain data, JsonUtility-friendly.</summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        // Deliberately 0 by default: a file without a Version field must read as corrupt, not as a valid empty save.
        // SaveSerializer.ToJson stamps CurrentVersion when writing.
        public int Version;
        public string CheckpointId = "";
        public string[] InventoryIds = new string[0];
        public string[] SolvedPuzzleIds = new string[0];
        // Added after the first release of the format. Absent in older saves, which then read as "nothing fired yet", so Version stays 1.
        public string[] FiredEventIds = new string[0];
        // Run state (F-14g). -1 means "not recorded": a full set of lives.
        public int LivesLeft = -1;
        public int DeathCount;
        public DeathRecord[] DeathLog = new DeathRecord[0];
        // Entity memory (F-14i). Absent in older saves: reads as an empty memory.
        public EntityMemoryData Memory = new EntityMemoryData();
        // Room progress (F-14f). Absent in older saves: reads as room 1, nothing changed.
        public RoomVisitsData Rooms = new RoomVisitsData();
        // Run statistics for the ending (F-14l). Absent in older saves: reads as a fresh run.
        public RunStatsData Stats = new RunStatsData();
        public string SavedAtUtc = "";
    }
}
