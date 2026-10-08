using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using NocturneAnnex.Entity;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.Editor
{
    /// <summary>
    /// Adds the Stilt-Walker and its noise plumbing to a generated level: NoiseHub, runtime NavMesh, player and door noise,
    /// the entity itself (greybox body, wooden leg, big eyes) and its patrol points. Stand-in footstep clips come from the
    /// CC0 clips already in the project; real audio replaces them later (F-14c).
    /// </summary>
    public static class StalkerBuilder
    {
        const string SfxDir = "Assets/_Project/Audio/Sfx/";

        public static StalkerAgent Build(GameObject player, Transform geometry, Vector3 spawn, Vector3[] patrol)
        {
            var hub = new GameObject("NoiseHub").AddComponent<NoiseHub>();
            var nav = new GameObject("LevelNavMesh").AddComponent<LevelNavMesh>();
            nav.Player = player.transform;
            nav.transform.position = new Vector3(0f, 0f, 20f);

            var emitter = player.AddComponent<PlayerNoiseEmitter>();
            emitter.Hub = hub;
            foreach (var door in Object.FindObjectsByType<Door>(FindObjectsSortMode.None))
                door.gameObject.AddComponent<DoorNoise>().Hub = hub;

            var root = new GameObject("Stilt-Walker");
            root.transform.position = spawn;
            var skin = Mat("Stalker_Skin", new Color(0.62f, 0.62f, 0.58f));
            var cloth = Mat("Stalker_Coat", new Color(0.08f, 0.08f, 0.09f));
            var wood = Mat("Stalker_Wood", new Color(0.25f, 0.15f, 0.08f));
            var eyeMat = Mat("Stalker_Eye", new Color(0.02f, 0.02f, 0.02f));

            Part(root.transform, PrimitiveType.Capsule, "Torso", new Vector3(0f, 1.55f, 0f), new Vector3(0.5f, 0.75f, 0.35f), cloth);
            Part(root.transform, PrimitiveType.Capsule, "ArmL", new Vector3(-0.38f, 1.15f, 0f), new Vector3(0.1f, 0.65f, 0.1f), skin);
            Part(root.transform, PrimitiveType.Capsule, "ArmR", new Vector3(0.38f, 1.15f, 0f), new Vector3(0.1f, 0.65f, 0.1f), skin);
            Part(root.transform, PrimitiveType.Cylinder, "LegNormal", new Vector3(-0.15f, 0.55f, 0f), new Vector3(0.18f, 0.55f, 0.18f), cloth);
            Part(root.transform, PrimitiveType.Cylinder, "LegWooden", new Vector3(0.15f, 0.5f, 0f), new Vector3(0.12f, 0.5f, 0.12f), wood);
            var head = Part(root.transform, PrimitiveType.Sphere, "Head", new Vector3(0f, 2.3f, 0.05f), new Vector3(0.38f, 0.45f, 0.4f), skin);
            Part(head.transform, PrimitiveType.Sphere, "EyeL", new Vector3(-0.2f, 0.1f, 0.42f), new Vector3(0.3f, 0.34f, 0.2f), eyeMat);
            Part(head.transform, PrimitiveType.Sphere, "EyeR", new Vector3(0.2f, 0.1f, 0.42f), new Vector3(0.3f, 0.34f, 0.2f), eyeMat);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f; agent.height = 2.5f; agent.acceleration = 8f; agent.angularSpeed = 0f;
            agent.stoppingDistance = 0.5f;
            var stalker = root.AddComponent<StalkerAgent>();
            stalker.Hub = hub;
            stalker.Player = player.transform;
            stalker.Eye = head.transform;
            stalker.NormalSteps = Clips("footstep00", "footstep01", "footstep02", "footstep03");
            stalker.WoodSteps = Clips("doorClose_1", "doorClose_2", "doorClose_3", "doorClose_4");

            var points = new GameObject("PatrolPoints").transform;
            stalker.PatrolPoints = patrol.Select((p, i) =>
            {
                var t = new GameObject("P" + i).transform;
                t.SetParent(points, false);
                t.position = p;
                return t;
            }).ToArray();
            HideSpotAt(geometry, "Cupboard_Stacks", new Vector3(-16.5f, 0f, 12.7f), new Vector3(1.4f, 2.2f, 0.9f), new Vector3(0f, 0f, 1.3f), 0.8f, hub, stalker);
            HideSpotAt(geometry, "Cupboard_Hall", new Vector3(5.4f, 0f, 11.5f), new Vector3(0.9f, 2.2f, 1.4f), new Vector3(-1.3f, 0f, 0f), 0.3f, hub, stalker);
            HideSpotAt(geometry, "Cupboard_Records", new Vector3(17.2f, 0f, 22.5f), new Vector3(0.9f, 2.2f, 1.4f), new Vector3(-1.3f, 0f, 0f), 0.55f, hub, stalker);
            return stalker;
        }

        /// <summary>A solid cupboard the player can hide in: hide point inside it, exit point in front of it.</summary>
        static void HideSpotAt(Transform parent, string name, Vector3 floorPos, Vector3 size, Vector3 exitOffset, float safety, NoiseHub hub, StalkerAgent stalker)
        {
            var box = LevelB1Builder.Box(parent, name, floorPos + Vector3.up * size.y / 2f, size, Mat("Cupboard", new Color(0.22f, 0.15f, 0.1f)));
            var spot = box.AddComponent<HideSpot>();
            spot.Safety = safety; spot.Hub = hub; spot.Stalker = stalker;
            var hide = new GameObject("HidePoint").transform;
            hide.SetParent(box.transform, true);
            hide.position = floorPos + Vector3.up * 0.1f;
            var exit = new GameObject("ExitPoint").transform;
            exit.SetParent(box.transform, true);
            exit.position = floorPos + exitOffset + Vector3.up * 0.1f;
            exit.rotation = Quaternion.LookRotation(exitOffset.normalized);
            spot.HidePoint = hide; spot.ExitPoint = exit;
        }

        static GameObject Part(Transform parent, PrimitiveType type, string name, Vector3 local, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());   // the body must not block the player or the sight line
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static AudioClip[] Clips(params string[] names) =>
            names.Select(n => AssetDatabase.LoadAssetAtPath<AudioClip>(SfxDir + n + ".ogg")).Where(c => c != null).ToArray();

        static Material Mat(string name, Color color) => LevelB1Builder.Mat(name, color);
    }
}
