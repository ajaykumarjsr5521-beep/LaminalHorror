using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using NocturneAnnex.Inventory;
using NocturneAnnex.Horror;
using NocturneAnnex.Puzzle;

namespace NocturneAnnex.Save
{
    /// <summary>
    /// Captures and applies game state (checkpoint, inventory, solved puzzles) through a SaveStore.
    /// Checkpoint/pause triggers call SaveCheckpoint; the menu (F-07) calls Continue / StartNewGame.
    /// </summary>
    public class SaveGame : MonoBehaviour
    {
        public PlayerInventory Inventory;
        public CodeLock[] Locks = new CodeLock[0];
        [Tooltip("Optional: saves which one-shot horror events already fired.")]
        public HorrorEventRunner Horror;
        [Tooltip("Leave empty to use persistentDataPath/save.json.")]
        public string FilePathOverride = "";

        /// <summary>Raised with a player-facing message when saving fails.</summary>
        public event Action<string> SaveFailed;

        public string LastCheckpointId { get; private set; } = "";

        /// <summary>The run's lives and death log; saved and restored with each checkpoint.</summary>
        public RunLives Lives { get; } = new RunLives();

        SaveStore _store;
        public SaveStore Store => _store ??= new SaveStore(
            string.IsNullOrEmpty(FilePathOverride) ? Path.Combine(Application.persistentDataPath, "save.json") : FilePathOverride);

        void Awake() => ValidateLockIds();

        void ValidateLockIds()
        {
            var seen = new HashSet<string>();
            foreach (var l in Locks)
            {
                if (l == null) continue;
                if (string.IsNullOrEmpty(l.PuzzleId)) Debug.LogError($"SaveGame: CodeLock '{l.name}' has no PuzzleId; its state will not be saved.", l);
                else if (!seen.Add(l.PuzzleId)) Debug.LogError($"SaveGame: duplicate PuzzleId '{l.PuzzleId}'.", l);
            }
        }

        public SaveData Capture(string checkpointId)
        {
            var solved = new List<string>();
            foreach (var l in Locks)
                if (l != null && l.IsSolved && !string.IsNullOrEmpty(l.PuzzleId)) solved.Add(l.PuzzleId);
            return new SaveData
            {
                CheckpointId = checkpointId ?? "",
                InventoryIds = Inventory != null ? Inventory.Snapshot() : new string[0],
                SolvedPuzzleIds = solved.ToArray(),
                FiredEventIds = Horror != null && Horror.Picker != null ? Horror.Picker.SnapshotFiredOnce() : new string[0],
                LivesLeft = Lives.Left,
                DeathCount = Lives.Deaths,
                DeathLog = System.Linq.Enumerable.ToArray(Lives.Log)
            };
        }

        public bool SaveCheckpoint(string checkpointId)
        {
            var result = Store.Write(Capture(checkpointId));
            if (!result.Ok) { SaveFailed?.Invoke(result.Error); return false; }
            LastCheckpointId = checkpointId ?? "";
            return true;
        }

        /// <summary>Loads the save and applies it if valid. The caller decides what to show for other statuses.</summary>
        public LoadResult Continue()
        {
            var result = Store.Load();
            if (result.Ok) Apply(result.Data);
            return result;
        }

        public void Apply(SaveData data)
        {
            Inventory?.Restore(data.InventoryIds);
            var solved = new HashSet<string>(data.SolvedPuzzleIds);
            foreach (var l in Locks)
                if (l != null && !string.IsNullOrEmpty(l.PuzzleId)) l.RestoreSolved(solved.Contains(l.PuzzleId));
            Horror?.Picker?.RestoreFiredOnce(data.FiredEventIds);
            Lives.Restore(data.LivesLeft, data.DeathCount, data.DeathLog);
            LastCheckpointId = data.CheckpointId;
        }

        /// <summary>
        /// Clears the slot for a new game. An unreadable save is moved aside, never deleted.
        /// Asking the player to confirm is the menu's job (F-07).
        /// </summary>
        public bool StartNewGame()
        {
            LastCheckpointId = "";
            Lives.NewRun();
            Lives.ClearLog();
            return Store.ClearForNewGame();
        }
    }
}
