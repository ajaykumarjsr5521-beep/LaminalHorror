using System;
using UnityEngine.SceneManagement;
using NocturneAnnex.Flow;
using NocturneAnnex.Save;

namespace NocturneAnnex.Level
{
    /// <summary>
    /// Moves the player between room scenes. Everything that must follow them (items, lives, memory, visits) travels through the save,
    /// so a transition is: record the entry, write the checkpoint, load the scene in "continue" mode. Leaving the last room raises
    /// Escaped instead (the ending, F-14l).
    /// </summary>
    public static class RoomFlow
    {
        public const string ScenesDir = "Assets/_Project/Scenes/";

        /// <summary>Overridable for tests; defaults to SceneManager.LoadScene.</summary>
        public static Action<string> SceneLoader = path => SceneManager.LoadScene(path);

        public static event Action Escaped;

        static bool _pendingChange, _arrivedFromBack;

        public static string ScenePath(int room) => ScenesDir + (room == 1 ? "Level_B1" : "Level_R" + room.ToString("00")) + ".unity";

        public static bool HasNext(int room) => room >= 1 && room < RoomCatalog.Count;

        /// <summary>Goes to a room (forward or back). A target past the last room means the player escaped. False if the room is unknown.</summary>
        public static bool Go(SaveGame save, int target, bool back)
        {
            if (target > RoomCatalog.Count) { Escaped?.Invoke(); return true; }
            var room = RoomCatalog.Get(target);
            if (room == null || save == null) return false;
            _pendingChange = save.Rooms.Enter(target);
            _arrivedFromBack = back;
            save.SaveCheckpoint(back ? room.EndCheckpointId : room.StartCheckpointId);
            LevelLaunch.RequestContinue();
            SceneLoader(ScenePath(target));
            return true;
        }

        /// <summary>True once after an entry that made the room's return change fire. The room scene reads it to play the one-off part.</summary>
        public static bool ConsumePendingChange() { bool v = _pendingChange; _pendingChange = false; return v; }

        /// <summary>True once after the player came back through a back door.</summary>
        public static bool ConsumeArrivedFromBack() { bool v = _arrivedFromBack; _arrivedFromBack = false; return v; }

        public static void ResetForTests() { _pendingChange = _arrivedFromBack = false; SceneLoader = path => SceneManager.LoadScene(path); }
    }
}
