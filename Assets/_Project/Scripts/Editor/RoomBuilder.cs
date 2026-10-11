using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using NocturneAnnex.Controls;
using NocturneAnnex.Entity;
using NocturneAnnex.Interaction;
using NocturneAnnex.Inventory;
using NocturneAnnex.Level;
using NocturneAnnex.Player;
using NocturneAnnex.Puzzle;
using NocturneAnnex.Save;

namespace NocturneAnnex.Editor
{
    /// <summary>
    /// Generates the greybox scenes for rooms 2-10 from RoomCatalog (F-14f). Every room shares one footprint: a back door in the south
    /// wall, a row of doors in the north wall (each with a small closet behind it, the progression door in a different slot per room),
    /// shelves, hiding cupboards, throwable bottles, the items the table places, and the stalker tuned to the room's difficulty row.
    /// Room 1 stays the hand-built Level_B1. Art is greybox by design.
    /// </summary>
    public static class RoomBuilder
    {
        const float W = 24f, ClosetW = 3.4f, ClosetD = 3.5f;
        const string FalseKey = "__never__";

        static float Depth(RoomDefinition r) => 20f + r.Index;

        [MenuItem("Build/Create Room Scenes (R02-R10)")]
        public static void CreateAll()
        {
            var problems = RoomValidator.Validate(RoomCatalog.All);
            if (problems.Count > 0) throw new System.InvalidOperationException("Room table is invalid:\n" + string.Join("\n", problems));
            LevelItems.CreateAll();
            LevelAudio.CreateAssets();
            if (!File.Exists(GameplayUiBuilder.KeypadPath)) GameplayUiBuilder.CreateAll();
            foreach (var room in RoomCatalog.All.Where(r => r.Index >= 2)) Create(room);
            AssetDatabase.SaveAssets();
        }

        public static void Create(RoomDefinition room)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(LevelItems.DatabasePath);
            LevelB1Builder.MakeMaterials();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.27f, 0.27f, 0.31f);

            float d = Depth(room);
            var geo = new GameObject("Geometry").transform;
            var wall = LevelB1Builder._wall; var prop = LevelB1Builder._prop;

            // slots along the north wall: the progression door moves from room to room
            var north = room.Doors.Where(x => x.Kind != DoorKind.Back).ToList();
            int n = north.Count;
            int progSlot = (room.Index * 2) % n;
            var ordered = new DoorSpec[n];
            var rest = north.Where(x => x.Kind != DoorKind.Progression).ToList();
            ordered[progSlot] = room.Progression;
            for (int i = 0, k = 0; i < n; i++) if (ordered[i] == null) ordered[i] = rest[k++];
            var slotX = new float[n];
            for (int i = 0; i < n; i++) slotX[i] = -W / 2f + (i + 0.5f) * W / n;

            BuildShell(geo, d, slotX);
            var doors = new Dictionary<string, Door>();
            var closets = new Dictionary<string, Vector3>();   // door id -> closet floor centre

            // back door in the south wall, vestibule behind it
            var backSpec = room.Doors.First(x => x.Kind == DoorKind.Back);
            Vestibule(geo, "Back", 0f, 0f, -ClosetD, +1);
            doors[backSpec.Id] = LevelB1Builder.MakeDoor(geo, "Door_" + backSpec.Id, new Vector3(0f, 0f, 0f), true, "", +LevelB1Builder.OpenAngle);
            // north doors, each with a closet
            for (int i = 0; i < n; i++)
            {
                var spec = ordered[i];
                Vestibule(geo, "Closet_" + spec.Id, slotX[i], d, d + ClosetD, +1);
                closets[spec.Id] = new Vector3(slotX[i], 0f, d + ClosetD / 2f);
                doors[spec.Id] = LevelB1Builder.MakeDoor(geo, "Door_" + spec.Id, new Vector3(slotX[i], 0f, d), true, DoorKeyFor(spec), -LevelB1Builder.OpenAngle);
            }

            LevelLighting.MarkShellStatic(geo);

            var props = new GameObject("Props").transform;
            props.SetParent(geo, false);
            BuildShelves(props, d);

            new GameObject("InputRouter").AddComponent<InputRouter>();
            var player = LevelB1Builder.BuildPlayer(db, out var interactor, out var inventory);

