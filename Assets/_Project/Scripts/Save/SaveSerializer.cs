using System;
using UnityEngine;
using NocturneAnnex.Core;

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
                return new LoadResult(LoadStatus.Corrupt, null, Loc.Get("save.empty"));

            SaveData data;
            try { data = JsonUtility.FromJson<SaveData>(json); }
            catch (Exception e)
            {
                return new LoadResult(LoadStatus.Corrupt, null, Loc.Format("save.unreadable", e.Message));
            }

            if (data == null || data.Version <= 0)
                return new LoadResult(LoadStatus.Corrupt, null, Loc.Get("save.damaged"));
            if (data.Version > SaveData.CurrentVersion)
                return new LoadResult(LoadStatus.UnsupportedVersion, null,
                    Loc.Format("save.newer", data.Version, SaveData.CurrentVersion));
            if (data.Version < SaveData.CurrentVersion)
                return new LoadResult(LoadStatus.UnsupportedVersion, null,
                    Loc.Format("save.older", data.Version));

            data.InventoryIds ??= new string[0];
            data.SolvedPuzzleIds ??= new string[0];
            data.CheckpointId ??= "";
            return new LoadResult(LoadStatus.Ok, data, null);
        }
    }
}
