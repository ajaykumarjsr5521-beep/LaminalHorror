using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using NocturneAnnex.Audio;
using NocturneAnnex.Controls;
using NocturneAnnex.Level;
using NocturneAnnex.Liminal;
using NocturneAnnex.Save;

namespace NocturneAnnex.Editor
{
    /// <summary>
    /// Real-scale greybox of Hotel Meridian (F-13b): a tall lit atrium with a mezzanine ring, two stairs, landmarks and a long east wing.
    /// Primitives only; the point is to judge scale, sightlines and light before art. Run from Build > Create Hotel Meridian Scene.
    /// </summary>
    public static class HotelBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Hotel_Meridian.unity";
        public const string LightingPath = "Assets/_Project/Settings/Hotel_Meridian.lighting";

        // lobby atrium
        const float HalfW = 15f, Depth = 46f, AtriumH = 12f, MezzY = 5.5f, MezzDepth = 4f, Wall = 0.5f;
        // east wing
        const float WingZ0 = 18f, WingZ1 = 22f, WingLen = 56f, WingH = 3.2f, DoorSpacing = 6f;

        static Material _wall, _floor, _carpet, _ceiling, _trim, _glow, _sign, _night;

        [MenuItem("Build/Create Hotel Meridian Scene")]
        public static void Create()
        {
            LevelAudio.CreateAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MakeMaterials();
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.30f, 0.29f, 0.27f);   // bright: the wrongness must be visible
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.55f, 0.52f, 0.46f);
            RenderSettings.fogDensity = 0.012f;

            var shell = new GameObject("Shell").transform;
            BuildAtrium(shell);
            BuildWing(shell);
            LevelLighting.MarkShellStatic(shell);
            BuildLights();

            new GameObject("InputRouter").AddComponent<InputRouter>();
            var db = AssetDatabase.LoadAssetAtPath<NocturneAnnex.Inventory.ItemDatabase>(LevelItems.DatabasePath);
            var player = LevelB1Builder.BuildPlayer(db, out var interactor, out var inventory);
            player.transform.position = new Vector3(0f, 0.1f, 3f);
            LevelAudio.Build(player);
            BuildLiminal(player);

            var cps = new[] { LevelB1Builder.Checkpoint(new GameObject("Checkpoints").transform, "lobby", new Vector3(0f, 0f, 3f), new Vector3(6f, 2.5f, 3f)) };
            var save = new GameObject("SaveGame").AddComponent<SaveGame>();
            save.Inventory = inventory;
            save.Locks = new NocturneAnnex.Puzzle.CodeLock[0];
            var level = new GameObject("LevelBootstrap").AddComponent<LevelBootstrap>();
            level.Checkpoints = cps;
            level.SaveGame = save;
            level.Player = player.transform;
            LevelHudBuilder.Build(interactor, inventory, null, level);

            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!LevelLighting.Bake(LightingPath, 2f)) Debug.LogError("Hotel_Meridian lightmap bake did not finish.");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Hotel_Meridian written to " + Path.GetFullPath(ScenePath));
        }

        // ---------- structure ----------

        static void BuildAtrium(Transform p)
        {
            B(p, "Floor_Lobby", new Vector3(0f, -0.25f, Depth / 2f), new Vector3(HalfW * 2f, 0.5f, Depth), _floor);
            B(p, "Ceiling_Lobby", new Vector3(0f, AtriumH + 0.25f, Depth / 2f), new Vector3(HalfW * 2f, 0.5f, Depth), _ceiling);
            B(p, "Skylight", new Vector3(0f, AtriumH - 0.02f, Depth / 2f), new Vector3(10f, 0.05f, Depth - 6f), _glow);

            // front: glass wall with a glowing night view
            B(p, "FrontGlass", new Vector3(0f, AtriumH / 2f, -Wall / 2f), new Vector3(HalfW * 2f, AtriumH, Wall), _night);
            // back wall with the elevator bank and the sign
            B(p, "BackWall", new Vector3(0f, AtriumH / 2f, Depth + Wall / 2f), new Vector3(HalfW * 2f, AtriumH, Wall), _wall);
            B(p, "Sign_Hotel", new Vector3(0f, 9f, Depth - 0.1f), new Vector3(9f, 1.6f, 0.1f), _sign);
            for (int i = 0; i < 3; i++)
            {
                float x = -9f + i * 3f;
                B(p, "ElevatorDoor_" + i, new Vector3(x, 1.5f, Depth - 0.1f), new Vector3(2.2f, 3f, 0.1f), _trim);
                B(p, "ElevatorLight_" + i, new Vector3(x, 3.4f, Depth - 0.1f), new Vector3(1f, 0.15f, 0.1f), _glow);
            }
            B(p, "Reception", new Vector3(5f, 0.6f, Depth - 6f), new Vector3(9f, 1.2f, 1.6f), _trim);

            // side walls (east wall is open for the wing)
            B(p, "WestWall", new Vector3(-HalfW - Wall / 2f, AtriumH / 2f, Depth / 2f), new Vector3(Wall, AtriumH, Depth), _wall);
            B(p, "EastWall_a", new Vector3(HalfW + Wall / 2f, AtriumH / 2f, WingZ0 / 2f), new Vector3(Wall, AtriumH, WingZ0), _wall);
            B(p, "EastWall_b", new Vector3(HalfW + Wall / 2f, AtriumH / 2f, (WingZ1 + Depth) / 2f), new Vector3(Wall, AtriumH, Depth - WingZ1), _wall);
            B(p, "EastWall_lintel", new Vector3(HalfW + Wall / 2f, (WingH + AtriumH) / 2f, (WingZ0 + WingZ1) / 2f), new Vector3(Wall, AtriumH - WingH, WingZ1 - WingZ0), _wall);

            // mezzanine ring: west, east (broken by the wing opening), back
            B(p, "Mezz_West", new Vector3(-HalfW + MezzDepth / 2f, MezzY - 0.25f, Depth / 2f), new Vector3(MezzDepth, 0.5f, Depth), _carpet);
            B(p, "Mezz_East", new Vector3(HalfW - MezzDepth / 2f, MezzY - 0.25f, Depth / 2f), new Vector3(MezzDepth, 0.5f, Depth), _carpet);
            B(p, "Mezz_Back", new Vector3(0f, MezzY - 0.25f, Depth - MezzDepth / 2f), new Vector3(HalfW * 2f - MezzDepth * 2f, 0.5f, MezzDepth), _carpet);
            B(p, "Rail_West", new Vector3(-HalfW + MezzDepth, MezzY + 0.5f, Depth / 2f), new Vector3(0.1f, 1f, Depth), _trim);
            B(p, "Rail_East", new Vector3(HalfW - MezzDepth, MezzY + 0.5f, Depth / 2f), new Vector3(0.1f, 1f, Depth), _trim);
            B(p, "Rail_Back", new Vector3(0f, MezzY + 0.5f, Depth - MezzDepth), new Vector3(HalfW * 2f - MezzDepth * 2f, 1f, 0.1f), _trim);

            // columns every 8 m on both sides
            for (float z = 6f; z < Depth; z += 8f)
                foreach (float x in new[] { -HalfW + MezzDepth, HalfW - MezzDepth })
                    B(p, $"Column_{x}_{z}", new Vector3(x, AtriumH / 2f, z), new Vector3(0.9f, AtriumH, 0.9f), _trim);

            // two stairs up to the mezzanine, along the side walls from the front
            Stair(p, "Stair_West", -HalfW + 1.5f, 3f);
            Stair(p, "Stair_East", HalfW - 1.5f, 3f);
        }

        static void Stair(Transform p, string name, float x, float z0)
        {
            const float rise = 0.18f, run = 0.3f;
            int steps = Mathf.CeilToInt(MezzY / rise);
            for (int i = 0; i < steps; i++)
                B(p, $"{name}_{i}", new Vector3(x, (i + 1) * rise - rise / 2f, z0 + i * run + run / 2f), new Vector3(2.5f, rise, run), _carpet).transform.localScale = new Vector3(2.5f, (i + 1) * rise, run);
            float endZ = z0 + steps * run;
            B(p, name + "_landing", new Vector3(x, MezzY - 0.25f, endZ + 1f), new Vector3(2.5f, 0.5f, 2f), _carpet);
        }

        static void BuildWing(Transform p)
        {
            float zc = (WingZ0 + WingZ1) / 2f, w = WingZ1 - WingZ0, x0 = HalfW, x1 = HalfW + WingLen, xc = (x0 + x1) / 2f;
            B(p, "Floor_Wing", new Vector3(xc, -0.25f, zc), new Vector3(WingLen, 0.5f, w), _carpet);
            B(p, "Ceiling_Wing", new Vector3(xc, WingH + 0.25f, zc), new Vector3(WingLen, 0.5f, w), _ceiling);
            B(p, "WingWall_S", new Vector3(xc, WingH / 2f, WingZ0 - Wall / 2f), new Vector3(WingLen, WingH, Wall), _wall);
            B(p, "WingWall_N", new Vector3(xc, WingH / 2f, WingZ1 + Wall / 2f), new Vector3(WingLen, WingH, Wall), _wall);
            B(p, "WingEnd", new Vector3(x1 + Wall / 2f, WingH / 2f, zc), new Vector3(Wall, WingH, w), _glow);   // bright end: a glass wall to somewhere
            for (float x = x0 + DoorSpacing; x < x1 - 2f; x += DoorSpacing)
            {
                B(p, $"DoorFrame_S_{x}", new Vector3(x, 1.1f, WingZ0 + 0.02f), new Vector3(1.1f, 2.2f, 0.1f), _trim);
                B(p, $"DoorFrame_N_{x}", new Vector3(x + DoorSpacing / 2f, 1.1f, WingZ1 - 0.02f), new Vector3(1.1f, 2.2f, 0.1f), _trim);
                B(p, $"RoomNo_{x}", new Vector3(x, 1.8f, WingZ0 + 0.03f), new Vector3(0.3f, 0.12f, 0.05f), _sign);
            }
        }

        static void BuildLights()
        {
            var parent = new GameObject("Lights").transform;
            // two realtime lights: the atrium chandelier and the wing's last light (the ones events may flicker)
            Light(parent, "Light_Atrium", new Vector3(0f, 9f, Depth / 2f), 12f, 16f, true);
            Light(parent, "Light_WingEnd", new Vector3(HalfW + WingLen - 4f, 2.9f, (WingZ0 + WingZ1) / 2f), 4f, 9f, true);
            // baked fill down the middle of the atrium so the floor reads
            for (float z = 6f; z < Depth; z += 8f)
                Light(parent, "Light_Fill_" + z, new Vector3(0f, 4.5f, z), 9f, 13f, false);
            // baked: mezzanine and wing
            foreach (float z in new[] { 8f, 20f, 32f, 42f })
            {
                Light(parent, "Light_MezzW_" + z, new Vector3(-HalfW + 2f, MezzY + 3f, z), 6f, 10f, false);
                Light(parent, "Light_MezzE_" + z, new Vector3(HalfW - 2f, MezzY + 3f, z), 6f, 10f, false);
            }
            for (float x = HalfW + 7f; x < HalfW + WingLen - 6f; x += 12f)
                Light(parent, "Light_Wing_" + x, new Vector3(x, 2.9f, (WingZ0 + WingZ1) / 2f), 3f, 8f, false);
        }

        static void Light(Transform parent, string name, Vector3 pos, float intensity, float range, bool realtime)
        {
            var l = new GameObject(name).AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.position = pos;
            l.type = LightType.Point;
            l.color = new Color(1f, 0.88f, 0.7f);
            l.intensity = intensity;
            l.range = range;
            l.lightmapBakeType = realtime ? LightmapBakeType.Realtime : LightmapBakeType.Baked;
        }

        static void BuildLiminal(GameObject player)
        {
            var root = new GameObject("Liminal");
            var director = root.AddComponent<LiminalDirector>();
            // legend notes: the Guest's folklore, one by the reception, one in the wing, one on the mezzanine
            Legend(root.transform, director, "guest_rule_room", "Front desk memo", "Do not check in to a room you did not book. The night manager says the key cards beep in the next corridor first.", new Vector3(5f, 1.3f, Depth - 6f), 2);
            Legend(root.transform, director, "guest_mirror", "Housekeeping note", "Mirrors on the 4th floor. If the reflection moves before you do, stand still until it stops.", new Vector3(HalfW + 12f, 1.1f, WingZ0 + 0.6f), 2);
            Legend(root.transform, director, "guest_register", "Register page", "Room 237: booked, paid, never occupied. The guest checks in every night anyway.", new Vector3(-HalfW + 2f, MezzY + 1.1f, 30f), 3);
            // the silhouette in the glass behind the spawn point
            var figure = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            figure.name = "GuestSilhouette";
            figure.transform.SetParent(root.transform, false);
            figure.transform.position = new Vector3(6f, 1f, 1.2f);
            figure.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            Object.DestroyImmediate(figure.GetComponent<Collider>());
            figure.GetComponent<Renderer>().sharedMaterial = LevelB1Builder.Mat("Hotel_Silhouette", new Color(0.02f, 0.02f, 0.03f));
            var guest = root.AddComponent<GuestManifestation>();
            guest.Director = director;
            guest.Figure = figure;
            guest.ViewCamera = player.GetComponentInChildren<Camera>();
        }

        static void Legend(Transform parent, LiminalDirector director, string id, string title, string body, Vector3 pos, int points)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Legend_" + id;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.3f, 0.02f, 0.22f);
            go.GetComponent<Renderer>().sharedMaterial = _glow;
            var pick = go.AddComponent<LegendPickup>();
            pick.EntryId = id; pick.Title = title; pick.Body = body; pick.EvidencePoints = points; pick.Director = director;
        }

        static GameObject B(Transform p, string name, Vector3 c, Vector3 s, Material m) => LevelB1Builder.Box(p, name, c, s, m);

        static void MakeMaterials()
        {
            _wall = LevelB1Builder.Mat("Hotel_Wall", new Color(0.78f, 0.72f, 0.62f));
            _floor = LevelB1Builder.Mat("Hotel_Floor", new Color(0.55f, 0.50f, 0.44f));
            _carpet = LevelB1Builder.Mat("Hotel_Carpet", new Color(0.48f, 0.16f, 0.18f));
            _ceiling = LevelB1Builder.Mat("Hotel_Ceiling", new Color(0.85f, 0.83f, 0.78f));
            _trim = LevelB1Builder.Mat("Hotel_Trim", new Color(0.30f, 0.20f, 0.12f));
            _glow = Emissive("Hotel_Glow", new Color(1f, 0.95f, 0.8f), 0.9f);
            _sign = Emissive("Hotel_Sign", new Color(0.95f, 0.65f, 0.2f), 1.4f);
            _night = Emissive("Hotel_Night", new Color(0.18f, 0.28f, 0.5f), 0.35f);
        }

        static Material Emissive(string name, Color color, float intensity)
        {
            var m = LevelB1Builder.Mat(name, color);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * intensity);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
