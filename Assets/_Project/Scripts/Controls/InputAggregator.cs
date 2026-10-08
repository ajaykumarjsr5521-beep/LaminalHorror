using System.Collections.Generic;
using UnityEngine;

namespace NocturneAnnex.Controls
{
    /// <summary>Merges all sources so e.g. keyboard and touch can be used together.</summary>
    public class InputAggregator
    {
        readonly List<IInputSource> _sources = new List<IInputSource>();

        public void Add(IInputSource source) => _sources.Add(source);

        public PlayerInputState Poll()
        {
            var merged = new PlayerInputState();
            foreach (var s in _sources)
            {
                var one = new PlayerInputState();
                s.Poll(ref one);
                merged.Move += one.Move;
                merged.Look += one.Look;
                merged.Sprint |= one.Sprint;
                merged.Crouch |= one.Crouch;
                merged.InteractPressed |= one.InteractPressed;
                merged.ThrowPressed |= one.ThrowPressed;
                merged.PausePressed |= one.PausePressed;
            }
            merged.Move = Vector2.ClampMagnitude(merged.Move, 1f);
            return merged;
        }

        public void ResetAll()
        {
            foreach (var s in _sources) s.ResetState();
        }
    }
}
