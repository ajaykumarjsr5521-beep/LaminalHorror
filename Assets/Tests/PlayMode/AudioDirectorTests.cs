#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NocturneAnnex.Audio;
using NocturneAnnex.Core;

namespace NocturneAnnex.Tests.PlayMode
{
    [Timeout(60000)]
    public class AudioDirectorTests
    {
        GameObject _go;
        AudioDirector _dir;
        CueCatalog _cat;
        AudioClip _clip;

        [SetUp]
        public void SetUp()
        {
            Accessibility.Reset();
            Captions.Service.Clear();
            _clip = AudioClip.Create("test", 44100 * 3, 1, 44100, false);   // 3 s of silence
            _cat = CueCatalog.CreateDefault();
            foreach (var c in _cat.Cues) c.Clips = new[] { _clip };
            _go = new GameObject("Director");
            _dir = _go.AddComponent<AudioDirector>();
            _dir.Catalog = _cat;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_cat);
            Object.DestroyImmediate(_clip);
            Captions.Service.Clear();
        }

        [UnityTest]
        public IEnumerator Play_StartsAVoice_AndPostsTheCaption()
        {
            Assert.IsTrue(_dir.Play("event.door_slam", Vector3.zero));
            yield return null;
            Assert.IsTrue(_dir.IsPlaying("event.door_slam"));
            CollectionAssert.Contains(Captions.Service.Lines.ToList(), Loc.Get("event.door_slam"));
        }

        [UnityTest]
        public IEnumerator MissingClip_StillPostsTheCaption_AndLogsOnce()
        {
            _cat.Find("event.figure").Clips = new AudioClip[0];
            LogAssert.Expect(LogType.Error, new Regex("no clip assigned"));
            Assert.IsFalse(_dir.Play("event.figure"));
            Assert.IsFalse(_dir.Play("event.figure"));   // a second error would fail the test
            yield return null;
            CollectionAssert.Contains(Captions.Service.Lines.ToList(), Loc.Get("event.figure"));
        }

        [UnityTest]
        public IEnumerator UnknownCue_IsAnErrorAndReturnsFalse()
        {
            LogAssert.Expect(LogType.Error, new Regex("unknown cue"));
            Assert.IsFalse(_dir.Play("nope"));
            yield return null;
        }

        [UnityTest]
        public IEnumerator GameplayCue_DucksMusic_ThenItRecovers()
        {
            _dir.Play("event.whisper", Vector3.zero);
            for (int i = 0; i < 20; i++) yield return null;
            Assert.Less(_dir.Bus.Gain(CueCatalog.Music), 0.9f, "music must duck while a gameplay cue plays");
            Assert.AreEqual(1f, _dir.Bus.Gain(CueCatalog.Sfx), 1e-4f);
            _dir.Stop("event.whisper");
            float end = Time.realtimeSinceStartup + 10f;
            while (_dir.Bus.Gain(CueCatalog.Music) < 0.999f && Time.realtimeSinceStartup < end) yield return null;
            Assert.AreEqual(1f, _dir.Bus.Gain(CueCatalog.Music), 1e-3f);
        }

        [UnityTest]
        public IEnumerator Sliders_ScalePlayingVoices()
        {
            _dir.Play("door.open", Vector3.zero);
            yield return null;
            var src = _go.GetComponentsInChildren<AudioSource>().First(s => s.isPlaying);
            float full = src.volume;
            _dir.Bus.SetSlider(CueCatalog.Sfx, 0.25f);
            yield return null;
            Assert.AreEqual(full * 0.25f, src.volume, 1e-4f);
        }

        [UnityTest]
        public IEnumerator VoicePool_IsCapped_AndGameplayCuesStealTheOldest()
        {
            for (int i = 0; i < AudioDirector.MaxVoices; i++) Assert.IsTrue(_dir.Play("door.open", Vector3.zero, loop: true));
            yield return null;
            Assert.AreEqual(AudioDirector.MaxVoices, _dir.ActiveVoices);
            Assert.IsFalse(_dir.Play("door.close", Vector3.zero), "an ordinary cue is dropped when every voice is busy");
            Assert.IsTrue(_dir.Play("event.door_slam", Vector3.zero), "a gameplay cue steals the oldest voice");
            Assert.AreEqual(AudioDirector.MaxVoices, _go.GetComponentsInChildren<AudioSource>().Length);
        }
    }
}
#endif
