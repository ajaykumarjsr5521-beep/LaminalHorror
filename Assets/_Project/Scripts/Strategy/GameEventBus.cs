using System;

namespace NocturneAnnex.Strategy
{
    public enum GameEventType { Move, Noise, Hide, RouteUsed, RoomEntered, Death, ChaseEnded, PuzzleSolved }
    public enum MoveMode { Walk, Run, Crouch, Still }

    /// <summary>
    /// A fact about the player for the strategic layer. It carries categories only (hide spot TYPE, route GROUP, room id):
    /// never a position or a specific hide spot, so nothing here can leak where the player is hiding.
    /// </summary>
    public readonly struct GameEvent
    {
        public readonly GameEventType Type;
        public readonly float Time;
        public readonly float Value;   // seconds for Move/ChaseEnded/PuzzleSolved, 0..1 loudness for Noise, 1 = survived for Hide
        public readonly string Tag;    // spot type, route group or room id
        public readonly MoveMode Mode;

        public GameEvent(GameEventType type, float time, float value = 0f, string tag = null, MoveMode mode = MoveMode.Still)
        { Type = type; Time = time; Value = value; Tag = tag; Mode = mode; }
    }

    /// <summary>Plain in-process event hub. Gameplay publishes; trackers and clients subscribe. Never blocks and never calls the network.</summary>
    public sealed class GameEventBus
    {
        public event Action<GameEvent> Published;
        public int Count { get; private set; }
        public void Publish(GameEvent e) { Count++; Published?.Invoke(e); }
    }
}
