using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using NocturneAnnex.Horror;
using NocturneAnnex.Interaction;
using NocturneAnnex.Level;
using NocturneAnnex.Save;

namespace NocturneAnnex.Editor
{
    /// <summary>Generates the six horror event assets for Floor B1 and wires them, safe zones and the runner into the level scene.</summary>
    public static class LevelEvents
    {
        public const string Dir = LevelItems.DataDir + "/Events";

        static readonly (string id, HorrorEventKind kind, bool once, float cooldown, float minTension, string caption, float duration, float strength, ScareTag tag)[] Defs =
        {
            ("flicker_hall", HorrorEventKind.LightFlicker, false, 90f, 0.55f, "event.light_buzz", 3f, 0.85f, ScareTag.Harmless),
            ("whisper_stacks", HorrorEventKind.AudioCue, false, 120f, 0.5f, "event.whisper", 1f, 0f, ScareTag.Harmless),
            ("prop_shift_counter", HorrorEventKind.PropShift, true, 0f, 0.65f, "event.prop_shift", 0f, 0f, ScareTag.Neutral),
            ("door_slam_stacks", HorrorEventKind.DoorSlam, true, 0f, 0.7f, "event.door_slam", 1f, 0.8f, ScareTag.Neutral),
            ("shadow_hall", HorrorEventKind.ShadowFigure, true, 0f, 0.75f, "event.figure", 1.5f, 0.6f, ScareTag.Neutral),
            ("misfile_alcove", HorrorEventKind.MisfileReveal, true, 0f, 0.8f, "event.misfile", 0f, 0f, ScareTag.Neutral),
            // F-14j
            ("mannequin_stacks", HorrorEventKind.Mannequin, false, 180f, 0.6f, "event.mannequin", 0f, 0f, ScareTag.Neutral),
            ("light_off_hall", HorrorEventKind.LightOff, false, 150f, 0.6f, "event.light_off", 3.5f, 0f, ScareTag.Harmless),
            ("door_open_break", HorrorEventKind.DoorOpen, false, 200f, 0.5f, "event.door_open", 0f, 0f, ScareTag.Harmless),
            ("whisper_behind_hall", HorrorEventKind.WhisperBehind, false, 150f, 0.55f, "event.whisper_behind", 1f, 0f, ScareTag.Harmless),
            ("false_steps_stacks", HorrorEventKind.FalseFootsteps, false, 150f, 0.65f, "event.false_steps", 0f, 0f, ScareTag.EntityLinked),
            ("sighting_dock", HorrorEventKind.ShadowFigure, true, 0f, 0.8f, "event.sighting", 1f, 0.5f, ScareTag.EntityLinked),
        };

        /// <summary>Repeatable events shared by rooms 2-10. Not "once", so their saved flags never block a later room.</summary>
        static readonly (string id, HorrorEventKind kind, float cooldown, float minTension, string caption, float duration, float strength, ScareTag tag)[] RoomDefs =
        {
            ("room_flicker", HorrorEventKind.LightFlicker, 100f, 0.55f, "event.light_buzz", 3f, 0.85f, ScareTag.Harmless),
            ("room_light_off", HorrorEventKind.LightOff, 160f, 0.6f, "event.light_off", 3.5f, 0f, ScareTag.Harmless),
            ("room_whisper_behind", HorrorEventKind.WhisperBehind, 150f, 0.55f, "event.whisper_behind", 1f, 0f, ScareTag.Harmless),
            ("room_false_steps", HorrorEventKind.FalseFootsteps, 150f, 0.65f, "event.false_steps", 0f, 0f, ScareTag.EntityLinked),
            ("room_door_open", HorrorEventKind.DoorOpen, 200f, 0.5f, "event.door_open", 0f, 0f, ScareTag.Harmless),
        };

