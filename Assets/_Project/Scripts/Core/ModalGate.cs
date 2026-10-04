using System;
using UnityEngine;

namespace NocturneAnnex.Core
{
    /// <summary>
    /// Counts open modal screens (keypad, note reader, journal). While any is open, gameplay input such as movement,
    /// look and interact is ignored. Each Open returns a handle; disposing it more than once is harmless, so a view
    /// cannot leave the gate stuck open or release it twice.
    /// </summary>
    public static class ModalGate
    {
        static int _openCount;
        static int _generation;   // bumped by Reset so handles from before it become inert

        public static bool IsOpen => _openCount > 0;

        public static Handle Open()
        {
            _openCount++;
            return new Handle(_generation);
        }

        /// <summary>Clears all state. Used by tests and when a scene is torn down.</summary>
        public static void Reset()
        {
            _openCount = 0;
            _generation++;
        }

        // Keeps the gate correct when Enter Play Mode runs without a domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad() => Reset();

        public sealed class Handle : IDisposable
        {
            readonly int _generationAtOpen;
            bool _released;

            internal Handle(int generation) { _generationAtOpen = generation; }

            public void Dispose()
            {
                if (_released) return;
                _released = true;
                if (_generationAtOpen == _generation && _openCount > 0) _openCount--;
            }
        }
    }
}
