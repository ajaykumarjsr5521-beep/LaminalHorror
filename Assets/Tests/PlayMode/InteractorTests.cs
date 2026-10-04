using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NocturneAnnex.Controls;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.Tests.PlayMode
{
    public class InteractorTests
    {
        class Probe : MonoBehaviour, IInteractable
        {
            public int Count;
            public string Prompt => "Use";
            public void Interact(Interactor i) => Count++;
        }

        GameObject _root;
        Interactor _interactor;
        PlayerInputState _input;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Root");
            _input = default;
            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(_root.transform);
            camGo.transform.position = Vector3.up * 1.6f;     // looks down +Z
            var cam = camGo.AddComponent<Camera>();
            _interactor = camGo.AddComponent<Interactor>();
            _interactor.ViewCamera = cam;
            _interactor.InputProvider = () => _input;
        }

        [TearDown]
        public void TearDown() => Object.Destroy(_root);

        Probe SpawnProbe(float z)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.transform.SetParent(_root.transform);
            g.transform.position = new Vector3(0, 1.6f, z);
            return g.AddComponent<Probe>();
        }

        [UnityTest]
        public IEnumerator Focus_WithinRange_IsSetWithinOneFrame()
        {
            var p = SpawnProbe(1.5f);
            yield return new WaitForFixedUpdate();   // colliders registered
            yield return null;
            Assert.AreSame(p, _interactor.Focus);
        }

        [UnityTest]
        public IEnumerator Focus_BeyondRange_IsNull()
        {
            SpawnProbe(3.0f);
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsNull(_interactor.Focus);
        }

        [UnityTest]
        public IEnumerator Wall_BlocksInteraction()
        {
            var p = SpawnProbe(1.8f);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(_root.transform);
            wall.transform.position = new Vector3(0, 1.6f, 1f);
            wall.transform.localScale = new Vector3(4, 4, 0.1f);
            yield return new WaitForFixedUpdate();
            yield return null;
            _input.InteractPressed = true;
            yield return null;
            Assert.IsNull(_interactor.Focus);
            Assert.AreEqual(0, p.Count);
        }

        [UnityTest]
        public IEnumerator InteractPress_InvokesFocusedInteractable()
        {
            var p = SpawnProbe(1.5f);
            yield return new WaitForFixedUpdate();
            yield return null;
            _input.InteractPressed = true;
            yield return null;
            Assert.AreEqual(1, p.Count);
        }

        [UnityTest]
        public IEnumerator FocusChanged_FiresOnGainAndLoss()
        {
            var p = SpawnProbe(1.5f);
            int changes = 0;
            _interactor.FocusChanged += _ => changes++;
            yield return new WaitForFixedUpdate();
            yield return null;
            Object.Destroy(p.gameObject);
            yield return null;
            yield return null;
            Assert.AreEqual(2, changes);
        }
    }
}