        /// <summary>Call before the scene is created: scene creation unloads unused assets.</summary>
        public static void CreateAssets()
        {
            Directory.CreateDirectory(Dir);
            foreach (var d in Defs)
            {
                string path = $"{Dir}/Event_{d.id}.asset";
                var a = AssetDatabase.LoadAssetAtPath<HorrorEventAsset>(path);
                if (a == null)
                {
                    a = ScriptableObject.CreateInstance<HorrorEventAsset>();
                    AssetDatabase.CreateAsset(a, path);
                }
                a.Id = d.id; a.Kind = d.kind; a.Once = d.once; a.CooldownSeconds = d.cooldown; a.MinTension = d.minTension;
                a.CaptionKey = d.caption; a.DurationSeconds = d.duration; a.Strength = d.strength; a.Tag = d.tag;
                EditorUtility.SetDirty(a);
            }
            foreach (var d in RoomDefs)
            {
                string path = $"{Dir}/Event_{d.id}.asset";
                var a = AssetDatabase.LoadAssetAtPath<HorrorEventAsset>(path);
                if (a == null)
                {
                    a = ScriptableObject.CreateInstance<HorrorEventAsset>();
                    AssetDatabase.CreateAsset(a, path);
                }
                a.Id = d.id; a.Kind = d.kind; a.Once = false; a.CooldownSeconds = d.cooldown; a.MinTension = d.minTension;
                a.CaptionKey = d.caption; a.DurationSeconds = d.duration; a.Strength = d.strength; a.Tag = d.tag;
                EditorUtility.SetDirty(a);
            }
            AssetDatabase.SaveAssets();
        }

        static HorrorEventAsset Load(string id) => AssetDatabase.LoadAssetAtPath<HorrorEventAsset>($"{Dir}/Event_{id}.asset");

        /// <summary>Call after the level objects exist. Scene objects are found by the names LevelB1Builder gives them.</summary>
        public static void Build(LevelBootstrap level, SaveGame save, Camera playerCamera)
        {
            var root = new GameObject("Horror");
            var runner = root.AddComponent<HorrorEventRunner>();
            root.AddComponent<HorrorDebugOverlay>().Runner = runner;
            runner.Shake = playerCamera.gameObject.AddComponent<CameraShake>();

            var spots = new List<HorrorEventSpot>
            {
                Spot(root, "flicker_hall", s => s.Lights = new[] { Find<Light>("Light_Hall"), Find<Light>("Light_Stacks") }),
                Spot(root, "whisper_stacks", s => s.Audio = AudioAt("WhisperSource", new Vector3(-12f, 1.5f, 13f))),
                Spot(root, "prop_shift_counter", s => { s.Prop = Find<Transform>("Counter_Hall"); s.PropOffset = new Vector3(0.5f, 0f, 0f); }),
                Spot(root, "door_slam_stacks", s => s.Door = Find<Door>("Door_HallToStacks")),
                Spot(root, "shadow_hall", s => s.Figure = Figure()),
                Spot(root, "misfile_alcove", s => s.Reveal = Alcove()),
                Spot(root, "mannequin_stacks", s => { s.Prop = Mannequin(); s.Listener = playerCamera.transform; }),
                Spot(root, "light_off_hall", s => s.Lights = new[] { Find<Light>("Light_Hall") }),
                Spot(root, "door_open_break", s => s.Door = Find<Door>("Door_BreakToHall")),
                Spot(root, "whisper_behind_hall", s => { s.Audio = AudioAt("WhisperBehindSource", new Vector3(0f, 1.5f, 14f)); s.Listener = playerCamera.transform; }),
                Spot(root, "false_steps_stacks", s => s.transform.position = new Vector3(-17f, 0.1f, 22f)),
                Spot(root, "sighting_dock", s => s.Figure = SightingFigure()),
            };
            runner.Spots = spots.ToArray();
            LinkEntityScares(root, runner, spots);

            runner.SafeZones = new[]
            {
                Zone(root, "SafeZone_BreakRoom", new Vector3(0f, 1.5f, 5f), new Vector3(7.6f, 3f, 9.6f)),
                Zone(root, "SafeZone_Records", new Vector3(12f, 1.5f, 18f), new Vector3(11.6f, 3f, 11.6f)),
            };

            level.Horror = runner;
            save.Horror = runner;
            LinkHaptics(runner);
        }

