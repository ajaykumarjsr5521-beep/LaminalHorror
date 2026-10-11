using System.Collections.Generic;
using System.Linq;

namespace NocturneAnnex.Level
{
    public enum DoorKind
    {
        Open,          // never locked (room 1 internal passages only)
        Progression,   // leads to the next room (the exit in room 10); exactly one per room
        Locked,        // locked, no way to open it
        Fake,          // looks like a door, never opens
        Blocked,       // blocked from the other side, never opens
        Key,           // opened by a key the player can find
        Back           // returns to the previous room, always unlocked
    }

    public enum UnlockKind { None, Key, Code, Switch }

    public enum KeyPlacement
    {
        Open,           // lying in the room
        BehindDoor,     // inside a space the named door opens
        AfterReturn     // appears when the player comes back to this room (return change RevealKey)
    }

    public enum ReturnChangeKind { None, ChairMoved, LightDead, NewSound, RevealKey }

    public sealed class DoorSpec
    {
        public string Id;
        public DoorKind Kind;
        public UnlockKind Unlock;
        public string UnlockId = "";   // key id, switch id, or empty for a code (the code is in PuzzleSpec)

        public DoorSpec(string id, DoorKind kind, UnlockKind unlock = UnlockKind.None, string unlockId = "")
        {
            Id = id; Kind = kind; Unlock = unlock; UnlockId = unlockId ?? "";
        }
    }

    public sealed class KeySpec
    {
        public string Id;
        public KeyPlacement Placement;
        public string BehindDoorId = "";

        public KeySpec(string id, KeyPlacement placement = KeyPlacement.Open, string behindDoor = "")
        {
            Id = id; Placement = placement; BehindDoorId = behindDoor ?? "";
        }
    }

    /// <summary>A hidden switch (or fuse box when it needs an item) that unlocks one door.</summary>
    public sealed class SwitchSpec
    {
        public string Id;
        public string RequiredItemId = "";   // e.g. "fuse"; empty means a plain switch
        public string BehindDoorId = "";     // empty means it is in the open

        public SwitchSpec(string id, string requiredItem = "", string behindDoor = "")
        {
            Id = id; RequiredItemId = requiredItem ?? ""; BehindDoorId = behindDoor ?? "";
        }
    }

    public sealed class CodeSpec
    {
        public string Code;
        public string ClueBehindDoorId = "";   // empty means the clue is in the open

        public CodeSpec(string code, string clueBehindDoor = "") { Code = code; ClueBehindDoorId = clueBehindDoor ?? ""; }
    }

    /// <summary>Per-room entity tuning. Every value must move the same way room after room (see RoomValidator).</summary>
    public sealed class DifficultySpec
    {
        public float Threat;                    // 0..1, rises
        public float InspectIntervalSeconds;    // falls (the entity checks hiding spots more often)
        public float InspectRange;              // rises
        public int PatrolPoints;                // rises or holds

        public DifficultySpec(float threat, float interval, float range, int patrol)
        {
            Threat = threat; InspectIntervalSeconds = interval; InspectRange = range; PatrolPoints = patrol;
        }
    }

    public sealed class RoomDefinition
    {
        public int Index;                 // 1..10
        public string Id;                 // "B1", "R02" ...
        public string Title;
        public List<DoorSpec> Doors = new List<DoorSpec>();
        public List<KeySpec> Keys = new List<KeySpec>();
        public List<SwitchSpec> Switches = new List<SwitchSpec>();
        public CodeSpec Code;             // null when the room has no combination lock
        public int HideSpots, NoiseProps;
        public DifficultySpec Difficulty;
        public ReturnChangeKind ReturnChange;
        public string RevealedKeyId = "";   // for ReturnChange RevealKey

        public DoorSpec Progression => Doors.FirstOrDefault(d => d.Kind == DoorKind.Progression);
        public DoorSpec Door(string id) => Doors.FirstOrDefault(d => d.Id == id);
        public string StartCheckpointId => "start_" + Id;
        /// <summary>The checkpoint saved when the player comes back into this room from the next one (the room is already done up to its far end).</summary>
        public string EndCheckpointId => Index == 1 ? "dock" : "end_" + Id;
    }
}
