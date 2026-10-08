using System;
using System.Collections.Generic;
using UnityEngine;

namespace NocturneAnnex.Save
{
    public enum LifeOutcome { Respawn, RunOver }

    /// <summary>What killed the player and where. Saved so the entity can remember (F-14i); one entry per death.</summary>
    [Serializable]
    public class DeathRecord
    {
        public string Checkpoint = "";   // last checkpoint reached, the "room" of the death
        public string Cause = "";        // "caught" or "found_hiding"
        public string HidingSpot = "";   // name of the hide spot, empty when not hiding
        public Vector3 Position;
        public float RunSeconds;
    }

    /// <summary>
    /// The two lives of a run. The first death costs a life and respawns at the last checkpoint; the second ends the run.
    /// Plain data so it saves with the checkpoint and tests need no scene.
    /// </summary>
    public class RunLives
    {
        public const int MaxLives = 2;
        public const int MaxLog = 20;

        readonly List<DeathRecord> _log = new List<DeathRecord>();

        public int Left { get; private set; } = MaxLives;
        public int Deaths { get; private set; }
        public IReadOnlyList<DeathRecord> Log => _log;

        /// <summary>Records a death and spends a life. RunOver means no lives remain.</summary>
        public LifeOutcome LoseLife(DeathRecord record = null)
        {
            Deaths++;
            if (Left > 0) Left--;
            if (record != null)
            {
                _log.Add(record);
                if (_log.Count > MaxLog) _log.RemoveAt(0);
            }
            return Left > 0 ? LifeOutcome.Respawn : LifeOutcome.RunOver;
        }

        /// <summary>A fresh run: full lives, no deaths. The log is kept so the entity still remembers (F-14i clears it on a new game).</summary>
        public void NewRun() { Left = MaxLives; Deaths = 0; }

        public void ClearLog() => _log.Clear();

        /// <summary>Restores saved values. A negative or out-of-range life count (older saves) means a fresh run.</summary>
        public void Restore(int left, int deaths, IEnumerable<DeathRecord> log)
        {
            Left = (left < 0 || left > MaxLives) ? MaxLives : left;
            Deaths = Math.Max(0, deaths);
            _log.Clear();
            if (log != null) foreach (var r in log) if (r != null) _log.Add(r);
            while (_log.Count > MaxLog) _log.RemoveAt(0);
        }
    }
}
