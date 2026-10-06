using System;
using System.Collections.Generic;
using System.Linq;

namespace NocturneAnnex.Liminal
{
    public enum AnomalyTrigger { FirstVisit, OnReturn, LookedAway, PhaseEntered }

    /// <summary>An authored anomaly. It must point at the legend entry or rule it relates to; there are no random anomalies.</summary>
    public class AnomalyDefinition
    {
        public string Id = "";
        public string EntityId = "";
        public string EvidenceLinkId = "";            // the legend entry or rule this anomaly supports
        public AnomalyTrigger Trigger = AnomalyTrigger.FirstVisit;
        public EntityPhase MinPhase = EntityPhase.Phenomenon;
        public int EvidencePoints = 1;
        public bool VisibleOnlyWhenObserved;
    }

    /// <summary>Chooses which authored anomaly may happen now. Each fires at most once.</summary>
    public class AnomalyDirector
    {
        readonly List<AnomalyDefinition> _defs;
        readonly HashSet<string> _fired = new HashSet<string>();

        public AnomalyDirector(IEnumerable<AnomalyDefinition> definitions)
        {
            _defs = new List<AnomalyDefinition>(definitions ?? throw new ArgumentNullException(nameof(definitions)));
            var seen = new HashSet<string>();
            foreach (var d in _defs)
            {
                if (string.IsNullOrEmpty(d.Id)) throw new ArgumentException("An anomaly needs an id.", nameof(definitions));
                if (!seen.Add(d.Id)) throw new ArgumentException($"Duplicate anomaly id '{d.Id}'.", nameof(definitions));
                if (string.IsNullOrEmpty(d.EvidenceLinkId)) throw new ArgumentException($"Anomaly '{d.Id}' has no evidence link; anomalies must mean something.", nameof(definitions));
            }
        }

        /// <summary>Returns the first unfired anomaly for this trigger and phase, and marks it fired.</summary>
        public bool TryTrigger(AnomalyTrigger trigger, EntityPhase phase, out AnomalyDefinition anomaly)
        {
            anomaly = _defs.FirstOrDefault(d => d.Trigger == trigger && phase >= d.MinPhase && !_fired.Contains(d.Id));
            if (anomaly == null) return false;
            _fired.Add(anomaly.Id);
            return true;
        }

        public string[] SnapshotFired() => _fired.OrderBy(x => x).ToArray();

        public void RestoreFired(IEnumerable<string> ids)
        {
            _fired.Clear();
            var known = new HashSet<string>(_defs.Select(d => d.Id));
            foreach (var id in ids ?? new string[0]) if (known.Contains(id)) _fired.Add(id);
        }
    }

    /// <summary>
    /// Remembers a few tracked objects per room when the player leaves, to compare when they return. A change is only fair when the
    /// player spent enough time in the room to have seen the earlier state.
    /// </summary>
    public class RoomMemory
    {
        public const float FairDwellSeconds = 30f;

        class Entry { public Dictionary<string, string> State; public float Dwell; }
        readonly Dictionary<string, Entry> _rooms = new Dictionary<string, Entry>();

        public void Record(string room, IDictionary<string, string> state, float dwellSeconds) =>
            _rooms[room] = new Entry { State = new Dictionary<string, string>(state), Dwell = dwellSeconds };

        public bool HasRoom(string room) => _rooms.ContainsKey(room);

        /// <summary>True when the player stayed long enough that a later change can be noticed.</summary>
        public bool FairToChange(string room) => _rooms.TryGetValue(room, out var e) && e.Dwell >= FairDwellSeconds;

        /// <summary>Keys whose state differs from the record (changed, added or missing). Empty for an unknown room.</summary>
        public List<string> Changed(string room, IDictionary<string, string> current)
        {
            var changed = new List<string>();
            if (!_rooms.TryGetValue(room, out var e)) return changed;
            foreach (var kv in e.State)
                if (!current.TryGetValue(kv.Key, out var now) || now != kv.Value) changed.Add(kv.Key);
            foreach (var key in current.Keys)
                if (!e.State.ContainsKey(key)) changed.Add(key);
            return changed;
        }
    }
}
