using UnityEngine;
using NocturneAnnex.Core;
using NocturneAnnex.Player;
using NocturneAnnex.Save;

namespace NocturneAnnex.Entity
{
    /// <summary>
    /// Feeds EntityMemory from play: every hide, every throw, every second spent running, and each death. When a chase starts it may post
    /// one rare caption line (EntityMemory enforces one per ten minutes; PlayerPrefs "entity.lines" turns them off). Level and bonus only
    /// ever come out of EntityMemory, so nothing here can speed the entity up or tell it where the player is.
    /// </summary>
    public sealed class EntityMemoryRecorder : MonoBehaviour
    {
        public SaveGame Save;
        public StalkerAgent Stalker;
        public PlayerMotor Motor;
        public PlayerThrower Thrower;

        HideSpot[] _spots = new HideSpot[0];
        bool[] _was = new bool[0];
        float _runClock;

        public static bool LinesEnabled => PlayerPrefs.GetInt("entity.lines", 1) == 1;
        public string LastLineKey { get; private set; }

        void Start()
        {
            _spots = FindObjectsByType<HideSpot>(FindObjectsSortMode.None);
            _was = new bool[_spots.Length];
            if (Stalker != null) { Stalker.Memory = Save.Memory; Stalker.Brain.StateChanged += OnState; }
            if (Thrower != null) Thrower.Thrown += OnThrown;
        }

        void OnDestroy()
        {
            if (Stalker != null) { Stalker.Brain.StateChanged -= OnState; }
            if (Thrower != null) Thrower.Thrown -= OnThrown;
        }

        void OnThrown() => Save.Memory.RecordThrow();
        void OnState(EntityState from, EntityState to)
        {
            if (to != EntityState.Chase) return;
            LastLineKey = Save.Memory.NextLine(Time.time, LinesEnabled);
            if (LastLineKey != null) Captions.Post(LastLineKey);
        }

        void Update()
        {
            for (int i = 0; i < _spots.Length; i++)
            {
                bool now = _spots[i].Occupied;
                if (now && !_was[i]) Save.Memory.RecordHide(_spots[i].name);
                _was[i] = now;
            }
            if (Motor != null && Motor.IsSprinting)
            {
                _runClock += Time.deltaTime;
                if (_runClock >= 1f) { _runClock -= 1f; Save.Memory.RecordRun(); }
            }
        }
    }
}
