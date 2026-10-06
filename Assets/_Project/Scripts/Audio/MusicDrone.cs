using System;
using UnityEngine;
using NocturneAnnex.Horror;

namespace NocturneAnnex.Audio
{
    /// <summary>Plays the looping music drone and sets its volume from tension. Silent while the game is blocked.</summary>
    public class MusicDrone : MonoBehaviour
    {
        public const string CueId = "music.drone";

        public AudioDirector Director;
        /// <summary>Where tension comes from. Left empty, it binds to the level's HorrorEventRunner on Start.</summary>
        public Func<float> TensionSource;
        public Func<bool> BlockedSource;

        public MusicDroneModel Model { get; } = new MusicDroneModel();

        void Start()
        {
            if (TensionSource == null)
            {
                var runner = FindFirstObjectByType<HorrorEventRunner>();
                if (runner != null)
                {
                    TensionSource = () => runner.Director.Tension;
                    BlockedSource = () => runner.Blocked;
                }
            }
            if (Director != null)
            {
                Director.Play(CueId, null, loop: true, volumeScale: 1f);
                Director.SetVolumeScale(CueId, 0f);
            }
        }

        void Update()
        {
            if (Director == null) return;
            float tension = TensionSource != null ? TensionSource() : 0f;
            bool blocked = BlockedSource != null && BlockedSource();
            Director.SetVolumeScale(CueId, Model.Tick(Time.unscaledDeltaTime, tension, blocked));
        }
    }
}