            // items, switches, code
            var pickups = new Dictionary<string, GameObject>();
            var rng = new System.Random(room.Index * 7919);
            foreach (var key in room.Keys)
            {
                Vector3 at = key.Placement == KeyPlacement.BehindDoor && closets.TryGetValue(key.BehindDoorId, out var c)
                    ? c + new Vector3(0f, 0f, 0.3f)
                    : new Vector3(Mathf.Lerp(-8f, 8f, (float)rng.NextDouble()), 0f, Mathf.Lerp(5f, d - 6f, (float)rng.NextDouble()));
                LevelB1Builder.Box(props, "Table_" + key.Id, at + new Vector3(0f, 0.4f, 0f), new Vector3(0.9f, 0.8f, 0.6f), prop);
                var go = LevelB1Builder.Box(props, "Item_" + key.Id, at + new Vector3(0f, 0.9f, 0f), new Vector3(0.25f, 0.2f, 0.25f), LevelB1Builder._brass);
                var pickup = go.AddComponent<Pickup>();
                pickup.ItemId = key.Id;
                pickup.DisplayName = LevelItems.Get(db, key.Id).DisplayName;
                pickups[key.Id] = go;
                if (key.Placement == KeyPlacement.AfterReturn) go.SetActive(false);
            }

            foreach (var sw in room.Switches)
            {
                Vector3 at = !string.IsNullOrEmpty(sw.BehindDoorId) && closets.TryGetValue(sw.BehindDoorId, out var c)
                    ? new Vector3(c.x, 1.2f, d + ClosetD - 0.35f)
                    : new Vector3(-W / 2f + 0.4f, 1.2f, d * 0.55f);
                var box = LevelB1Builder.Box(props, "Switch_" + sw.Id, at, new Vector3(0.3f, 0.4f, 0.2f), LevelB1Builder._brass);
                var us = box.AddComponent<UnlockSwitch>();
                us.RequiredItemId = sw.RequiredItemId;
                var target = room.Doors.FirstOrDefault(x => x.Unlock == UnlockKind.Switch && x.UnlockId == sw.Id);
                if (target != null) us.TargetDoor = doors[target.Id];
            }

            CodeLock codeLock = null;
            if (room.Code != null)
            {
                var pad = LevelB1Builder.Box(props, "Keypad", new Vector3(W / 2f - 0.4f, 1.3f, d * 0.5f), new Vector3(0.2f, 1f, 0.7f), LevelB1Builder._paper);
                LevelB1Builder.AddBoardText(pad.transform, "Keypad", "CODE", "- - - -", Color.black, bigPercent: 130);
                pad.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                codeLock = pad.AddComponent<CodeLock>();
                codeLock.Code = room.Code.Code;
                codeLock.PuzzleId = "code_" + room.Id;
                var codeDoor = room.Doors.First(x => x.Unlock == UnlockKind.Code);
                codeLock.TargetDoor = doors[codeDoor.Id];

                Vector3 clueAt = !string.IsNullOrEmpty(room.Code.ClueBehindDoorId) && closets.TryGetValue(room.Code.ClueBehindDoorId, out var cc)
                    ? new Vector3(cc.x, 1.5f, d + ClosetD - 0.2f)
                    : new Vector3(-W / 2f + 0.2f, 1.5f, d * 0.3f);
                var board = LevelB1Builder.Box(props, "ClueBoard", clueAt, new Vector3(0.9f, 0.6f, 0.1f), LevelB1Builder._paper);
                LevelB1Builder.AddBoardText(board.transform, "Clue", "THE NUMBER", room.Code.Code, new Color(0.6f, 0.05f, 0.05f), bigPercent: 150);
                if (string.IsNullOrEmpty(room.Code.ClueBehindDoorId)) board.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            }

            // return-change props
            Transform chair = null; Light deadLight = null; GameObject revealed = null;
            chair = LevelB1Builder.Box(props, "Chair", new Vector3(-5f, 0.4f, d * 0.45f), new Vector3(0.6f, 0.8f, 0.6f), prop).transform;
            if (!string.IsNullOrEmpty(room.RevealedKeyId)) pickups.TryGetValue(room.RevealedKeyId, out revealed);

            // lights: two realtime points, the second is the one that can die on a return visit
            var lights = new GameObject("Lights").transform;
            for (int i = 0; i < 2; i++)
            {
                var l = new GameObject("Light_" + (i + 1)).AddComponent<Light>();
                l.transform.SetParent(lights, false);
                l.type = LightType.Point; l.color = new Color(1f, 0.85f, 0.65f); l.intensity = 7f; l.range = 14f;
                l.transform.position = new Vector3(0f, 2.6f, d * (i == 0 ? 0.3f : 0.7f));
                if (i == 1) deadLight = l;
            }

            LevelAudio.Build(player);

            // stalker, hiding places, bottles
            var layout = RoomLayout(room, d);
            var patrol = PatrolPoints(room, d);
            var stalker = StalkerBuilder.Build(player, geo, new Vector3(8f, 0.1f, d - 6f), patrol, layout);
            RoomDifficulty.Apply(stalker, room.Difficulty);

