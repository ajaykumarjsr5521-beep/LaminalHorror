using System;
using System.Collections.Generic;

namespace NocturneAnnex.Save
{
    [Serializable]
    public class MemoryCount
    {
        public string Key = "";
        public int Count;
    }

    /// <summary>The saved form of EntityMemory. Plain arrays so JsonUtility can write it.</summary>
    [Serializable]
    public class EntityMemoryData
    {
        public MemoryCount[] HidesBySpot = new MemoryCount[0];
        public MemoryCount[] DeathsHidingBySpot = new MemoryCount[0];
        public MemoryCount[] DeathsByCheckpoint = new MemoryCount[0];
        public int Deaths, Hides, Runs, Throws, LinesGiven;
    }

    /// <summary>
    /// What the entity remembers about this run: where the player hid and died, and how they move. It only produces two things for the
    /// entity: a small inspection bonus for a hide spot, and a rare caption line. It never exposes positions, speed or sight, so it cannot
    /// make the entity faster or tell it where the player is. Level (0-5) is a pure function of the counters, so it never falls.
    /// </summary>
    public sealed class EntityMemory
    {
        public const float LineIntervalSeconds = 600f;
        public const int MaxLevel = 5;
        static readonly int[] Thresholds = { 0, 2, 4, 7, 10, 14 };
        static readonly string[] LineKeys = { "memory.again", "memory.remember", "memory.you_again" };

        readonly Dictionary<string, int> _hides = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, int> _deathsHiding = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, int> _deathsAt = new Dictionary<string, int>(StringComparer.Ordinal);
        int _deaths, _hideEvents, _runs, _throws, _linesGiven;
        float _lastLine = float.NegativeInfinity;

        public int Deaths => _deaths;
        public int HideEvents => _hideEvents;
        public int Runs => _runs;
        public int Throws => _throws;

        /// <summary>One hide in a spot (whether or not it was found).</summary>
        public void RecordHide(string spot)
        {
            _hideEvents++;
            if (!string.IsNullOrEmpty(spot)) Bump(_hides, spot);
        }

        public void RecordDeath(DeathRecord record)
        {
            _deaths++;
            if (record == null) return;
            if (!string.IsNullOrEmpty(record.Checkpoint)) Bump(_deathsAt, record.Checkpoint);
            if (!string.IsNullOrEmpty(record.HidingSpot)) Bump(_deathsHiding, record.HidingSpot);
        }

        public void RecordRun() => _runs++;
        public void RecordThrow() => _throws++;

        public int Level
        {
            get
            {
                int points = _deaths * 2 + _hideEvents + _throws / 5;
                int level = 0;
                for (int i = 1; i < Thresholds.Length; i++) if (points >= Thresholds[i]) level = i;
                return level;
            }
        }

        /// <summary>Extra inspection chance for this spot, 0 to cap. Never reaches certainty: the caller's cap is the limit.</summary>
        public float HideBonus(string spot, float cap)
        {
            if (string.IsNullOrEmpty(spot) || cap <= 0f) return 0f;
            float b = 0.04f * Level;
            if (_deathsHiding.TryGetValue(spot, out var d) && d > 0) b += 0.10f;
            if (_hides.TryGetValue(spot, out var h)) b += 0.02f * h;
            return Math.Min(cap, b);
        }

        /// <summary>A caption key, or null. At most one per LineIntervalSeconds, only from level 2, and never when lines are off.</summary>
        public string NextLine(float now, bool enabled)
        {
            if (!enabled || Level < 2 || now - _lastLine < LineIntervalSeconds) return null;
            _lastLine = now;
            return LineKeys[_linesGiven++ % LineKeys.Length];
        }

        public void Clear()
        {
            _hides.Clear(); _deathsHiding.Clear(); _deathsAt.Clear();
            _deaths = _hideEvents = _runs = _throws = _linesGiven = 0;
            _lastLine = float.NegativeInfinity;
        }

        public EntityMemoryData Snapshot() => new EntityMemoryData
        {
            HidesBySpot = ToArray(_hides), DeathsHidingBySpot = ToArray(_deathsHiding), DeathsByCheckpoint = ToArray(_deathsAt),
            Deaths = _deaths, Hides = _hideEvents, Runs = _runs, Throws = _throws, LinesGiven = _linesGiven
        };

        /// <summary>Restores a snapshot exactly. Null (older saves) means an empty memory.</summary>
        public void Restore(EntityMemoryData data)
        {
            Clear();
            if (data == null) return;
            Fill(_hides, data.HidesBySpot); Fill(_deathsHiding, data.DeathsHidingBySpot); Fill(_deathsAt, data.DeathsByCheckpoint);
            _deaths = Math.Max(0, data.Deaths); _hideEvents = Math.Max(0, data.Hides); _runs = Math.Max(0, data.Runs);
            _throws = Math.Max(0, data.Throws); _linesGiven = Math.Max(0, data.LinesGiven);
        }

        static void Bump(Dictionary<string, int> d, string key) => d[key] = (d.TryGetValue(key, out var n) ? n : 0) + 1;

        static MemoryCount[] ToArray(Dictionary<string, int> d)
        {
            var keys = new List<string>(d.Keys);
            keys.Sort(StringComparer.Ordinal);   // stable order so a snapshot of the same memory is identical
            var a = new MemoryCount[keys.Count];
            for (int i = 0; i < a.Length; i++) a[i] = new MemoryCount { Key = keys[i], Count = d[keys[i]] };
            return a;
        }

        static void Fill(Dictionary<string, int> d, MemoryCount[] src)
        {
            if (src == null) return;
            foreach (var m in src) if (m != null && !string.IsNullOrEmpty(m.Key) && m.Count > 0) d[m.Key] = m.Count;
        }
    }
}
