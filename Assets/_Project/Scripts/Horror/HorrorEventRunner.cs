using System;
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
        readonly Dictionary<string, string> _catalogueToLocalId = new Dictionary<string, string>(StringComparer.Ordinal);
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
            _catalogueToLocalId.Clear();
            var candidates = new List<EventCandidate>();
            foreach (var spot in Spots)
            {
                if (spot == null || spot.Event == null) { Debug.LogError("HorrorEventRunner: a spot has no event asset; skipped.", this); continue; }
                if (string.IsNullOrEmpty(spot.Event.Id)) { Debug.LogError($"HorrorEventRunner: event asset '{spot.Event.name}' has an empty Id; skipped.", spot); continue; }
                if (_byId.ContainsKey(spot.Event.Id)) { Debug.LogError($"HorrorEventRunner: duplicate event id '{spot.Event.Id}'; second one skipped.", spot); continue; }
                _byId[spot.Event.Id] = spot;
                _catalogueToLocalId[CatalogueName(spot.Event.Id)] = spot.Event.Id;
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
            if (!Picker.TryPick(Director.Tension, GameTime, out var id, IsSpotFree)) return;
            Director.TryTakeEvent();
            Fire(id);
        }

        bool IsSpotFree(string id) => _byId.TryGetValue(id, out var spot) && !spot.IsPlaying;

        /// <summary>Plays the local event that backs a server catalogue name (e.g. LIGHT_FLICKER). False if none maps to it or its spot is busy.</summary>
        public bool FireCatalogue(string catalogueName) =>
            _catalogueToLocalId.TryGetValue(catalogueName ?? "", out var localId) && Fire(localId);

        /// <summary>Plays an event now, bypassing pacing. For tests and the debug overlay. False if the id is unknown or its spot is busy.</summary>
        public bool Fire(string id)
        {
            if (!_byId.TryGetValue(id, out var spot)) { Debug.LogError($"HorrorEventRunner: unknown event id '{id}'.", this); return false; }
            if (!spot.Play(_budget, Shake)) return false;
            LastEventId = id;
            EventFired?.Invoke(id);
            return true;
        }

        /// <summary>
        /// Start of a level run. A fresh run forgets fired one-shots; either way the scene objects go back to their load state,
        /// then the lasting results of one-shots that already fired (restored from a save) are re-applied.
        /// </summary>
        public void ResetRun(bool freshRun)
        {
            if (freshRun) Picker.ClearFiredOnce();
            foreach (var spot in Spots) if (spot != null) spot.ResetWorld();
            foreach (var id in Picker.SnapshotFiredOnce()) if (_byId.TryGetValue(id, out var s)) s.ApplyPersistent();
            ResetPacing();
        }

        /// <summary>Fresh run or respawn: no leftover tension.</summary>
        public void ResetPacing()
        {
            Director.Reset();
            Picker.ClearCooldowns();   // cooldown times belong to the old clock
            GameTime = 0f;
        }

        static string CatalogueName(string id)
        {
            switch (id)
            {
                case "flicker_hall": return "LIGHT_FLICKER";
                case "whisper_stacks": return "DISTANT_BREATHING";
                case "door_slam_stacks": return "DOOR_MOVEMENT";
                case "prop_shift_counter": return "OBJECT_FALL";
                case "shadow_hall": return "SHADOW_EVENT";
                case "misfile_alcove": return "FALSE_ENTITY_SIGHTING";
                default: return id;
            }
        }
    }
}
