using System;
using System.Collections.Generic;

namespace NocturneAnnex.Strategy
{
    /// <summary>Anonymous summary of how the player behaves. Ratios and rates only; safe to send to the server.</summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public float RunFrequency, WalkFrequency, CrouchFrequency; // share of moving time, sum to 1 (or 0 with no movement)
        public float NoiseFrequency;     // 0..1, noises per minute / NoisesPerMinuteAtMax
        public float RiskTolerance;      // 0..1, see PlayerBehaviorTracker.Snapshot
        public float AverageChaseSeconds;
        public float PuzzleSeconds;      // average seconds per solved puzzle, 0 when none
        public int DeathCount;
        public int HideCount;
        public float HideSuccessRate;    // 0..1 of hides that were not found
        public SortedDictionary<string, float> HideUsageByType = new SortedDictionary<string, float>();   // share of hides, sums to 1
        public SortedDictionary<string, float> RouteUsage = new SortedDictionary<string, float>();       // share of route uses
        public SortedDictionary<string, float> RoomTimeShare = new SortedDictionary<string, float>();    // share of time per room
    }

    /// <summary>Listens to the GameEventBus and keeps running counts. Pure C#; Snapshot is deterministic for the same event sequence.</summary>
    public sealed class PlayerBehaviorTracker
    {
        public const float NoisesPerMinuteAtMax = 6f;

        float _run, _walk, _crouch, _noises, _chaseTotal, _puzzleTotal;
        int _chases, _puzzles, _deaths, _hides, _hidesSurvived;
        float _firstTime = float.NaN, _lastTime;
        string _room; float _roomSince;
        readonly Dictionary<string, int> _hideTypes = new Dictionary<string, int>();
        readonly Dictionary<string, int> _routes = new Dictionary<string, int>();
        readonly Dictionary<string, float> _roomTime = new Dictionary<string, float>();

        public void Attach(GameEventBus bus) { bus.Published += Record; }
        public void Detach(GameEventBus bus) { bus.Published -= Record; }

        public void Record(GameEvent e)
        {
            if (float.IsNaN(_firstTime)) _firstTime = e.Time;
            if (e.Time > _lastTime) _lastTime = e.Time;
            switch (e.Type)
            {
                case GameEventType.Move:
                    if (e.Value <= 0f) break;
                    if (e.Mode == MoveMode.Run) _run += e.Value;
                    else if (e.Mode == MoveMode.Walk) _walk += e.Value;
                    else if (e.Mode == MoveMode.Crouch) _crouch += e.Value;
                    break;
                case GameEventType.Noise: _noises++; break;
                case GameEventType.Hide:
                    _hides++; if (e.Value >= 0.5f) _hidesSurvived++;
                    Bump(_hideTypes, e.Tag ?? "unknown");
                    break;
                case GameEventType.RouteUsed: Bump(_routes, e.Tag ?? "unknown"); break;
                case GameEventType.RoomEntered:
                    CloseRoom(e.Time); _room = e.Tag; _roomSince = e.Time; break;
                case GameEventType.Death: _deaths++; break;
                case GameEventType.ChaseEnded: _chases++; _chaseTotal += Math.Max(0f, e.Value); break;
                case GameEventType.PuzzleSolved: _puzzles++; _puzzleTotal += Math.Max(0f, e.Value); break;
            }
        }

        public PlayerProfile Snapshot()
        {
            var p = new PlayerProfile();
            float moving = _run + _walk + _crouch;
            if (moving > 0f) { p.RunFrequency = _run / moving; p.WalkFrequency = _walk / moving; p.CrouchFrequency = _crouch / moving; }
            float minutes = Math.Max(1f / 60f, ((float.IsNaN(_firstTime) ? 0f : _lastTime - _firstTime)) / 60f);
            p.NoiseFrequency = Clamp01(_noises / minutes / NoisesPerMinuteAtMax);
            p.DeathCount = _deaths; p.HideCount = _hides;
            p.HideSuccessRate = _hides > 0 ? (float)_hidesSurvived / _hides : 0f;
            p.AverageChaseSeconds = _chases > 0 ? _chaseTotal / _chases : 0f;
            p.PuzzleSeconds = _puzzles > 0 ? _puzzleTotal / _puzzles : 0f;
            // Risk: running and making noise raise it; leaning on hiding to survive lowers it.
            p.RiskTolerance = Clamp01(0.5f * p.RunFrequency + 0.3f * p.NoiseFrequency + 0.2f * (1f - Math.Min(1f, _hides / 5f)));
            Share(_hideTypes, p.HideUsageByType);
            Share(_routes, p.RouteUsage);
            var rt = new Dictionary<string, float>(_roomTime);
            if (_room != null) { float add = Math.Max(0f, _lastTime - _roomSince); rt[_room] = (rt.TryGetValue(_room, out var v) ? v : 0f) + add; }
            float total = 0f; foreach (var kv in rt) total += kv.Value;
            if (total > 0f) foreach (var kv in rt) p.RoomTimeShare[kv.Key] = kv.Value / total;
            return p;
        }

        void CloseRoom(float now)
        {
            if (_room == null) return;
            _roomTime[_room] = (_roomTime.TryGetValue(_room, out var v) ? v : 0f) + Math.Max(0f, now - _roomSince);
        }

        static void Bump(Dictionary<string, int> d, string k) { d[k] = (d.TryGetValue(k, out var v) ? v : 0) + 1; }

        static void Share(Dictionary<string, int> src, SortedDictionary<string, float> dst)
        {
            int total = 0; foreach (var kv in src) total += kv.Value;
            if (total == 0) return;
            foreach (var kv in src) dst[kv.Key] = (float)kv.Value / total;
        }

        static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
