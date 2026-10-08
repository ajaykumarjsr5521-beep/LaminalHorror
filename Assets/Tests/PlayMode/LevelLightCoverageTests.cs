#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Core;
using NocturneAnnex.Flow;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>F-14n: big rooms have no floor spot far from every light, and the ambient floor is high enough to read furniture.</summary>
    [Timeout(120000)]
    public class LevelLightCoverageTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Level_B1.unity";

        static readonly (string name, float x0, float x1, float z0, float z1)[] BigAreas =
        {
            ("Hall", -6f, 6f, 10f, 26f), ("Stacks", -18f, -6f, 12f, 24f), ("Records", 6f, 18f, 12f, 24f),
        };

        [UnitySetUp]
        public IEnumerator Load()
        {
            Time.timeScale = 1f;
            ModalGate.Reset();
            LevelLaunch.RequestNewGame();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
        }

        [TearDown]
        public void TearDown() { ModalGate.Reset(); Time.timeScale = 1f; }

        [UnityTest]
        public IEnumerator AmbientIsRaisedAndBigRoomsHaveFillLights()
        {
            Assert.GreaterOrEqual(RenderSettings.ambientLight.r, 0.25f);
            foreach (var a in BigAreas)
            {
                Assert.IsNotNull(GameObject.Find("Light_" + a.name), a.name + " main light");
                Assert.IsNotNull(GameObject.Find("Light_" + a.name + "_Fill1"), a.name + " fill 1");
                Assert.IsNotNull(GameObject.Find("Light_" + a.name + "_Fill2"), a.name + " fill 2");
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryFloorPointInBigRoomsIsInRangeOfTwoLights()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var a in BigAreas)
                for (float x = a.x0 + 1f; x <= a.x1 - 1f; x += 2f)
                    for (float z = a.z0 + 1f; z <= a.z1 - 1f; z += 2f)
                    {
                        int near = 0;
                        var p = new Vector3(x, 0.1f, z);
                        foreach (var l in lights)
                            if (l.type == LightType.Point && Vector3.Distance(l.transform.position, p) <= l.range * 0.85f) near++;
                        Assert.GreaterOrEqual(near, 2, $"{a.name} ({x},{z}) is lit by {near} lights");
                    }
            yield return null;
        }

        [UnityTest]
        public IEnumerator EventFlickerStillOnlyTouchesTheMainLights()
        {
            var spots = Object.FindObjectsByType<NocturneAnnex.Horror.HorrorEventSpot>(FindObjectsSortMode.None);
            foreach (var s in spots)
                foreach (var l in s.Lights)
                    if (l != null) Assert.That(l.name, Does.Not.Contain("_Fill"), "fill lights are never flickered");
            yield return null;
        }
    }
}
#endif
