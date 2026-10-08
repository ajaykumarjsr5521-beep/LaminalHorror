using System;
using UnityEngine;

namespace NocturneAnnex.Entity
{
    public enum EntityState { Patrol, Listen, Investigate, Chase, Search, Cooldown, Watch }

    /// <summary>What the entity perceived this step. The brain never sees the player directly, only these results.</summary>
    public struct BrainInput
    {
        public bool Heard;          // HearingModel produced a noise
        public Vector3 NoisePos;
        public float NoiseScore;
        public bool Sees;           // SightModel.CanSee was true
        public Vector3 SeenPos;
        public bool Arrived;        // reached the current target
        public float Roll;          // 0..1 random value from the caller, keeps the brain deterministic
    }

    /// <summary>
    /// Decision logic of the entity. Calm states (Patrol, Cooldown) always pass through Listen before acting, so the player
    /// can hear the entity stop and turn. Watch lets it stare without attacking. Speeds are returned as multipliers of the
    /// player's walk speed so the entity is always slower than a running player.
    /// </summary>
    public class EntityBrain
    {
        public const float ListenSeconds = 1.2f;
        public const float SearchSeconds = 15f;

        /// <summary>Multiplies the search time. Set from the strategic layer, 1 = baseline.</summary>
        public float SearchScale = 1f;
        public const float ChaseLoseSeconds = 20f;
        public const float CooldownSeconds = 8f;
        public const float WatchSeconds = 6f;
        public const float WatchChance = 0.2f;
        public const float WatchMaxNoiseScore = 6f;   // louder noises are never ignored

        public EntityState State { get; private set; } = EntityState.Patrol;
        public Vector3 Target { get; private set; }
        public bool HasTarget { get; private set; }
        public float StateTime { get; private set; }

        public event Action<EntityState, EntityState> StateChanged;

        Vector3 _pending;
        bool _pendingSeen;
        float _sinceContact;

        /// <summary>Speed multiplier of the player's walk speed. Chase 1.15x walk stays under 0.9x of a run (1.7x walk).</summary>
        public static float SpeedMultiplier(EntityState s)
        {
            switch (s)
            {
                case EntityState.Patrol: return 0.6f;
                case EntityState.Investigate: return 0.8f;
                case EntityState.Search: return 0.7f;
                case EntityState.Chase: return 1.15f;
                default: return 0f;
            }
        }

        public void Step(float dt, BrainInput input)
        {
            StateTime += dt;
            switch (State)
            {
                case EntityState.Patrol:
                case EntityState.Cooldown:
                    if (Perceive(input)) break;
                    if (State == EntityState.Cooldown && StateTime >= CooldownSeconds) Enter(EntityState.Patrol);
                    break;

                case EntityState.Listen:
                    if (input.Sees) { _pending = input.SeenPos; _pendingSeen = true; }
                    else if (input.Heard && !_pendingSeen) _pending = input.NoisePos;
                    if (StateTime >= ListenSeconds) EndListen(input);
                    break;

                case EntityState.Investigate:
                    if (input.Sees) { StartChase(input.SeenPos); break; }
                    if (input.Heard) SetTarget(input.NoisePos);
                    if (input.Arrived) Enter(EntityState.Search);
                    break;

                case EntityState.Search:
                    if (input.Sees) { StartChase(input.SeenPos); break; }
                    if (input.Heard) { Enter(EntityState.Listen); _pending = input.NoisePos; _pendingSeen = false; break; }
                    if (StateTime >= SearchSeconds * SearchScale) Enter(EntityState.Cooldown);
                    break;

                case EntityState.Chase:
                    _sinceContact += dt;
                    if (input.Sees) { SetTarget(input.SeenPos); _sinceContact = 0f; }
                    else if (input.Heard) { SetTarget(input.NoisePos); _sinceContact = 0f; }
                    if (_sinceContact >= ChaseLoseSeconds) Enter(EntityState.Search);
                    break;

                case EntityState.Watch:
                    // Staring never turns into an attack by itself; only a new loud noise restarts the sequence.
                    if (input.Heard && input.NoiseScore > WatchMaxNoiseScore) { Perceive(input); break; }
                    if (StateTime >= WatchSeconds) Enter(EntityState.Cooldown);
                    break;
            }
        }

        bool Perceive(BrainInput input)
        {
            if (!input.Sees && !input.Heard) return false;
            _pendingSeen = input.Sees;
            _pending = input.Sees ? input.SeenPos : input.NoisePos;
            _watchRoll = input.Roll;
            _weakNoise = !input.Sees && input.NoiseScore <= WatchMaxNoiseScore;
            Enter(EntityState.Listen);
            return true;
        }

        float _watchRoll;
        bool _weakNoise;

        void EndListen(BrainInput input)
        {
            if (_pendingSeen && _watchRoll < WatchChance) { Enter(EntityState.Watch); return; }
            if (_pendingSeen) { StartChase(_pending); return; }
            if (_weakNoise && _watchRoll < WatchChance) { Enter(EntityState.Watch); return; }
            SetTarget(_pending);
            Enter(EntityState.Investigate);
        }

        /// <summary>Back to calm patrol, forgetting targets and contact. Used after a death.</summary>
        public void Reset()
        {
            _sinceContact = 0f; _pendingSeen = false; HasTarget = false;
            Enter(EntityState.Patrol);
        }

        void StartChase(Vector3 at) { SetTarget(at); _sinceContact = 0f; Enter(EntityState.Chase); }

        void SetTarget(Vector3 t) { Target = t; HasTarget = true; }

        void Enter(EntityState next)
        {
            var prev = State;
            State = next;
            StateTime = 0f;
            if (next == EntityState.Patrol) HasTarget = false;
            StateChanged?.Invoke(prev, next);
        }
    }
}
