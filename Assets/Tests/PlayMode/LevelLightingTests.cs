#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Controls;
using NocturneAnnex.Core;
using NocturneAnnex.Flow;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>Level_B1 stays inside the mobile lighting budget: at most 2 realtime lights, the rest baked into lightmaps.</summary>
    [Timeout(120000)]
    public class LevelLightingTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Level_B1.unity";
        const int MaxRealtimeLights = 2;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            Time.timeScale = 1f;
            ModalGate.Reset();
            LevelLaunch.RequestNewGame();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            ModalGate.Reset();
            LevelLaunch.RequestNewGame();
            if (InputRouter.Instance != null) Object.DestroyImmediate(InputRouter.Instance.gameObject);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator Level_HasAtMostTwoRealtimeLights()
        {
            yield return null;
            var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int realtime = lights.Count(l => l.lightmapBakeType == LightmapBakeType.Realtime);
            Assert.LessOrEqual(realtime, MaxRealtimeLights, "realtime lights: " + string.Join(", ", lights.Where(l => l.lightmapBakeType == LightmapBakeType.Realtime).Select(l => l.name)));
            Assert.GreaterOrEqual(lights.Count(l => l.lightmapBakeType == LightmapBakeType.Baked), 3, "the other area lights are baked");
        }

        [UnityTest]
        public IEnumerator Level_HasBakedLightmaps()
        {
            yield return null;
            Assert.Greater(LightmapSettings.lightmaps.Length, 0, "Level_B1 must be baked; run Build > Create Level B1 Scene");
        }

        [UnityTest]
        public IEnumerator FlickeredLights_AreRealtime_SoTheEffectCanChangeThem()
        {
            yield return null;
            foreach (var name in new[] { "Light_Hall", "Light_Stacks" })
                Assert.AreEqual(LightmapBakeType.Realtime, GameObject.Find(name).GetComponent<Light>().lightmapBakeType, name);
        }
    }
}
#endif
