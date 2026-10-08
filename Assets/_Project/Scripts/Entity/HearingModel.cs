using System;
using System.Collections.Generic;
using UnityEngine;

namespace NocturneAnnex.Entity
{
    /// <summary>
    /// What the entity hears. A noise is audible when the listener is inside its radius, the radius halved for every wall
    /// between them. Older noises fade and expire. The strongest unhandled noise wins; ties go to the newer one.
    /// The model knows only noises, never the player, so the entity cannot locate a silent player.
    /// </summary>
    public class HearingModel
    {
        public const float MaxAgeSeconds = 10f;
        public const float WallFactor = 0.5f;

        readonly List<NoiseEvent> _events = new List<NoiseEvent>();
        float _handledUpTo = float.NegativeInfinity;

        public int Count => _events.Count;

        public void Report(NoiseEvent e) => _events.Add(e);

        /// <summary>Marks every noise up to and including this time as dealt with, so it is not heard again.</summary>
        public void MarkHandled(float time) { if (time > _handledUpTo) _handledUpTo = time; }

        public void Clear() { _events.Clear(); _handledUpTo = float.NegativeInfinity; }

        /// <param name="wallsBetween">Number of walls between two points; null means open space.</param>
        public bool TryHear(Vector3 listener, float now, Func<Vector3, Vector3, int> wallsBetween,
                            out NoiseEvent best, out float score)
        {
            _events.RemoveAll(e => now - e.Time > MaxAgeSeconds);
            best = default; score = 0f;
            bool found = false;
            foreach (var e in _events)
            {
                if (e.Time <= _handledUpTo || e.Time > now) continue;
                int walls = wallsBetween == null ? 0 : Math.Max(0, wallsBetween(e.Position, listener));
                float effective = e.Radius * Mathf.Pow(WallFactor, walls);
                float dist = Vector3.Distance(e.Position, listener);
                if (effective <= 0f || dist > effective) continue;
                float s = (1f - dist / effective) * e.Radius * (1f - (now - e.Time) / MaxAgeSeconds);
                if (!found || s > score || (Mathf.Approximately(s, score) && e.Time > best.Time))
                {
                    best = e; score = s; found = true;
                }
            }
            return found;
        }
    }
}
