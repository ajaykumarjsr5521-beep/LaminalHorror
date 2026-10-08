using UnityEngine;
using NocturneAnnex.Entity;
using NocturneAnnex.Player;

namespace NocturneAnnex.Strategy
{
    /// <summary>
    /// Connects the level to the strategic layer. Publishes categories only (spot type, room id, seconds, loudness), never a position or a
    /// specific hide spot, and exposes the executor's two allowed knobs to the entity. With no server everything stays at baseline.
    /// </summary>
    public sealed class StrategyBridge : MonoBehaviour, IStrategyModifiers
    {
        public StrategyRunner Runner;
        public NoiseHub Hub;
        public StalkerAgent Stalker;
        public PlayerMotor Motor;
        public string RoomId = "B1";

        HideSpot[] _spots = new HideSpot[0];
        bool[] _wasOccupied = new bool[0];
        bool _found;
        float _chaseStart = -1f, _moveClock;
        CharacterController _cc;

        public float HideBonus => Runner != null && Runner.Executor != null ? Runner.Executor.HideBonus : 0f;
        public float InvestigationMultiplier => Runner != null && Runner.Executor != null ? Runner.Executor.InvestigationMultiplier : 1f;

        void Start()
        {
            _spots = FindObjectsByType<HideSpot>(FindObjectsSortMode.None);
            _wasOccupied = new bool[_spots.Length];
            if (Motor != null) _cc = Motor.GetComponent<CharacterController>();
            if (Stalker != null) Stalker.Modifiers = this;
            Subscribe();
            Runner.Bus.Publish(new GameEvent(GameEventType.RoomEntered, Time.time, 0f, RoomId));
            Runner.Trigger("NEW_ROOM");
        }

        void OnDestroy() => Unsubscribe();

        void Subscribe()
        {
            if (Hub != null) Hub.Bus.Emitted += OnNoise;
            if (Stalker != null)
            {
                Stalker.CaughtPlayer += OnCaught;
                Stalker.FoundHidingPlayer += OnFoundHiding;
                Stalker.Brain.StateChanged += OnState;
            }
        }

        void Unsubscribe()
        {
            if (Hub != null) Hub.Bus.Emitted -= OnNoise;
            if (Stalker != null)
            {
                Stalker.CaughtPlayer -= OnCaught;
                Stalker.FoundHidingPlayer -= OnFoundHiding;
                Stalker.Brain.StateChanged -= OnState;
            }
        }

        void OnNoise(NoiseEvent e) =>
            Runner.Bus.Publish(new GameEvent(GameEventType.Noise, Time.time, Mathf.Clamp01(e.Radius / 20f), e.Kind.ToString()));

        public void OnCaught()
        {
            Runner.Bus.Publish(new GameEvent(GameEventType.Death, Time.time));
            Runner.Trigger("DEATH");
        }

        public void OnFoundHiding(HideSpot spot)
        {
            _found = true;
            Runner.Bus.Publish(new GameEvent(GameEventType.Hide, Time.time, 0f, SpotType(spot)));
        }

        public void OnState(EntityState from, EntityState to)
        {
            if (to == EntityState.Chase) _chaseStart = Time.time;
            else if (from == EntityState.Chase && _chaseStart >= 0f)
            {
                Runner.Bus.Publish(new GameEvent(GameEventType.ChaseEnded, Time.time, Time.time - _chaseStart));
                _chaseStart = -1f;
                Runner.Trigger("ENCOUNTER_ENDED");
            }
        }

        void Update()
        {
            for (int i = 0; i < _spots.Length; i++)
            {
                bool now = _spots[i].Occupied;
                if (_wasOccupied[i] && !now)
                {
                    if (!_found) Runner.Bus.Publish(new GameEvent(GameEventType.Hide, Time.time, 1f, SpotType(_spots[i])));
                    _found = false;
                }
                _wasOccupied[i] = now;
            }

            _moveClock += Time.deltaTime;
            if (_moveClock >= 1f && Motor != null)
            {
                _moveClock -= 1f;
                float speed = _cc != null ? new Vector2(_cc.velocity.x, _cc.velocity.z).magnitude : 0f;
                var mode = speed < 0.1f ? MoveMode.Still : (Motor.IsCrouched ? MoveMode.Crouch : (Motor.IsSprinting ? MoveMode.Run : MoveMode.Walk));
                Runner.Bus.Publish(new GameEvent(GameEventType.Move, Time.time, 1f, null, mode));
            }
        }

        static string SpotType(HideSpot spot) => spot.name.Split('_')[0].ToLowerInvariant();
    }
}
