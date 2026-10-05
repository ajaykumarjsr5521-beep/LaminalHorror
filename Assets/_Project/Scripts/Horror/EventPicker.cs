using System;
using System.Collections.Generic;

namespace NocturneAnnex.Horror
{
    /// <summary>The picker's view of one authored event. Plain data; the HorrorEvent asset (F-09b) maps onto it.</summary>
    public readonly struct EventCandidate
    {
        public readonly string Id;
        public readonly bool Once;
        public readonly float CooldownSeconds;
        public readonly float MinTension;

        public EventCandidate(string id, bool once, float cooldownSeconds, float minTension)
        {
            Id = id; Once = once; CooldownSeconds = cooldownSeconds; MinTension = minTension;
        }
    }

    /// <summary>
    /// Chooses which authored event may fire now. Skips one-shots that already fired, events still on cooldown and events
    /// that need more tension than there is. Among the rest the one with the highest minimum tension wins, ties go to the
    /// earlier list entry, so the choice is deterministic.
    /// </summary>
    public class EventPicker
    {
        readonly List<EventCandidate> _candidates;
        readonly HashSet<string> _firedOnce = new HashSet<string>();
        readonly Dictionary<string, float> _lastFired = new Dictionary<string, float>();

        public EventPicker(IEnumerable<EventCandidate> candidates)
        {
            _candidates = new List<EventCandidate>(candidates ?? throw new ArgumentNullException(nameof(candidates)));
            var seen = new HashSet<string>();
            foreach (var c in _candidates)
            {
                if (string.IsNullOrEmpty(c.Id)) throw new ArgumentException("Event ids must not be empty.", nameof(candidates));
                if (!seen.Add(c.Id)) throw new ArgumentException($"Duplicate event id '{c.Id}'.", nameof(candidates));
                if (c.CooldownSeconds < 0f) throw new ArgumentException($"Event '{c.Id}' has a negative cooldown.", nameof(candidates));
                if (c.MinTension < 0f || c.MinTension > 1f) throw new ArgumentException($"Event '{c.Id}' needs a minimum tension between 0 and 1.", nameof(candidates));
            }
        }

        /// <summary>Picks an event and records it as fired at <paramref name="now"/>. Returns false when nothing is eligible. <paramref name="isAvailable"/> lets the caller skip events that cannot play right now (so they are not marked fired).</summary>
        public bool TryPick(float tension, float now, out string id, Func<string, bool> isAvailable = null)
        {
            id = null;
            float best = -1f;
            foreach (var c in _candidates)
            {
                if (!IsEligible(c, tension, now) || c.MinTension <= best) continue;
                if (isAvailable != null && !isAvailable(c.Id)) continue;   // strict: ties keep the earlier entry
                best = c.MinTension;
                id = c.Id;
            }
            if (id == null) return false;
            _lastFired[id] = now;
            _firedOnce.Add(id);
            return true;
        }

        bool IsEligible(EventCandidate c, float tension, float now)
        {
            if (tension < c.MinTension) return false;
            if (c.Once && _firedOnce.Contains(c.Id)) return false;
            if (_lastFired.TryGetValue(c.Id, out var last) && now - last < c.CooldownSeconds) return false;
            return true;
        }

        /// <summary>Forgets cooldown timestamps, for when the game clock restarts (new run, respawn). Fired one-shots stay fired.</summary>
        public void ClearCooldowns() => _lastFired.Clear();

        /// <summary>Forgets which one-shots fired, for a brand new run.</summary>
        public void ClearFiredOnce()
        {
            _firedOnce.Clear();
            _lastFired.Clear();
        }

        /// <summary>Ids of one-shot events that already fired, for saving.</summary>
        public string[] SnapshotFiredOnce()
        {
            var ids = new List<string>();
            foreach (var c in _candidates) if (c.Once && _firedOnce.Contains(c.Id)) ids.Add(c.Id);
            return ids.ToArray();
        }

        /// <summary>Restores fired one-shots from a save. Unknown ids are ignored.</summary>
        public void RestoreFiredOnce(IEnumerable<string> ids)
        {
            _firedOnce.Clear();
            _lastFired.Clear();
            var known = new HashSet<string>();
            foreach (var c in _candidates) if (c.Once) known.Add(c.Id);
            foreach (var id in ids ?? new string[0]) if (known.Contains(id)) _firedOnce.Add(id);
        }
    }
}
