using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NocturneAnnex.Controls;
using NocturneAnnex.Flow;

namespace NocturneAnnex.Tests.PlayMode
{
    public class PauseControllerTests
    {
        GameObject _go;
        PauseController _pause;
        PlayerInputState _input;
        float _oldScale;
        bool _oldAudioPause;

        [SetUp]
        public void SetUp()
        {
            _oldScale = Time.timeScale;
            _oldAudioPause = AudioListener.pause;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            _input = default;
            _go = new GameObject("Pause");
            _pause = _go.AddComponent<PauseController>();
            _pause.InputProvider = () => _input;
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.Destroy(_go);
            Time.timeScale = _oldScale;
            AudioListener.pause = _oldAudioPause;
        }

        [Test]
        public void Pause_FreezesTimeAndAudio()
        {
            _pause.Pause();
            Assert.IsTrue(_pause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(AudioListener.pause);
        }

        [Test]
        public void Resume_RestoresPreviousTimeScaleExactly()
        {
            Time.timeScale = 0.5f;
            _pause.Pause();
            _pause.Resume();
            Assert.IsFalse(_pause.IsPaused);
            Assert.AreEqual(0.5f, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
        }

        [Test]
        public void Resume_KeepsAudioPausedIfItWasAlreadyPaused()
        {
            AudioListener.pause = true;
            _pause.Pause();
            _pause.Resume();
            Assert.IsTrue(AudioListener.pause);
        }

        [Test]
        public void PauseTwice_DoesNotLoseTheRememberedTimeScale()
        {
            _pause.Pause();
            _pause.Pause();
            _pause.Resume();
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void ResumeWhenNotPaused_ChangesNothing()
        {
            Time.timeScale = 0.7f;
            _pause.Resume();
            Assert.AreEqual(0.7f, Time.timeScale);
        }

        [Test]
        public void PausedChanged_FiresOnceForEachRealChange()
        {
            int fired = 0;
            _pause.PausedChanged += _ => fired++;
            _pause.Pause();
            _pause.Pause();
            _pause.Resume();
            _pause.Resume();
            Assert.AreEqual(2, fired);
        }

        [Test]
        public void LosingFocus_Pauses_ButRegainingFocusDoesNotResume()
        {
            _pause.HandleApplicationFocus(false);
            Assert.IsTrue(_pause.IsPaused);
            _pause.HandleApplicationFocus(true);
            Assert.IsTrue(_pause.IsPaused, "the player must choose to resume");
        }

        [Test]
        public void AppPause_Pauses()
        {
            _pause.HandleApplicationPause(true);
            Assert.IsTrue(_pause.IsPaused);
        }

        [UnityTest]
        public IEnumerator PauseButton_TogglesPause()
        {
            _input.PausePressed = true;
            yield return null;
            _input.PausePressed = false;
            Assert.IsTrue(_pause.IsPaused);

            _input.PausePressed = true;
            yield return null;
            _input.PausePressed = false;
            Assert.IsFalse(_pause.IsPaused);
        }

        [UnityTest]
        public IEnumerator Pause_ClearsHeldInput_SoNothingStaysStuck()
        {
            var routerGo = new GameObject("Router");
            var router = routerGo.AddComponent<InputRouter>();
            router.Touch.SetMove(Vector2.up);
            router.Touch.SetSprint(true);
            _pause.Pause();
            yield return null;
            Assert.AreEqual(Vector2.zero, router.Current.Move);
            Assert.IsFalse(router.Current.Sprint);
            Object.Destroy(routerGo);
        }

        [UnityTest]
        public IEnumerator DestroyedWhilePaused_RestoresTimeSoNextSceneIsNotFrozen()
        {
            Time.timeScale = 1f;
            _pause.Pause();
            Object.Destroy(_go);
            yield return null;
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(AudioListener.pause);
        }
    }
}
