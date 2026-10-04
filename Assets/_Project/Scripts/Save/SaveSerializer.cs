using System;
using UnityEngine;

namespace NocturneAnnex.Save
{
    public enum LoadStatus { Ok, Missing, Corrupt, UnsupportedVersion }

    public readonly struct LoadResult
    {
        public readonly LoadStatus Status;
        public readonly SaveData Data;
        public readonly string Message;

        public LoadResult(LoadStatus status, SaveData data, string message)
        {
            Status = status; Data = data; Message = message;
        }

        public bool Ok => Status == LoadStatus.Ok;
    }

    /// <summary>JSON conversion with explicit failure results; never returns half-parsed data.</summary>
    public static class SaveSerializer
    {
        public static string ToJson(SaveData data)
        {
            data.Version = SaveData.CurrentVersion;
            return JsonUtility.ToJson(data);
        }

        public static LoadResult FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new LoadResult(LoadStatus.Corrupt, null, "The save file is empty.");

            SaveData data;
            try { data = JsonUtility.FromJson<SaveData>(json); }
            catch (Exception e)
            {
                return new LoadResult(LoadStatus.Corrupt, null, "The save file could not be read: " + e.Message);
            }

            if (data == null || data.Version <= 0)
                return new LoadResult(LoadStatus.Corrupt, null, "The save file is damaged or incomplete.");
            if (data.Version > SaveData.CurrentVersion)
                return new LoadResult(LoadStatus.UnsupportedVersion, null,
                    $"This save was made by a newer version of the game (save v{data.Version}, game supports v{SaveData.CurrentVersion}).");
            if (data.Version < SaveData.CurrentVersion)
                return new LoadResult(LoadStatus.UnsupportedVersion, null,
                    $"This save is from an older, unsupported version (v{data.Version}).");

            data.InventoryIds ??= new string[0];
            data.SolvedPuzzleIds ??= new string[0];
            data.CheckpointId ??= "";
            return new LoadResult(LoadStatus.Ok, data, null);
        }
    }
}
