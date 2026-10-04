using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NocturneAnnex.Controls;
using NocturneAnnex.Core;

namespace NocturneAnnex.Tests.PlayMode
{
    public class InputRouterModalTests
    {
        GameObject _go;
        InputRouter _router;

        [SetUp]
        public void SetUp()
        {
            ModalGate.Reset();
            _go = new GameObject("Router");
            _router = _go.AddComponent<InputRouter>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);   // InputRouter is a singleton; deferred Destroy would make the next test's router self-destruct
            ModalGate.Reset();
        }

        [UnityTest]
        public IEnumerator OpenModal_BlocksMovementAndInteract_ClosingRestoresThem()
        {
            _router.Touch.SetMove(Vector2.up);
            yield return null;
            Assert.AreEqual(1f, _router.Current.Move.y, 1e-3f);

            var handle = ModalGate.Open();
            _router.Touch.SetMove(Vector2.up);
            _router.Touch.PressInteract();
            yield return null;
            Assert.AreEqual(Vector2.zero, _router.Current.Move);
            Assert.IsFalse(_router.Current.InteractPressed);

            handle.Dispose();
            _router.Touch.SetMove(Vector2.up);
            yield return null;
            Assert.AreEqual(1f, _router.Current.Move.y, 1e-3f);
        }

        [UnityTest]
        public IEnumerator OpenModal_StillAllowsPause()
        {
            using (ModalGate.Open())
            {
                _router.Touch.PressPause();
                yield return null;
                Assert.IsTrue(_router.Current.PausePressed);
            }
        }
    }
}