            // checkpoints, save, level
            var cpParent = new GameObject("Checkpoints").transform;
            float progX = slotX[progSlot];
            var start = LevelB1Builder.Checkpoint(cpParent, room.StartCheckpointId, new Vector3(0f, 0f, 2f), new Vector3(6f, 2.5f, 3f));
            var end = LevelB1Builder.Checkpoint(cpParent, room.EndCheckpointId, new Vector3(progX, 0f, d - 3f), new Vector3(3f, 2.5f, 2.5f));

            var save = new GameObject("SaveGame").AddComponent<SaveGame>();
            save.Inventory = inventory;
            save.Locks = codeLock != null ? new[] { codeLock } : new CodeLock[0];

            var level = new GameObject("LevelBootstrap").AddComponent<LevelBootstrap>();
            level.Checkpoints = new[] { start, end };
            level.FinalLock = codeLock;
            level.SaveGame = save;
            level.Player = player.transform;

            // transitions: forward through the progression closet, back through the vestibule
            Transition(save, "To_" + (room.Index + 1), new Vector3(progX, 1.25f, d + ClosetD / 2f), room.Index + 1, false);
            Transition(save, "To_" + (room.Index - 1), new Vector3(0f, 1.25f, -ClosetD / 2f), room.Index - 1, true);

            var backSpawn = new GameObject("BackSpawn").transform;
            backSpawn.position = new Vector3(progX, 0.1f, d - 2f);
            backSpawn.rotation = Quaternion.Euler(0f, 180f, 0f);
            var runtime = new GameObject("RoomRuntime").AddComponent<RoomRuntime>();
            runtime.Save = save; runtime.Player = player.transform; runtime.RoomIndex = room.Index; runtime.BackSpawn = backSpawn;
            runtime.Change = room.ReturnChange; runtime.Chair = chair; runtime.DeadLight = deadLight; runtime.RevealedObject = revealed;

            var death = new GameObject("DeathController").AddComponent<DeathController>();
            death.Stalker = stalker; death.Level = level; death.Save = save;
            var recorder = death.gameObject.AddComponent<EntityMemoryRecorder>();
            recorder.Save = save; recorder.Stalker = stalker; recorder.Motor = player.GetComponent<PlayerMotor>(); recorder.Thrower = player.GetComponent<PlayerThrower>();

            LevelHudBuilder.Build(interactor, inventory, codeLock, level);

