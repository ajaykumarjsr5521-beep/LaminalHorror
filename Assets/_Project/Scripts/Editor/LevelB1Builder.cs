using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using NocturneAnnex.Audio;
using NocturneAnnex.Controls;
using NocturneAnnex.Interaction;
using NocturneAnnex.Inventory;
using NocturneAnnex.Level;
using NocturneAnnex.Player;
using NocturneAnnex.Puzzle;
using NocturneAnnex.Save;

namespace NocturneAnnex.Editor
{
    /// <summary>
    /// Generates the Floor B1 greybox scene: six areas, the Three Dates puzzle, checkpoints, exit and HUD.
    /// Primitive geometry only, so there are no third-party assets. Regenerate rather than hand-editing.
    ///
    /// Plan view (x across, z forward):
    ///   Break Room (start) -> Main Reading Hall (hub) -> west: Stacks A (stamp), east: Records Office (keypad, needs stamp),
    ///   north: Loading Dock Stair (locked until the keypad is solved) -> exit.
    /// </summary>
    public static class LevelB1Builder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Level_B1.unity";
        const string MatDir = "Assets/_Project/Materials";

        const float OpenAngle = 100f;
        const float WallH = 3f, WallT = 0.3f, DoorW = 1.5f, DoorH = 2.2f;
        static Material _wall, _floor, _prop, _paper, _brass;

        [MenuItem("Build/Create Level B1 Scene")]
        public static void Create()
        {
            LevelItems.CreateAll();
            LevelEvents.CreateAssets();
            LevelAudio.CreateAssets();
            // Prefab regeneration rewrites every file id, so only build them when missing.
            if (!File.Exists(GameplayUiBuilder.KeypadPath)) GameplayUiBuilder.CreateAll();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Load assets only after the new scene exists: creating it unloads unused assets and would leave stale references.
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>(LevelItems.DatabasePath);
            MakeMaterials();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.16f, 0.19f);

            var geo = new GameObject("Geometry").transform;
            BuildShell(geo);
            LevelLighting.MarkShellStatic(geo);   // only the shell exists under Geometry so far; doors and props are added after
            var doors = BuildDoors(geo);
            var props = BuildProps(geo, db, doors.codeDoor, out var finalLock);
            BuildLights();

            new GameObject("InputRouter").AddComponent<InputRouter>();
            var player = BuildPlayer(db, out var interactor, out var inventory);
            LevelAudio.Build(player);

            var cps = BuildCheckpoints();
            var exit = new GameObject("Exit", typeof(BoxCollider));
            exit.transform.position = new Vector3(0f, 1.25f, 40.6f);
            var exitCol = exit.GetComponent<BoxCollider>();
            exitCol.isTrigger = true;
            exitCol.size = new Vector3(2.8f, 2.5f, 1.2f);
            var exitTrigger = exit.AddComponent<ExitTrigger>();

            var save = new GameObject("SaveGame").AddComponent<SaveGame>();
            save.Inventory = inventory;
            save.Locks = new[] { finalLock };

            var level = new GameObject("LevelBootstrap").AddComponent<LevelBootstrap>();
            level.Checkpoints = cps;
            level.Exit = exitTrigger;
            level.FinalLock = finalLock;
            level.SaveGame = save;
            level.Player = player.transform;
            level.Gates = new[]
            {
                // once the player is inside the Records Office its door stays open to them and the used stamp stays gone
                new ProgressGate
                {
                    CheckpointId = "records",
                    Unlock = new[] { doors.records },
                    Hide = new[] { props.transform.Find("BrassStamp").gameObject },
                },
            };

