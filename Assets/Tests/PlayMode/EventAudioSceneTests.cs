#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Audio;
using NocturneAnnex.Controls;
using NocturneAnnex.Core;
using NocturneAnnex.Flow;
using NocturneAnnex.Horror;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>Each horror event on the real Level_B1 plays its sound cue through the audio director.</summary>
    [Timeout(120000)]
    public class EventAudioSceneTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Level_B1.unity";

        HorrorEventRunner _runner;
        AudioDirector _director;
        AudioClip _clip;
        readonly Dictionary<CueDefinition, AudioClip[]> _originalClips = new Dictionary<CueDefinition, AudioClip[]>();

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            Time.timeScale = 1f;
            ModalGate.Reset();
            Accessibility.Reset();
            LevelLaunch.RequestNewGame();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _runner = Object.FindFirstObjectByType<HorrorEventRunner>();
            _director = Object.FindFirstObjectByType<AudioDirector>();
            _clip = AudioClip.Create("test", 44100, 1, 44100, false);
            // the catalog asset has no real clips yet: lend it a test clip, and put the originals back afterwards
            foreach (var cue in _director.Catalog.Cues)
            {
                _originalClips[cue] = cue.Clips;
                cue.Clips = new[] { _clip };
            }
            Captions.Service.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var kv in _originalClips) kv.Key.Clips = kv.Value;
            _originalClips.Clear();
            Object.DestroyImmediate(_clip);
            Accessibility.Reset();
            ModalGate.Reset();
            Time.timeScale = 1f;
            LevelLaunch.RequestNewGame();
            if (InputRouter.Instance != null) Object.DestroyImmediate(InputRouter.Instance.gameObject);
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects()) Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator EveryEvent_PlaysItsCue_ThroughTheDirector_AndPostsItsCaption()
        {
            var cues = new List<string>();
            foreach (var spot in _runner.Spots)
            {
                string id = spot.Event.Id;
                string cue = spot.Event.CaptionKey;
                Assert.IsNotNull(_director.Catalog.Find(cue), $"event '{id}' needs a cue '{cue}' in the catalog");
                Assert.AreEqual(CueCatalog.Sfx, _director.Catalog.Find(cue).Group);
                Captions.Service.Clear();
                Assert.IsTrue(_runner.Fire(id), id);
                yield return null;
                Assert.IsTrue(_director.IsPlaying(cue), $"event '{id}' must play cue '{cue}'");
                CollectionAssert.Contains(Captions.Service.Lines.ToList(), Loc.Get(cue));
                cues.Add(cue);
            }
            Assert.GreaterOrEqual(cues.Distinct().Count(), 6, "six events, six different cues");
        }

        [UnityTest]
        public IEnumerator AnEvent_DucksTheMusic()
        {
            var spot = _runner.Spots.First();
            _runner.Fire(spot.Event.Id);
            float until = Time.realtimeSinceStartup + 5f;
            while (_director.Bus.Gain(CueCatalog.Music) > 0.55f && Time.realtimeSinceStartup < until) yield return null;
            Assert.Less(_director.Bus.Gain(CueCatalog.Music), 0.6f);
        }
    }
}
#endif
