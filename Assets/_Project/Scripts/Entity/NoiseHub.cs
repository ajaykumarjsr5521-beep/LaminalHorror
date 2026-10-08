using UnityEngine;

namespace NocturneAnnex.Entity
{
    /// <summary>Scene holder of the level's NoiseBus, so emitters and the entity find the same bus without static state.</summary>
    public class NoiseHub : MonoBehaviour
    {
        [Tooltip("Story mode halves every noise radius.")] public bool Story;

        public NoiseBus Bus { get; } = new NoiseBus();

        /// <summary>Emits a noise at a position using the table radius, the floor scale and the Story setting.</summary>
        public void Emit(Vector3 position, NoiseKind kind, float scale = 1f) =>
            Bus.Emit(NoiseEvent.Make(position, kind, Time.time, Story, scale));
    }
}