            LevelEvents.Build(level, save, player.GetComponentInChildren<Camera>());
            LevelHudBuilder.Build(interactor, inventory, finalLock, level);

            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!LevelLighting.Bake()) Debug.LogError("Level_B1 lightmap bake did not finish; the baked lights will not light the level.");
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Level_B1 written to " + Path.GetFullPath(ScenePath));
        }

        // ---------- structure ----------

        static void BuildShell(Transform parent)
        {
            // floors and ceilings, one slab per area
            foreach (var (name, x0, x1, z0, z1) in Areas)
            {
                var floor = Box(parent, "Floor_" + name, new Vector3((x0 + x1) / 2f, -0.25f, (z0 + z1) / 2f), new Vector3(x1 - x0, 0.5f, z1 - z0), _floor);
                floor.AddComponent<FloorSurface>().Surface = LevelAudio.SurfaceFor(name);
                Box(parent, "Ceiling_" + name, new Vector3((x0 + x1) / 2f, WallH + 0.15f, (z0 + z1) / 2f), new Vector3(x1 - x0, 0.3f, z1 - z0), _wall);
            }

            // Break Room (x -4..4, z 0..10)
            WallZ(parent, "BreakW", -4f, 0f, 10f);
            WallZ(parent, "BreakE", 4f, 0f, 10f);
            WallX(parent, "BreakS", 0f, -4f, 4f);
            // Hall (x -6..6, z 10..26); doorways at x=0 (south, to Break Room, z=10), z=18 (west and east), x=0 (north, z=26)
            WallX(parent, "HallS", 10f, -6f, 6f, 0f);
            WallZ(parent, "HallW", -6f, 10f, 26f, 18f);
            WallZ(parent, "HallE", 6f, 10f, 26f, 18f);
            WallX(parent, "HallN", 26f, -6f, 6f, 0f);
            // Stacks A (x -18..-6, z 12..24)
            WallZ(parent, "StacksW", -18f, 12f, 24f);
            WallX(parent, "StacksS", 12f, -18f, -6f);
            WallX(parent, "StacksN", 24f, -18f, -6f);
            // Records Office (x 6..18, z 12..24)
            WallZ(parent, "RecordsE", 18f, 12f, 24f);
            WallX(parent, "RecordsS", 12f, 6f, 18f);
            WallX(parent, "RecordsN", 24f, 6f, 18f);
            // Loading Dock Stair (x -1.5..1.5, z 26..42)
            WallZ(parent, "DockW", -1.5f, 26f, 42f);
            WallZ(parent, "DockE", 1.5f, 26f, 42f);
            WallX(parent, "DockEnd", 42f, -1.5f, 1.5f);
        }

        static readonly (string name, float x0, float x1, float z0, float z1)[] Areas =
        {
            ("BreakRoom", -4f, 4f, 0f, 10f),
            ("Hall", -6f, 6f, 10f, 26f),
            ("Stacks", -18f, -6f, 12f, 24f),
            ("Records", 6f, 18f, 12f, 24f),
            ("Dock", -1.5f, 1.5f, 26f, 42f),
        };

        struct DoorSet { public Door a, stacks, records, codeDoor; }

        static DoorSet BuildDoors(Transform parent)
        {
            return new DoorSet
            {
                a = MakeDoor(parent, "Door_BreakToHall", new Vector3(0f, 0f, 10f), alongX: true, key: "", openAngle: -OpenAngle),
                stacks = MakeDoor(parent, "Door_HallToStacks", new Vector3(-6f, 0f, 18f), alongX: false, key: "", openAngle: -OpenAngle),
                records = MakeDoor(parent, "Door_HallToRecords", new Vector3(6f, 0f, 18f), alongX: false, key: LevelItems.StampId, openAngle: OpenAngle),
                // locked with a key that does not exist; only the solved keypad unlocks it
                codeDoor = MakeDoor(parent, "Door_HallToDock", new Vector3(0f, 0f, 26f), alongX: true, key: "__code_lock__", openAngle: -OpenAngle),
            };
        }

        static GameObject BuildProps(Transform parent, ItemDatabase db, Door codeDoor, out CodeLock finalLock)
        {
            var root = new GameObject("Props");
            root.transform.SetParent(parent, false);
            var p = root.transform;

            // Break Room: table with the memo, calendar showing the first date
            Box(p, "Table_Break", new Vector3(2.4f, 0.4f, 3f), new Vector3(1.4f, 0.8f, 0.8f), _prop);
            MakeNote(p, db, LevelItems.MemoId, new Vector3(2.4f, 0.84f, 3f));
            Calendar(p, "Calendar_Lights", new Vector3(-3.84f, 1.6f, 5f), Quaternion.Euler(0f, -90f, 0f), "POWER FAILED", LevelItems.Days[0], Color.black);
            Box(p, "Crate_Break", new Vector3(-2.5f, 0.5f, 1.5f), new Vector3(1f, 1f, 1f), _prop);

            // Hall: tape on a counter, calendar with the circled date
            Box(p, "Counter_Hall", new Vector3(-3.5f, 0.5f, 14f), new Vector3(1.2f, 1f, 3f), _prop);
            MakeNote(p, db, LevelItems.TapeId, new Vector3(-3.5f, 1.04f, 14f));
            Calendar(p, "Calendar_Dock", new Vector3(3f, 1.6f, 25.84f), Quaternion.identity, "LOADING DOCK", "(" + LevelItems.Days[2] + ")", new Color(0.75f, 0.1f, 0.1f));

            // Stacks: shelf rows, ledger with the second date, and the stamp on the far shelf
            foreach (float x in new[] { -9f, -12f, -15f })
                Box(p, "Shelf_" + x, new Vector3(x, 1f, 17.2f), new Vector3(0.6f, 2f, 6.4f), _prop);
            Box(p, "LedgerStand", new Vector3(-8f, 0.45f, 22.6f), new Vector3(1.4f, 0.9f, 0.8f), _prop);
            MakeNote(p, db, LevelItems.LedgerId, new Vector3(-8f, 0.94f, 22.6f));
            Calendar(p, "LedgerSpine", new Vector3(-8f, 1.5f, 23.84f), Quaternion.identity, "LEDGER SEALED", LevelItems.Days[1], Color.black);
            Box(p, "FarShelf", new Vector3(-17.4f, 0.6f, 18f), new Vector3(0.8f, 1.2f, 2.4f), _prop);
            var stamp = Box(p, "BrassStamp", new Vector3(-17.3f, 1.3f, 18f), new Vector3(0.25f, 0.2f, 0.25f), _brass);
            var pickup = stamp.AddComponent<Pickup>();
            pickup.ItemId = LevelItems.StampId;
            pickup.DisplayName = LevelItems.Get(db, LevelItems.StampId).DisplayName;

            // Records Office: desk and the keypad
            Box(p, "Desk_Records", new Vector3(12f, 0.4f, 17f), new Vector3(2.4f, 0.8f, 1f), _prop);
            var pad = Box(p, "Keypad", new Vector3(12f, 1.3f, 23.8f), new Vector3(0.7f, 1f, 0.2f), _paper);
            AddBoardText(pad.transform, "Keypad", "RECORDS OFFICE", "- - - -", Color.black, bigPercent: 130);
            finalLock = pad.AddComponent<CodeLock>();
            finalLock.Code = LevelItems.Code;
            finalLock.PuzzleId = "three_dates";
            finalLock.TargetDoor = codeDoor;
            return root;
        }

        static void MakeNote(Transform parent, ItemDatabase db, string id, Vector3 pos)
        {
            var def = LevelItems.Get(db, id);
            var go = Box(parent, "Note_" + id, pos, new Vector3(0.3f, 0.02f, 0.4f), _paper);
            var note = go.AddComponent<Note>();
            note.ItemId = id;
            note.Title = def.DisplayName;
            note.Body = def.Body;
        }

        static void Calendar(Transform parent, string name, Vector3 pos, Quaternion rot, string caption, string day, Color dayColor)
        {
            var board = Box(parent, name, pos, new Vector3(0.9f, 1.1f, 0.05f), _paper);
            board.transform.rotation = rot;
            AddBoardText(board.transform, name, caption, day, dayColor);
        }

        /// <summary>World-space text on the -Z face of a board, sized so a two-digit number fits.</summary>
        static void AddBoardText(Transform board, string name, string caption, string big, Color color, int bigPercent = 250)
        {
            var textGo = new GameObject(name + "_Text", typeof(RectTransform));
            textGo.transform.SetParent(board, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.6f);   // in front of the board face, in the board's local units
            var s = board.localScale;
            textGo.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);   // cancel the board's non-uniform scale
            var tmp = textGo.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.fontSize = 2f;
            tmp.text = $"<size=40%>{caption}</size>\n<size={bigPercent}%><b>{big}</b></size>";
            ((RectTransform)textGo.transform).sizeDelta = new Vector2(0.85f, 1f);
        }

        // ---------- player, lights, checkpoints ----------

        static GameObject BuildPlayer(ItemDatabase db, out Interactor interactor, out PlayerInventory inventory)
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(0f, 0.1f, 2f);
            player.AddComponent<CharacterController>();
            var pivot = new GameObject("CameraPivot").transform;
            pivot.SetParent(player.transform, false);
            var cam = new GameObject("Main Camera");
            cam.tag = "MainCamera";
            var camera = cam.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cam.AddComponent<AudioListener>();
            cam.transform.SetParent(pivot, false);

            var motor = player.AddComponent<PlayerMotor>();
            motor.CameraPivot = pivot;
            player.AddComponent<PlayerLook>().CameraPivot = pivot;

            inventory = player.AddComponent<PlayerInventory>();
            inventory.Database = db;
            interactor = cam.AddComponent<Interactor>();
            interactor.ViewCamera = camera;
            return player;
        }

        static void BuildLights()
        {
            var parent = new GameObject("Lights").transform;
            foreach (var (name, x0, x1, z0, z1) in Areas)
            {
                var l = new GameObject("Light_" + name).AddComponent<Light>();
                l.transform.SetParent(parent, false);
                l.type = LightType.Point;
                l.color = new Color(1f, 0.85f, 0.65f);
                l.intensity = 7f;
                l.range = Mathf.Max(x1 - x0, z1 - z0) * 0.9f;
                l.transform.position = new Vector3((x0 + x1) / 2f, 2.6f, (z0 + z1) / 2f);
                LevelLighting.ConfigureLight(l, name);
            }
        }

        static CheckpointTrigger[] BuildCheckpoints()
        {
            var parent = new GameObject("Checkpoints").transform;
            return new[]
            {
                Checkpoint(parent, "entrance", new Vector3(0f, 0f, 2f), new Vector3(4f, 2.5f, 3f)),
                Checkpoint(parent, "hall", new Vector3(0f, 0f, 13f), new Vector3(5f, 2.5f, 3f)),
                Checkpoint(parent, "records", new Vector3(10f, 0f, 16f), new Vector3(3f, 2.5f, 3f)),
                Checkpoint(parent, "dock", new Vector3(0f, 0f, 29f), new Vector3(2.5f, 2.5f, 2f)),
            };
        }

        static CheckpointTrigger Checkpoint(Transform parent, string id, Vector3 floorPos, Vector3 size)
        {
            var go = new GameObject("Checkpoint_" + id, typeof(BoxCollider));
            go.transform.SetParent(parent, false);
            go.transform.position = floorPos + Vector3.up * (size.y / 2f);
            var col = go.GetComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            var cp = go.AddComponent<CheckpointTrigger>();
            cp.Id = id;
            var spawn = new GameObject("Spawn").transform;
            spawn.SetParent(go.transform, false);
            spawn.position = floorPos + Vector3.up * 0.1f;
            cp.SpawnPoint = spawn;
            return cp;
        }

        // ---------- geometry helpers ----------

        /// <summary>Wall along the X axis at depth z, from x0 to x1, with an optional doorway centred on gapX.</summary>
        static void WallX(Transform parent, string name, float z, float x0, float x1, float? gapX = null)
        {
            if (gapX == null) { Box(parent, name, new Vector3((x0 + x1) / 2f, WallH / 2f, z), new Vector3(x1 - x0, WallH, WallT), _wall); return; }
            float g0 = gapX.Value - DoorW / 2f, g1 = gapX.Value + DoorW / 2f;
            Box(parent, name + "_a", new Vector3((x0 + g0) / 2f, WallH / 2f, z), new Vector3(g0 - x0, WallH, WallT), _wall);
            Box(parent, name + "_b", new Vector3((g1 + x1) / 2f, WallH / 2f, z), new Vector3(x1 - g1, WallH, WallT), _wall);
            Box(parent, name + "_lintel", new Vector3(gapX.Value, (DoorH + WallH) / 2f, z), new Vector3(DoorW, WallH - DoorH, WallT), _wall);
        }

        /// <summary>Wall along the Z axis at x, from z0 to z1, with an optional doorway centred on gapZ.</summary>
        static void WallZ(Transform parent, string name, float x, float z0, float z1, float? gapZ = null)
        {
            if (gapZ == null) { Box(parent, name, new Vector3(x, WallH / 2f, (z0 + z1) / 2f), new Vector3(WallT, WallH, z1 - z0), _wall); return; }
            float g0 = gapZ.Value - DoorW / 2f, g1 = gapZ.Value + DoorW / 2f;
            Box(parent, name + "_a", new Vector3(x, WallH / 2f, (z0 + g0) / 2f), new Vector3(WallT, WallH, g0 - z0), _wall);
            Box(parent, name + "_b", new Vector3(x, WallH / 2f, (g1 + z1) / 2f), new Vector3(WallT, WallH, z1 - g1), _wall);
            Box(parent, name + "_lintel", new Vector3(x, (DoorH + WallH) / 2f, gapZ.Value), new Vector3(WallT, WallH - DoorH, DoorW), _wall);
        }

        /// <summary>A hinged door filling a doorway. The hinge is a child so the Door can rotate it without losing the base orientation.</summary>
        /// <remarks>The sign of openAngle picks the swing side: every door is set to swing away from the side the player approaches from.</remarks>
        static Door MakeDoor(Transform parent, string name, Vector3 doorwayCentre, bool alongX, string key, float openAngle)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = doorwayCentre;
            root.rotation = Quaternion.Euler(0f, alongX ? 0f : -90f, 0f);

            var hinge = new GameObject("Hinge").transform;
            hinge.SetParent(root, false);
            hinge.localPosition = new Vector3(-DoorW / 2f, 0f, 0f);

            var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leaf.name = "Leaf";
            leaf.transform.SetParent(hinge, false);
            leaf.transform.localPosition = new Vector3(DoorW / 2f, DoorH / 2f, 0f);
            leaf.transform.localScale = new Vector3(DoorW, DoorH, 0.12f);
            leaf.GetComponent<Renderer>().sharedMaterial = _prop;

            var door = root.gameObject.AddComponent<Door>();
            door.Hinge = hinge;
            door.RequiredKeyId = key;
            door.OpenAngle = openAngle;
            return door;
        }

        static GameObject Box(Transform parent, string name, Vector3 centre, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static void MakeMaterials()
        {
            Directory.CreateDirectory(MatDir);
            _wall = Mat("Wall", new Color(0.32f, 0.33f, 0.35f));
            _floor = Mat("Floor", new Color(0.18f, 0.17f, 0.16f));
            _prop = Mat("Prop", new Color(0.30f, 0.22f, 0.15f));
            _paper = Mat("Paper", new Color(0.85f, 0.82f, 0.7f));
            _brass = Mat("Brass", new Color(0.72f, 0.55f, 0.2f));
        }

        static Material Mat(string name, Color color)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.color = color;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
