using System;
using System.IO;
using UnityEngine;
using NocturneAnnex.Controls;
using NocturneAnnex.Core;
using NocturneAnnex.Flow;
using NocturneAnnex.Puzzle;
using NocturneAnnex.Save;
using NocturneAnnex.Settings;

namespace NocturneAnnex.Level
{
    /// <summary>
    /// Wires one level together: resumes or starts the run, saves at checkpoints and decides when the exit counts.
    /// Rules live in LevelProgress; this component only connects scene objects to them.
    /// </summary>
    public class LevelBootstrap : MonoBehaviour
    {
        [Tooltip("In play order; the first is the start.")]
        public CheckpointTrigger[] Checkpoints = new CheckpointTrigger[0];
        public ExitTrigger Exit;
        public CodeLock FinalLock;
        public SaveGame SaveGame;
        public Transform Player;
        public ProgressGate[] Gates = new ProgressGate[0];
        /// <summary>Overridable for tests; defaults to settings.json in persistentDataPath.</summary>
        public SettingsStore SettingsStore;

        public LevelProgress Progress { get; private set; }
        public bool Completed { get; private set; }
        public event Action LevelCompleted;

        void Start()
        {
            if (Progress != null) return;
            ApplySavedSettings();
            Begin(LevelLaunch.ConsumeContinue());
        }

        // The player may reach the level without passing the menu's settings step (restart, direct load).
        void ApplySavedSettings()
        {
            SettingsStore ??= new SettingsStore(Path.Combine(Application.persistentDataPath, "settings.json"), QualitySettings.names.Length);
            SettingsApplier.Apply(SettingsStore.Load().Data, InputRouter.Instance);
        }

        /// <summary>Starts the run, resuming the save when asked. Safe to call again (tests restart a level this way).</summary>
        public void Begin(bool resume)
        {
            Detach();
            Completed = false;
            var ids = new string[Checkpoints.Length];
            for (int i = 0; i < ids.Length; i++) ids[i] = Checkpoints[i].Id;
            Progress = new LevelProgress(ids);   // throws on empty or duplicate ids: a bad level must fail loudly

            foreach (var c in Checkpoints) c.Reached += OnCheckpointReached;
            if (Exit != null) Exit.Entered += OnExitEntered;

            if (resume && SaveGame != null)
            {
                var result = SaveGame.Continue();
                if (result.Ok) Progress.Restore(SaveGame.LastCheckpointId);
                else Debug.LogWarning("LevelBootstrap: could not resume, starting from the entrance. " + result.Message);
            }
            ApplyGates();
            MovePlayerTo(Progress.CurrentId);
        }

        void ApplyGates()
        {
            foreach (var g in Gates)
                if (Progress.HasReached(g.CheckpointId)) g.Apply();
        }

        void OnDestroy() => Detach();

        void Detach()
        {
            foreach (var c in Checkpoints) if (c != null) c.Reached -= OnCheckpointReached;
            if (Exit != null) Exit.Entered -= OnExitEntered;
        }

        void OnCheckpointReached(CheckpointTrigger checkpoint)
        {
            if (!Progress.TryReach(checkpoint.Id)) return;
            ApplyGates();
            bool saved = SaveGame != null && SaveGame.SaveCheckpoint(checkpoint.Id);
            Captions.Post(saved ? "level.checkpoint_saved" : "level.save_failed");
        }

        void OnExitEntered()
        {
            if (Completed) return;
            if (!LevelProgress.CanExit(FinalLock != null && FinalLock.IsSolved))
            {
                Captions.Post("level.exit_locked");
                return;
            }
            Completed = true;
            LevelCompleted?.Invoke();
        }

        void MovePlayerTo(string checkpointId)
        {
            if (Player == null) return;
            foreach (var c in Checkpoints)
            {
                if (c.Id != checkpointId) continue;
                // A CharacterController overrides transform changes unless it is switched off while moving.
                var cc = Player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                Player.SetPositionAndRotation(c.Spawn.position, c.Spawn.rotation);
                if (cc != null) cc.enabled = true;
                return;
            }
        }
    }
}
