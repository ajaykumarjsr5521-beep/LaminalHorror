using UnityEngine;
using NocturneAnnex.Core;

namespace NocturneAnnex.Liminal
{
    /// <summary>
    /// Runtime owner of a level's liminal state: legend progress, entity phase, rules, quiet-time clocks and the music state.
    /// Game time only runs while the game is not blocked (paused or a modal screen open).
    /// </summary>
    public class LiminalDirector : MonoBehaviour
    {
        public const string DefaultEntityId = "guest";

        public string EntityId = DefaultEntityId;
        [Tooltip("Legend entries the player must find before a hunt can start.")] public int MinLegendForHunt = 2;

        public LegendProgress Legend { get; private set; }
        public EntityPhaseMachine Phase { get; private set; }
        public RuleSystem Rules { get; } = new RuleSystem();

        public float SecondsSinceManifestation { get; private set; } = float.PositiveInfinity;
        public float SecondsSinceAftermath { get; private set; } = float.PositiveInfinity;
        public MusicState CurrentMusicState { get; private set; } = MusicState.Exploration;

        /// <summary>Set by the level: distance to the entity in metres, or infinity. Set by the level: whether the player stands at a vista.</summary>
        public float EntityDistance = float.PositiveInfinity;
        public bool AtVista;
        /// <summary>0 to 1, from the tension director when one is present.</summary>
        public float Tension;

        public bool Blocked => ModalGate.IsOpen || Time.timeScale <= 0f;

        void Awake() => Build();

        /// <summary>Creates a fresh state. Safe to call again for a new run.</summary>
        public void Build()
        {
            Legend = new LegendProgress();
            Phase = new EntityPhaseMachine(Legend, MinLegendForHunt);
            Phase.PhaseEntered += OnPhaseEntered;
            SecondsSinceManifestation = float.PositiveInfinity;
            SecondsSinceAftermath = float.PositiveInfinity;
        }

        void OnPhaseEntered(EntityPhase phase)
        {
            if (phase == EntityPhase.Aftermath) SecondsSinceAftermath = 0f;
        }

        /// <summary>The player read a legend entry. Counts once per entry; gives evidence points the first time.</summary>
        public bool ReadLegend(string entryId, int evidencePoints)
        {
            if (!Legend.Found(entryId)) return false;
            Phase.AddEvidence(evidencePoints);
            Phase.Advance();   // the legend count may have just opened the gate to Hunt
            return true;
        }

        public bool CanManifest() => QuietTimePolicy.CanManifest(Phase.Phase, SecondsSinceManifestation, SecondsSinceAftermath, Blocked);

        /// <summary>A manifestation just happened; the quiet clock restarts.</summary>
        public void NoteManifested() => SecondsSinceManifestation = 0f;

        void Update() => Tick(Time.deltaTime, Blocked);

        /// <summary>One step of game time. Public so tests can simulate long play without waiting.</summary>
        public void Tick(float deltaSeconds, bool blocked)
        {
            if (!blocked)
            {
                SecondsSinceManifestation += deltaSeconds;
                if (Phase.Phase == EntityPhase.Aftermath) SecondsSinceAftermath += deltaSeconds;
            }
            CurrentMusicState = MusicStateSelector.Select(new ExperienceState
            {
                Phase = Phase.Phase,
                Tension = Tension,
                EntityDistance = EntityDistance,
                SecondsSinceAftermath = SecondsSinceAftermath,
                AtVista = AtVista,
            });
        }
    }
}
