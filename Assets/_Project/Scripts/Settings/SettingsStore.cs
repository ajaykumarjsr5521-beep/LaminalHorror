using System;
using System.IO;
using UnityEngine;
using NocturneAnnex.Core;

namespace NocturneAnnex.Settings
{
    public enum SettingsLoadStatus { FirstRun, Ok, Corrupt, UnsupportedVersion }

    public readonly struct SettingsLoadResult
    {
        /// <summary>Always usable: defaults when the file is missing, unreadable or from a newer version.</summary>
        public readonly SettingsData Data;
        public readonly SettingsLoadStatus Status;
        /// <summary>Player-facing warning, null unless the file was corrupt or unsupported.</summary>
        public readonly string Warning;

        public SettingsLoadResult(SettingsData data, SettingsLoadStatus status, string warning)
        {
            Data = data;
            Status = status;
            Warning = warning;
        }
    }

    /// <summary>
    /// settings.json beside the save file but independent of it. A bad file never blocks the game: defaults are
    /// returned with an explicit warning, and the bad file is left on disk untouched.
    /// </summary>
    public class SettingsStore
    {
        readonly string _path;
        readonly int _qualityLevelCount;

        public SettingsStore(string path, int qualityLevelCount)
        {
            _path = path;
            _qualityLevelCount = qualityLevelCount;
        }

        public string Path => _path;

        public SettingsLoadResult Load()
        {
            if (!File.Exists(_path))
                return new SettingsLoadResult(SettingsData.CreateDefault(), SettingsLoadStatus.FirstRun, null);

            string json;
            try { json = File.ReadAllText(_path); }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                return Defaulted(SettingsLoadStatus.Corrupt, Loc.Get("settings.corrupt"));
            }

            SettingsData data = null;
            try { data = JsonUtility.FromJson<SettingsData>(json); }
            catch (ArgumentException) { /* treated as corrupt below */ }

            if (data == null || data.Version <= 0)
                return Defaulted(SettingsLoadStatus.Corrupt, Loc.Get("settings.corrupt"));
            if (data.Version > SettingsData.CurrentVersion)
                return Defaulted(SettingsLoadStatus.UnsupportedVersion, Loc.Get("settings.newer"));

            data.Clamp(_qualityLevelCount);
            return new SettingsLoadResult(data, SettingsLoadStatus.Ok, null);
        }

        public WriteResult Write(SettingsData data)
        {
            try
            {
                data.Clamp(_qualityLevelCount);
                data.Version = SettingsData.CurrentVersion;
                AtomicFile.Write(_path, JsonUtility.ToJson(data), written =>
                {
                    var back = JsonUtility.FromJson<SettingsData>(written);
                    return back != null && back.Version == SettingsData.CurrentVersion;
                });
                return new WriteResult(true, null);
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is InvalidDataException)
            {
                return new WriteResult(false, Loc.Format("settings.write_failed", e.Message));
            }
        }

        static SettingsLoadResult Defaulted(SettingsLoadStatus status, string warning) =>
            new SettingsLoadResult(SettingsData.CreateDefault(), status, warning);
    }
}
