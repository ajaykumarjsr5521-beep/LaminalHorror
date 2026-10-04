using System;
using System.Collections.Generic;

namespace NocturneAnnex.Flow
{
    /// <summary>
    /// Tracks how far the player has got through a level as an ordered list of checkpoint ids.
    /// Progress only moves forward: walking back through an earlier checkpoint never rolls the save back.
    /// The exit opens only once the final required puzzle is solved. Plain C#, no Unity types, so it is unit-tested.
    /// </summary>
    public class LevelProgress
    {
        readonly List<string> _order;
        int _index;

        /// <param name="orderedCheckpointIds">Checkpoints in the order the level is meant to be played; the first is the start.</param>
        public LevelProgress(IEnumerable<string> orderedCheckpointIds)
        {
            _order = new List<string>(orderedCheckpointIds ?? throw new ArgumentNullException(nameof(orderedCheckpointIds)));
            if (_order.Count == 0) throw new ArgumentException("A level needs at least one checkpoint.", nameof(orderedCheckpointIds));
            var seen = new HashSet<string>();
            foreach (var id in _order)
            {
                if (string.IsNullOrEmpty(id)) throw new ArgumentException("Checkpoint ids must not be empty.", nameof(orderedCheckpointIds));
                if (!seen.Add(id)) throw new ArgumentException($"Duplicate checkpoint id '{id}'.", nameof(orderedCheckpointIds));
            }
        }

        public string CurrentId => _order[_index];

        /// <summary>True when the checkpoint is later than the current one. Unknown ids and earlier ones change nothing.</summary>
        public bool TryReach(string id)
        {
            int i = _order.IndexOf(id ?? "");
            if (i <= _index) return false;
            _index = i;
            return true;
        }

        /// <summary>Resets to a saved checkpoint. An empty or unknown id (new game, old save) falls back to the start.</summary>
        public void Restore(string id)
        {
            int i = _order.IndexOf(id ?? "");
            _index = i < 0 ? 0 : i;
        }

        public bool HasReached(string id)
        {
            int i = _order.IndexOf(id ?? "");
            return i >= 0 && i <= _index;
        }

        /// <summary>The exit is open only when the puzzle is solved; progress alone never opens it.</summary>
        public static bool CanExit(bool finalPuzzleSolved) => finalPuzzleSolved;
    }
}
