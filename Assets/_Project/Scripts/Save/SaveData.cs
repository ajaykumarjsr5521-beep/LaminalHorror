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
        public string SavedAtUtc = "";
    }
}
