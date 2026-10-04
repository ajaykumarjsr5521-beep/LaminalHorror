using System;
using UnityEngine;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.Level
{
    /// <summary>
    /// World changes that must hold once a checkpoint has been reached, so resuming from a save never traps the player:
    /// e.g. the Records Office door stays unlocked and its consumed key stays gone after the player is inside.
    /// </summary>
    [Serializable]
    public class ProgressGate
    {
        public string CheckpointId = "";
        public Door[] Unlock = new Door[0];
        public GameObject[] Hide = new GameObject[0];

        public void Apply()
        {
            foreach (var d in Unlock) if (d != null) d.Unlock();
            foreach (var g in Hide) if (g != null) g.SetActive(false);
        }
    }
}
