#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NocturneAnnex.Audio;

namespace NocturneAnnex.Tests.PlayMode
{
    [Timeout(60000)]
    public class MusicDroneTests
    {
        GameObject _go;
        AudioDirector _dir;
        MusicDrone _drone;
        CueCatalog _cat;
        AudioClip _clip;
        float _tension;
        bool _blocked;

        [SetUp]
        public void SetUp()
        {
            _tension = 0f;
            _blocked = false;
            _clip = AudioClip.Create("drone", 44100 * 2, 1, 44100, false);
            _cat = CueCatalog.CreateDefault();
            _cat.Find(MusicDrone.CueId).Clips = new[] { _clip };
            _go = new GameObject("Audio");
            _go.SetActive(false);   // set the sources before Start runs
            _dir = _go.AddComponent<AudioDirector>();
            _dir.Catalog = _cat;
            _drone = _go.AddComponent<MusicDrone>();
            _drone.Director = _dir;
            _drone.TensionSource = () => _tension;
            _drone.BlockedSource = () => _blocked;
            _drone.Model.RisePerSecond = 50f;   // fast, so tests do not wait on real time
            _drone.Model.FallPerSecond = 50f;
            _go.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_cat);
            Object.DestroyImmediate(_clip);
        }

        [UnityTest]
        public IEnumerator Drone_Loops_AndFollowsTension()
        {
            yield return null;
            Assert.IsTrue(_dir.IsPlaying(MusicDrone.CueId));
            float until = Time.realtimeSinceStartup + 5f;
            while (_dir.GetVolume(MusicDrone.CueId) > _drone.Model.MinLevel * 0.9f + 0.05f && Time.realtimeSinceStartup < until) yield return null;
            float calm = _dir.GetVolume(MusicDrone.CueId);

            _tension = 1f;
            until = Time.realtimeSinceStartup + 5f;
            while (_dir.GetVolume(MusicDrone.CueId) < calm + 0.3f && Time.realtimeSinceStartup < until) yield return null;
            Assert.Greater(_dir.GetVolume(MusicDrone.CueId), calm + 0.3f, "the drone must get louder with tension");

            _tension = 0f;
            until = Time.realtimeSinceStartup + 5f;
            while (_dir.GetVolume(MusicDrone.CueId) > calm + 0.05f && Time.realtimeSinceStartup < until) yield return null;
            Assert.LessOrEqual(_dir.GetVolume(MusicDrone.CueId), calm + 0.05f, "and quieter when tension drops");
        }

        [UnityTest]
        public IEnumerator Drone_IsSilent_WhileBlocked()
        {
            _tension = 1f;
            yield return null;
            _blocked = true;
            float until = Time.realtimeSinceStartup + 5f;
            while (_dir.GetVolume(MusicDrone.CueId) > 0.001f && Time.realtimeSinceStartup < until) yield return null;
            Assert.AreEqual(0f, _dir.GetVolume(MusicDrone.CueId), 0.001f);
            _blocked = false;
            until = Time.realtimeSinceStartup + 5f;
            while (_dir.GetVolume(MusicDrone.CueId) < 0.5f && Time.realtimeSinceStartup < until) yield return null;
            Assert.Greater(_dir.GetVolume(MusicDrone.CueId), 0.5f);
        }
    }
}
#endif