        /// <summary>Generic scares for a generated room: flicker, lights out, a whisper behind the player, false footsteps and a self-opening back door.</summary>
        public static void BuildForRoom(LevelBootstrap level, SaveGame save, Camera playerCamera, Light[] lights, Door backDoor, float roomDepth)
        {
            var root = new GameObject("Horror");
            var runner = root.AddComponent<HorrorEventRunner>();
            runner.Shake = playerCamera.gameObject.AddComponent<CameraShake>();
            var spots = new List<HorrorEventSpot>
            {
                Spot(root, "room_flicker", s => s.Lights = lights),
                Spot(root, "room_light_off", s => s.Lights = lights),
                Spot(root, "room_whisper_behind", s => { s.Audio = AudioAt("WhisperBehindSource", new Vector3(0f, 1.5f, roomDepth * 0.5f)); s.Listener = playerCamera.transform; }),
                Spot(root, "room_false_steps", s => s.transform.position = new Vector3(-11f, 0.1f, roomDepth * 0.5f)),
                Spot(root, "room_door_open", s => s.Door = backDoor),
            };
            runner.Spots = spots.ToArray();
            runner.SafeZones = new[] { Zone(root, "SafeZone_Start", new Vector3(0f, 1.5f, 3f), new Vector3(12f, 3f, 6f)) };
            LinkEntityScares(root, runner, spots);
            level.Horror = runner;
            save.Horror = runner;
            LinkHaptics(runner);
        }

        static void LinkHaptics(HorrorEventRunner runner)
        {
            var haptics = UnityEngine.Object.FindFirstObjectByType<NocturneAnnex.Entity.HapticsPlayer>();
            if (haptics != null) haptics.Horror = runner;
        }

        /// <summary>Entity-linked events also make a real noise at their spot, so the stalker may come to look.</summary>
        static void LinkEntityScares(GameObject root, HorrorEventRunner runner, List<HorrorEventSpot> spots)
        {
            var hub = UnityEngine.Object.FindFirstObjectByType<NocturneAnnex.Entity.NoiseHub>();
            if (hub == null) return;
            var link = root.AddComponent<NocturneAnnex.Entity.ScareNoiseLink>();
            link.Runner = runner; link.Hub = hub;
            link.Links = spots.Where(s => s.Event != null && s.Event.Tag == ScareTag.EntityLinked)
                .Select(s => new NocturneAnnex.Entity.ScareNoiseLink.Link { EventId = s.Event.Id, At = s.Figure != null ? s.Figure.transform : s.transform, Scale = 1f })
                .ToArray();
        }

        static Transform Mannequin()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Mannequin";
            go.transform.position = new Vector3(-10f, 1f, 22.5f);
            go.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Paper.mat");
            return go.transform;
        }

        static GameObject SightingFigure()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "SightingFigure";
            go.transform.position = new Vector3(0f, 1.1f, 39f);
            go.transform.localScale = new Vector3(0.55f, 1.1f, 0.55f);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Floor.mat");
            go.SetActive(false);
            return go;
        }

        static HorrorEventSpot Spot(GameObject parent, string id, Action<HorrorEventSpot> bind)
        {
            var go = new GameObject("Spot_" + id);
            go.transform.SetParent(parent.transform, false);
            var spot = go.AddComponent<HorrorEventSpot>();
            spot.Event = Load(id);
            bind(spot);
            return spot;
        }

        static T Find<T>(string name) where T : Component
        {
            var go = GameObject.Find(name);
            if (go == null) throw new InvalidOperationException($"Level object '{name}' not found for a horror event.");
            var c = go.GetComponent<T>();
            if (c == null) throw new InvalidOperationException($"'{name}' has no {typeof(T).Name}.");
            return c;
        }

        static AudioSource AudioAt(string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            return src;   // no clip yet: the caption still shows. Real audio arrives with F-10.
        }

        static SafeZone Zone(GameObject parent, string name, Vector3 centre, Vector3 size)
        {
            var go = new GameObject(name, typeof(BoxCollider));
            go.transform.SetParent(parent.transform, false);
            go.transform.position = centre;
            var col = go.GetComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            return go.AddComponent<SafeZone>();
        }

        static GameObject Figure()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "ShadowFigure";
            go.transform.position = new Vector3(-4.6f, 0.95f, 24.8f);
            go.transform.localScale = new Vector3(0.6f, 0.95f, 0.6f);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());   // a figure you cannot bump into or interact with
            go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Floor.mat");
            go.SetActive(false);
            return go;
        }

        static GameObject Alcove()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "MisfileAlcove";
            go.transform.position = new Vector3(5.75f, 1.1f, 22f);
            go.transform.localScale = new Vector3(0.1f, 2.2f, 1.2f);
            UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Floor.mat");
            go.SetActive(false);
            return go;
        }
    }
}