            string path = RoomFlow.ScenePath(room.Index);
            EditorSceneManager.SaveScene(scene, path);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Room " + room.Id + " written to " + path);
        }

        // ---------- pieces ----------

        static string DoorKeyFor(DoorSpec spec)
        {
            switch (spec.Kind)
            {
                case DoorKind.Fake: return FalseKey + "fake";
                case DoorKind.Locked: return FalseKey + "locked";
                case DoorKind.Blocked: return FalseKey + "blocked";
            }
            switch (spec.Unlock)
            {
                case UnlockKind.Key: return spec.UnlockId;
                case UnlockKind.Code: return "__code_lock__";
                case UnlockKind.Switch: return "__switch__";
            }
            return "";
        }

        static void BuildShell(Transform geo, float d, float[] slotX)
        {
            var wall = LevelB1Builder._wall; var floor = LevelB1Builder._floor;
            float h = LevelB1Builder.WallH, t = LevelB1Builder.WallT;
            var f = LevelB1Builder.Box(geo, "Floor", new Vector3(0f, -0.25f, d / 2f), new Vector3(W, 0.5f, d), floor);
            f.AddComponent<NocturneAnnex.Audio.FloorSurface>().Surface = "tile";
            LevelB1Builder.Box(geo, "Ceiling", new Vector3(0f, h + 0.15f, d / 2f), new Vector3(W, 0.3f, d), wall);
            LevelB1Builder.WallZ(geo, "West", -W / 2f, 0f, d);
            LevelB1Builder.WallZ(geo, "East", W / 2f, 0f, d);
            LevelB1Builder.WallX(geo, "South", 0f, -W / 2f, W / 2f, 0f);

            // north wall with one doorway per slot
            float x0 = -W / 2f;
            float dw = LevelB1Builder.DoorW, dh = LevelB1Builder.DoorH;
            for (int i = 0; i <= slotX.Length; i++)
            {
                float x1 = i < slotX.Length ? slotX[i] - dw / 2f : W / 2f;
                if (x1 > x0) LevelB1Builder.Box(geo, "North_" + i, new Vector3((x0 + x1) / 2f, h / 2f, d), new Vector3(x1 - x0, h, t), wall);
                if (i < slotX.Length)
                {
                    LevelB1Builder.Box(geo, "NorthLintel_" + i, new Vector3(slotX[i], (dh + h) / 2f, d), new Vector3(dw, h - dh, t), wall);
                    x0 = slotX[i] + dw / 2f;
                }
            }
        }

        /// <summary>A small walled space behind a doorway: floor, ceiling, two side walls and an end wall. zEnd is the far end (south of zWall for the back door).</summary>
        static void Vestibule(Transform geo, string name, float x, float zWall, float zEnd, int _)
        {
            var wall = LevelB1Builder._wall; var floor = LevelB1Builder._floor;
            float h = LevelB1Builder.WallH, t = LevelB1Builder.WallT;
            float zMid = (zWall + zEnd) / 2f, len = Mathf.Abs(zEnd - zWall);
            LevelB1Builder.Box(geo, name + "_Floor", new Vector3(x, -0.25f, zMid), new Vector3(ClosetW, 0.5f, len), floor);
            LevelB1Builder.Box(geo, name + "_Ceiling", new Vector3(x, h + 0.15f, zMid), new Vector3(ClosetW, 0.3f, len), wall);
            LevelB1Builder.Box(geo, name + "_WallL", new Vector3(x - ClosetW / 2f, h / 2f, zMid), new Vector3(t, h, len), wall);
            LevelB1Builder.Box(geo, name + "_WallR", new Vector3(x + ClosetW / 2f, h / 2f, zMid), new Vector3(t, h, len), wall);
            LevelB1Builder.Box(geo, name + "_End", new Vector3(x, h / 2f, zEnd), new Vector3(ClosetW + t, h, t), wall);
        }

        static void BuildShelves(Transform props, float d)
        {
            var prop = LevelB1Builder._prop;
            int i = 0;
            foreach (float z in new[] { d * 0.35f, d * 0.65f })
                foreach (float x in new[] { -9f, -6f, 6f, 9f })
                    LevelB1Builder.Box(props, "Shelf_" + (++i), new Vector3(x, 1f, z), new Vector3(2.2f, 2f, 0.7f), prop);
        }

        static StalkerBuilder.Layout RoomLayout(RoomDefinition room, float d)
        {
            var cupboards = new List<StalkerBuilder.Cupboard>();
            for (int i = 0; i < room.HideSpots; i++)
            {
                bool west = i % 2 == 0;
                float z = Mathf.Lerp(d * 0.2f, d * 0.8f, room.HideSpots == 1 ? 0.5f : i / (float)(room.HideSpots - 1));
                cupboards.Add(new StalkerBuilder.Cupboard
                {
                    Name = "Cupboard_" + (i + 1),
                    FloorPos = new Vector3(west ? -W / 2f + 0.6f : W / 2f - 0.6f, 0f, z),
                    Size = new Vector3(0.9f, 2.2f, 1.4f),
                    ExitOffset = new Vector3(west ? 1.3f : -1.3f, 0f, 0f),
                    Safety = 0.3f + 0.15f * (i % 4)
                });
            }
            var bottles = new List<Vector3>();
            for (int i = 0; i < room.NoiseProps; i++)
                bottles.Add(new Vector3(Mathf.Lerp(-8f, 8f, ((i * 37) % 11) / 10f), 0.4f, Mathf.Lerp(4f, d - 5f, (i + 0.5f) / room.NoiseProps)));
            return new StalkerBuilder.Layout
            {
                Bottles = bottles.ToArray(), Cupboards = cupboards.ToArray(),
                NavOrigin = new Vector3(0f, 0f, d / 2f), NavSize = new Vector3(60f, 20f, d + 20f)
            };
        }

        static Vector3[] PatrolPoints(RoomDefinition room, float d)
        {
            int count = room.Difficulty.PatrolPoints;
            var pts = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = count == 1 ? 0.5f : i / (float)(count - 1);
                pts[i] = new Vector3(i % 2 == 0 ? -3.5f : 3.5f, 0.1f, Mathf.Lerp(5f, d - 4f, t));   // down the central corridor between the shelf rows
            }
            return pts;
        }

        static void Transition(SaveGame save, string name, Vector3 centre, int target, bool back)
        {
            var go = new GameObject(name, typeof(BoxCollider));
            go.transform.position = centre;
            var col = go.GetComponent<BoxCollider>();
            col.isTrigger = true; col.size = new Vector3(2.4f, 2.5f, 1.2f);
            var tr = go.AddComponent<RoomTransition>();
            tr.Save = save; tr.TargetRoom = target; tr.Back = back;
        }
    }
}
