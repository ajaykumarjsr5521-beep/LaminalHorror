using System.Collections;
using UnityEngine;
using NocturneAnnex.Save;

namespace NocturneAnnex.Level
{
    /// <summary>Sends the player on to the next room a moment after a hand-built level (room 1) completes, so its end card still shows first.</summary>
    public sealed class LevelAdvance : MonoBehaviour
    {
        public LevelBootstrap Level;
        public SaveGame Save;
        public int NextRoom = 2;
        public float DelaySeconds = 2.5f;

        void OnEnable() { if (Level != null) Level.LevelCompleted += OnCompleted; }
        void OnDisable() { if (Level != null) Level.LevelCompleted -= OnCompleted; }

        void OnCompleted() => StartCoroutine(Go());

        IEnumerator Go()
        {
            yield return new WaitForSecondsRealtime(DelaySeconds);
            RoomFlow.Go(Save, NextRoom, false);
        }
    }
}
