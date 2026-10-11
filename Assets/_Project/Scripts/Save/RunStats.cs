using System;
using System.Collections.Generic;

namespace NocturneAnnex.Save
{
    [Serializable]
    public class RunStatsData
    {
        public float Seconds;
        public string[] Secrets = new string[0];
    }

    /// <summary>What the ending shows about the run (F-14l): play time and secrets found. Plain data, saved with the game and cleared on a new game.</summary>
    public sealed class RunStats
    {
        readonly HashSet<string> _secrets = new HashSet<string>(StringComparer.Ordinal);

        public float Seconds { get; private set; }
        public int SecretCount => _secrets.Count;

        /// <summary>Adds play time. Negative, NaN and infinite values are ignored, so a bad frame cannot corrupt the total.</summary>
        public void AddSeconds(float dt)
        {
            if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0f) return;
            Seconds += dt;
        }

        /// <summary>Records a secret once. Returns true when it was new.</summary>
        public bool AddSecret(string id) => !string.IsNullOrEmpty(id) && _secrets.Add(id);

        public bool HasSecret(string id) => id != null && _secrets.Contains(id);

        public void Clear() { Seconds = 0f; _secrets.Clear(); }

        public RunStatsData Snapshot()
        {
            var list = new List<string>(_secrets);
            list.Sort(StringComparer.Ordinal);
            return new RunStatsData { Seconds = Seconds, Secrets = list.ToArray() };
        }

        /// <summary>Restores a snapshot. Null (older saves) means a fresh run.</summary>
        public void Restore(RunStatsData data)
        {
            Clear();
            if (data == null) return;
            if (!float.IsNaN(data.Seconds) && !float.IsInfinity(data.Seconds) && data.Seconds > 0f) Seconds = data.Seconds;
            if (data.Secrets != null) foreach (var s in data.Secrets) if (!string.IsNullOrEmpty(s)) _secrets.Add(s);
        }
    }
}
