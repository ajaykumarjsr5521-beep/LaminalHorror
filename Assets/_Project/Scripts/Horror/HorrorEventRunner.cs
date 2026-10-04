using System.Collections.Generic;
using UnityEngine;
using NocturneAnnex.Core;

namespace NocturneAnnex.Horror
{
    /// <summary>
    /// Ticks the tension director, and when it allows an event asks the picker which one, then plays it through its spot.
    /// Treats the game as blocked while a modal screen is open or time is frozen. Bad event data (empty or duplicate id)
    /// is reported and that spot skipped.
    /// </summary>
    public class HorrorEventRunner : MonoBehaviour
    {
        public TensionSettings Settings = new TensionSettings();
        public HorrorEventSpot[] Spots = new HorrorEventSpot[0];
        public SafeZone[] SafeZones = new SafeZone[0];
        public CameraShake Shake;

        public TensionDirector Director { get; private set; }
        public EventPicker Picker { get; private set; }
        public string LastEventId { get; private set; } = "";
        public float GameTime { get; private set; }
        public event System.Action<string> EventFired;

        readonly Dictionary<string, HorrorEventSpot> _byId = new Dictionary<string, HorrorEventSpot>();
        readonly FlashBudget _budget = new FlashBudget();

        public bool StoryMode
        {
            get => Director != null && Director.StoryMode;
            set { if (Director != null) Director.StoryMode = value; _storyMode = value; }
        }
        bool _storyMode;

        void Awake() => Build();

        /// <summary>Builds the director and picker from the current settings and spots. Safe to call again.</summary>
        public void Build()
        {
            _byId.Clear();
            var candidates = new List<EventCandidate>();
            foreach (var spot in Spots)
            {
                if (spot == null || spot.Event == null) { Debug.LogError("HorrorEventRunner: a spot has no event asset; skipped.", this); continue; }
                if (string.IsNullOrEmpty(spot.Event.Id)) { Debug.LogError($"HorrorEventRunner: event asset '{spot.Event.name}' has an empty Id; skipped.", spot); continue; }
                if (_byId.ContainsKey(spot.Event.Id)) { Debug.LogError($"HorrorEventRunner: duplicate event id '{spot.Event.Id}'; second one skipped.", spot); continue; }
                _byId[spot.Event.Id] = spot;
                candidates.Add(spot.Event.ToCandidate());
            }
            Director = new TensionDirector(Settings, _storyMode);
            Picker = new EventPicker(candidates);
            GameTime = 0f;
        }

        public bool Blocked => ModalGate.IsOpen || Time.timeScale <= 0f;

        public bool PlayerInSafeZone
        {
            get
            {
                foreach (var z in SafeZones) if (z != null && z.PlayerInside) return true;
                return false;
            }
        }

        void Update() => Advance(Time.deltaTime, PlayerInSafeZone, Blocked);

        /// <summary>One step of game time. Public so tests can simulate long play without waiting.</summary>
        public void Advance(float deltaSeconds, bool inSafeZone, bool blocked)
        {
            Director.Tick(deltaSeconds, inUnsafeZone: !inSafeZone, blocked: blocked);
            if (!blocked) GameTime += deltaSeconds;
            if (blocked || !Director.CanFire) return;
            if (!Picker.TryPick(Director.Tension, GameTime, out var id)) return;
            Director.TryTakeEvent();
            Fire(id);
        }

        /// <summary>Plays an event now, bypassing pacing. For tests and the debug overlay.</summary>
        public void Fire(string id)
        {
            if (!_byId.TryGetValue(id, out var spot)) { Debug.LogError($"HorrorEventRunner: unknown event id '{id}'.", this); return; }
            LastEventId = id;
            spot.Play(_budget, Shake);
            EventFired?.Invoke(id);
        }

        /// <summary>Fresh run or respawn: no leftover tension.</summary>
        public void ResetPacing()
        {
            Director.Reset();
            Picker.ClearCooldowns();   // cooldown times belong to the old clock
            GameTime = 0f;
        }
    }
}
