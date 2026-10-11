#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Audio;
using NocturneAnnex.Core;
using NocturneAnnex.Entity;
using NocturneAnnex.Flow;
using NocturneAnnex.Horror;
using NocturneAnnex.Level;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>F-14k: events become pulses through the HapticsPlayer; settings off or a paused game send nothing.</summary>
    public class HapticsPlayerTests
    {
        sealed class FakeDevice : IHapticsDevice
        {
            public readonly List<(HapticEvent kind, HapticPulse pulse)> Pulses = new List<(HapticEvent, HapticPulse)>();
            public void Pulse(HapticEvent kind, HapticPulse pulse) => Pulses.Add((kind, pulse));
        }

        GameObject _go;
        HapticsPlayer _player;
        FakeDevice _device;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            HapticSettings.Set(true, 1f);
            _go = new GameObject("Haptics");
            _player = _go.AddComponent<HapticsPlayer>();
            _player.Devices.Clear();   // only the fake: no real rumble from a test
            _device = new FakeDevice();
            _player.Devices.Add(_device);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_go);
            Time.timeScale = 1f;
            HapticSettings.Set(true, 0.7f);
        }

        [Test]
        public void AFootstepNearby_SendsAFootstepPulse_FadingWithDistance()
        {
            _player.OnStep(1f);
            Assert.AreEqual(1, _device.Pulses.Count);
            Assert.AreEqual(HapticEvent.Footstep, _device.Pulses[0].kind);

            _player.OnStep(1f);   // a second step inside the limiter gap is dropped
            Assert.AreEqual(1, _device.Pulses.Count, "limited");
        }

        [UnityTest]
        public IEnumerator FartherFootsteps_AreWeaker_AndBeyondRangeSendNothing()
        {
            _player.OnStep(2f);
            float near = _device.Pulses[0].pulse.Low;
            yield return new WaitForSecondsRealtime(0.3f);
            _player.OnStep(8f);
            Assert.AreEqual(2, _device.Pulses.Count);
            Assert.Less(_device.Pulses[1].pulse.Low, near);
            yield return new WaitForSecondsRealtime(0.3f);
            _player.OnStep(HapticsPlan.FootstepRange + 1f);
            Assert.AreEqual(2, _device.Pulses.Count, "too far to feel");
        }

        [Test]
        public void HitAndScare_SendTheirPulses()
        {
            _player.OnCaught();
            _player.OnScare("flicker_hall");
            CollectionAssert.AreEqual(new[] { HapticEvent.Hit, HapticEvent.Scare }, _device.Pulses.Select(p => p.kind).ToArray());
        }

        [Test]
        public void Disabled_SendsNothing_ForEveryEvent()
        {
            HapticSettings.Set(false, 1f);
            _player.OnStep(1f); _player.OnCaught(); _player.OnScare("x");
            Assert.AreEqual(0, _device.Pulses.Count);
            Assert.IsFalse(_player.Play(HapticEvent.Hit));
        }

        [Test]
        public void StrengthZero_SendsNothing()
        {
            HapticSettings.Set(true, 0f);
            Assert.IsFalse(_player.Play(HapticEvent.Hit));
            Assert.AreEqual(0, _device.Pulses.Count);
        }

        [Test]
        public void WhilePaused_SendsNothing()
        {
            Time.timeScale = 0f;
            Assert.IsFalse(_player.Play(HapticEvent.Hit));
            Assert.AreEqual(0, _device.Pulses.Count);
        }

        [Test]
        public void TheHeartbeat_IsFeltFromDangerLevelTwo()
        {
            var heart = _go.AddComponent<HeartbeatPlayer>();
            heart.Silent = true;
            _player.Heartbeat = heart;
            heart.Level = 1;
            _player.OnBeat();
            Assert.AreEqual(0, _device.Pulses.Count);
            heart.Level = 3;
            _player.OnBeat();
            Assert.AreEqual(HapticEvent.Heartbeat, _device.Pulses.Single().kind);
        }

        [UnityTest]
        public IEnumerator InARoomScene_TheHapticsPlayerIsWiredToTheStalkerHeartbeatAndScares()
        {
            Object.Destroy(_go);
            LevelLaunch.RequestNewGame();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RoomFlow.ScenePath(2), new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var haptics = Object.FindFirstObjectByType<HapticsPlayer>();
            Assert.IsNotNull(haptics, "the room places a HapticsPlayer");
            Assert.AreSame(Object.FindFirstObjectByType<StalkerAgent>(), haptics.Stalker);
            Assert.AreSame(Object.FindFirstObjectByType<HeartbeatPlayer>(), haptics.Heartbeat);
            Assert.AreSame(Object.FindFirstObjectByType<HorrorEventRunner>(), haptics.Horror);
            haptics.Devices.Clear();
            var fake = new FakeDevice();
            haptics.Devices.Add(fake);
            HapticSettings.Set(true, 1f);
            Object.FindFirstObjectByType<HorrorEventRunner>().Fire("room_flicker");   // a real event reaches the device
            yield return null;
            Assert.IsTrue(fake.Pulses.Any(p => p.kind == HapticEvent.Scare));
        }
    }
}
#endif
