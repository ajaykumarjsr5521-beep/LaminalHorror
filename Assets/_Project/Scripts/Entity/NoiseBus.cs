using System;

namespace NocturneAnnex.Entity
{
    /// <summary>
    /// Where noises go. The player, doors, thrown objects and puzzles emit here; the entity's hearing and the debug overlay
    /// listen. One instance lives in the level, so tests and scenes never share hidden static state.
    /// </summary>
    public class NoiseBus
    {
        public event Action<NoiseEvent> Emitted;

        public int EmittedCount { get; private set; }

        public void Emit(NoiseEvent e)
        {
            EmittedCount++;
            Emitted?.Invoke(e);
        }
    }
}
