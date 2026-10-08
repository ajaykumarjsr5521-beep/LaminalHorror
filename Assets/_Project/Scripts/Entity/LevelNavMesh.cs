using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.Entity
{
    /// <summary>
    /// Builds the level's NavMesh at start from the physics colliders, so no navigation package or baked asset is needed.
    /// Doors and the player are left out: the entity walks through doorways and opens or passes doors itself.
    /// </summary>
    public class LevelNavMesh : MonoBehaviour
    {
        public float AgentRadius = 0.45f;
        public float AgentHeight = 2.4f;
        public Vector3 Size = new Vector3(120f, 20f, 120f);
        public Transform Player;

        NavMeshData _data;
        NavMeshDataInstance _instance;

        public bool Built => _instance.valid;

        void Awake() => Build();

        public void Build()
        {
            if (Built) return;
            Physics.SyncTransforms();   // colliders created this frame must be at their final place
            var markups = new List<NavMeshBuildMarkup>();
            foreach (var d in FindObjectsByType<Door>(FindObjectsSortMode.None)) AddIgnore(markups, d.transform);
            if (Player != null) AddIgnore(markups, Player);
            var sources = new List<NavMeshBuildSource>();
            var bounds = new Bounds(transform.position, Size);
            NavMeshBuilder.CollectSources(bounds, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);

            var settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = AgentRadius;
            settings.agentHeight = AgentHeight;
            _data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, transform.position, Quaternion.identity);
            _instance = NavMesh.AddNavMeshData(_data);
        }

        static void AddIgnore(List<NavMeshBuildMarkup> markups, Transform t)
        {
            markups.Add(new NavMeshBuildMarkup { root = t, ignoreFromBuild = true, overrideArea = false });
        }

        void OnDestroy()
        {
            if (_instance.valid) NavMesh.RemoveNavMeshData(_instance);
        }
    }
}
