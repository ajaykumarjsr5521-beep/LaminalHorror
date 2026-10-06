#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Audio;
using NocturneAnnex.Controls;
using NocturneAnnex.Core;
using NocturneAnnex.Flow;
using NocturneAnnex.Level;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>Footsteps on the real Level_B1: the surface under the player picks the cue, and moving plays steps.</summary>
    [Timeout(120000)]
    public class FootstepSceneTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Level_B1.unity";

        LevelBootstrap _level;
        Transform _player;
        CharacterController _cc;
        FootstepPlayer _steps;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            Time.timeScale = 1f;
            ModalGate.Reset();
            LevelLaunch.RequestNewGame();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _level = Object.FindFirstObjectByType<LevelBootstrap>();
            _player = _level.Player;
            _cc = _player.GetComponent<CharacterController>();
            _steps = _player.GetComponent<FootstepPlayer>();
        }

        [TearDown]
        public void TearDown()
        {
            ModalGate.Reset();
            Time.timeScale = 1f;
            LevelLaunch.RequestNewGame();
            if (InputRouter.Instance != null) Object.DestroyImmediate(InputRouter.Instance.gameObject);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
        }

        IEnumerator Teleport(Vector3 pos)
        {
            _cc.enabled = false;
            _player.position = pos;
            _cc.enabled = true;
            _cc.Move(Vector3.down * 0.01f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Level_HasAnAudioDirector_AndFootstepPlayerOnThePlayer()
        {
            yield return null;
            Assert.IsNotNull(_steps);
            Assert.IsNotNull(_steps.Director);
            Assert.IsNotNull(_steps.Director.Catalog);
            CollectionAssert.IsEmpty(_steps.Director.Catalog.Validate(Loc.Has));
        }

        [UnityTest]
        public IEnumerator EachArea_PicksItsOwnSurface()
        {
            yield return Teleport(new Vector3(0f, 0.1f, 3f));      // break room
            Assert.AreEqual("tile", _steps.CurrentSurface());
            yield return Teleport(new Vector3(-12f, 0.1f, 18f));   // stacks
            Assert.AreEqual("carpet", _steps.CurrentSurface());
            yield return Teleport(new Vector3(12f, 0.1f, 18f));    // records
            Assert.AreEqual("carpet", _steps.CurrentSurface());
            yield return Teleport(new Vector3(0f, 0.1f, 34f));     // dock
            Assert.AreEqual("concrete", _steps.CurrentSurface());
        }

        [UnityTest]
        public IEnumerator Stepping_PlaysTheCueOfTheSurface_AndCrouchingIsQuieter()
        {
            yield return Teleport(new Vector3(-12f, 0.1f, 18f));
            string heard = null;
            _steps.StepTaken += c => heard = c;
            _steps.Advance(FootstepPlayer.WalkStride, grounded: true, crouched: false, sprinting: false);
            Assert.AreEqual("footstep.carpet", heard);
            Assert.AreEqual("footstep.carpet", _steps.LastCueId);

            heard = null;
            _steps.Advance(1f, true, crouched: true, sprinting: false);   // crouch stride is 1.2
            Assert.IsNull(heard, "a short move is not yet a step");
            _steps.Advance(0.5f, true, crouched: true, sprinting: false);
            Assert.AreEqual("footstep.carpet", heard);
        }

        [UnityTest]
        public IEnumerator FloorWithoutASurface_PlaysTheDefault()
        {
            yield return Teleport(new Vector3(0f, 0.1f, 3f));
            foreach (var f in Object.FindObjectsByType<FloorSurface>(FindObjectsSortMode.None)) Object.DestroyImmediate(f);
            Assert.AreEqual(FloorSurface.Default, _steps.CurrentSurface());
        }
    }
}
#endif
