using System;
using System.Collections.Generic;

namespace NocturneAnnex.Liminal
{
    public enum EntityPhase { Legend, Rumor, Clue, Phenomenon, Sighting, Evidence, Presence, Hunt, Survival, Aftermath }

    /// <summary>
    /// How far a level's entity has escalated. Evidence points move it up one phase at a time, never skipping, and never into
    /// Hunt before the player has found enough legend entries. Survival and Aftermath are reached only by explicit calls.
    /// </summary>
    public class EntityPhaseMachine
    {
        /// <summary>Evidence needed to enter Rumor, Clue, Phenomenon, Sighting, Evidence, Presence, Hunt.</summary>
        public static readonly int[] DefaultThresholds = { 1, 3, 5, 8, 11, 14, 18 };

        readonly int[] _thresholds;
        readonly LegendProgress _legend;
        readonly int _minLegendForHunt;

        public EntityPhase Phase { get; private set; } = EntityPhase.Legend;
        public int Evidence { get; private set; }

        public event Action<EntityPhase> PhaseEntered;

        public EntityPhaseMachine(LegendProgress legend, int minLegendEntriesForHunt = 2, int[] thresholds = null)
        {
            _legend = legend ?? throw new ArgumentNullException(nameof(legend));
            _thresholds = thresholds ?? DefaultThresholds;
            if (_thresholds.Length != 7) throw new ArgumentException("Seven thresholds are needed (Rumor to Hunt).", nameof(thresholds));
            for (int i = 1; i < _thresholds.Length; i++)
                if (_thresholds[i] <= _thresholds[i - 1]) throw new ArgumentException("Thresholds must rise.", nameof(thresholds));
            _minLegendForHunt = Math.Max(0, minLegendEntriesForHunt);
        }

        /// <summary>Adds evidence points and moves up as far as the thresholds and the legend gate allow. Returns the phases entered.</summary>
        public List<EntityPhase> AddEvidence(int points)
        {
            Evidence += Math.Max(0, points);
            return Advance();
        }

        /// <summary>Re-checks the gate, e.g. after a legend entry was found while evidence was already enough.</summary>
        public List<EntityPhase> Advance()
        {
            var entered = new List<EntityPhase>();
            while (Phase < EntityPhase.Hunt)
            {
                var next = Phase + 1;
                if (Evidence < _thresholds[(int)next - 1]) break;
                if (next == EntityPhase.Hunt && _legend.Count < _minLegendForHunt) break;
                Enter(next);
                entered.Add(next);
            }
            return entered;
        }

        /// <summary>The hunt ended and the player survived. Only valid during Hunt.</summary>
        public void EndHunt()
        {
            if (Phase != EntityPhase.Hunt) throw new InvalidOperationException("EndHunt is only valid during Hunt.");
            Enter(EntityPhase.Survival);
        }

        /// <summary>Survival settles into Aftermath: music stops, the player is left alone.</summary>
        public void Settle()
        {
            if (Phase != EntityPhase.Survival) throw new InvalidOperationException("Settle is only valid during Survival.");
            Enter(EntityPhase.Aftermath);
        }

        public (int phase, int evidence) Snapshot() => ((int)Phase, Evidence);

        /// <summary>Restores a saved state. A value out of range is clamped, never trusted.</summary>
        public void Restore(int phase, int evidence)
        {
            Phase = (EntityPhase)Math.Max(0, Math.Min((int)EntityPhase.Aftermath, phase));
            Evidence = Math.Max(0, evidence);
        }

        void Enter(EntityPhase phase)
        {
            Phase = phase;
            PhaseEntered?.Invoke(phase);
        }
    }
}
