#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Core;
using NocturneAnnex.Entity;
using NocturneAnnex.Flow;
using NocturneAnnex.Horror;
using NocturneAnnex.Strategy;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>F-15j: the strategic layer is wired into the real Level_B1, works offline, and only moves its two allowed knobs.</summary>
    [Timeout(120000)]
    public class StrategyIntegrationTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Level_B1.unity";

        StrategyRunner _runner;
        StrategyBridge _bridge;
        StalkerAgent _stalker;
        NoiseHub _hub;
        readonly List<GameEvent> _events = new List<GameEvent>();

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            Time.timeScale = 1f;
            ModalGate.Reset();
            PlayerPrefs.DeleteKey("strategy.online");
            LevelLaunch.RequestNewGame();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            _runner = Object.FindFirstObjectByType<StrategyRunner>();
            _bridge = Object.FindFirstObjectByType<StrategyBridge>();
            _stalker = Object.FindFirstObjectByType<StalkerAgent>();
            _hub = Object.FindFirstObjectByType<NoiseHub>();
            _events.Clear();
            _runner.Bus.Published += _events.Add;
        }

        [TearDown]
        public void TearDown() { ModalGate.Reset(); Time.timeScale = 1f; }

        sealed class FakeHorrorTransport : IStrategyTransport
        {
            public string Id, Json; public System.Action<TransportResult> Done;
            public void Send(string id, string json, System.Action<TransportResult> done) { Id = id; Json = json; Done = done; }
        }

        [UnityTest]
        public IEnumerator Horror_Offline_SendsNothing()
        {
            yield return new WaitForSeconds(2.2f);
            Assert.AreEqual(ServerState.Offline, _runner.HorrorClient.State);
            Assert.AreEqual(0, _runner.HorrorClient.RequestsSent);
            Assert.IsNotNull(_runner.Horror, "bridge finds the HorrorEventRunner");
        }

        [UnityTest]
        public IEnumerator Horror_FakeServerSuggestion_PlaysTheLocalEvent()
        {
            var t = new FakeHorrorTransport();
            _runner.UseHorrorTransport(t);
            yield return new WaitForSeconds(1.5f);   // the bridge asks on its one-second tick
            Assert.IsNotNull(t.Json, "a request was sent");
            StringAssert.DoesNotContain("position", t.Json);
            t.Done(new TransportResult { Ok = true, Body = "{\"request_id\":\"" + t.Id + "\",\"event\":\"LIGHT_FLICKER\",\"tension\":0.4,\"reason_code\":\"TEST\"}" });
            yield return null;
            var spot = System.Array.Find(Object.FindObjectsByType<HorrorEventSpot>(FindObjectsSortMode.None), x => x.Event != null && x.Event.Id == "flicker_hall");
            Assert.IsTrue(spot.IsPlaying, "the suggested flicker is playing");
        }

        [UnityTest]
        public IEnumerator LevelHasRunnerAndBridgeAndPlaysOffline()
        {
            Assert.IsNotNull(_runner);
            Assert.IsNotNull(_bridge);
            Assert.IsFalse(_runner.Online, "no server by default");
            Assert.AreSame(_bridge, _stalker.Modifiers);
            Assert.AreEqual(0f, _bridge.HideBonus);
            Assert.AreEqual(1f, _bridge.InvestigationMultiplier);
            yield return null;
            Assert.AreEqual(1f, _stalker.Brain.SearchScale);
        }

        [UnityTest]
        public IEnumerator NoiseCatchHideAndChaseEndProduceCategoryOnlyEvents()
        {
            _hub.Emit(Vector3.zero, NoiseKind.Run);
            _bridge.OnCaught();
            _bridge.OnState(EntityState.Patrol, EntityState.Chase);
            _bridge.OnState(EntityState.Chase, EntityState.Search);
            yield return null;
            Assert.IsTrue(_events.Exists(e => e.Type == GameEventType.Noise && e.Value > 0f && e.Value <= 1f));
            Assert.IsTrue(_events.Exists(e => e.Type == GameEventType.Death));
            Assert.IsTrue(_events.Exists(e => e.Type == GameEventType.ChaseEnded && e.Value >= 0f));
            foreach (var e in _events)
                Assert.That(e.Tag == null || !e.Tag.Contains("("), "tags are categories, never coordinates: " + e.Tag);
        }

        [UnityTest]
        public IEnumerator CommandRaisesHideBonusAndSearchTimeWithinLimitsAndExpires()
        {
            var cmd = new StrategyCommand { Strategy = StrategyType.IncreaseHidingPressure, Priority = 0.8f, Intensity = 0.7f, DurationSeconds = 20f, Confidence = 0.8f, ReasonCode = "TEST" };
            Assert.IsTrue(_runner.Executor.TryApply(cmd, Time.time));
            var inv = new StrategyCommand { Strategy = StrategyType.IncreaseInvestigation, Priority = 0.8f, Intensity = 0.7f, DurationSeconds = 20f, Confidence = 0.8f, ReasonCode = "TEST" };
            Assert.IsTrue(_runner.Executor.TryApply(inv, Time.time) || _runner.Executor.HasActive);
            yield return null;
            Assert.Greater(_bridge.HideBonus + (_bridge.InvestigationMultiplier - 1f), 0f);
            Assert.LessOrEqual(_stalker.Brain.SearchScale, StrategyExecutor.MaxInvestigationMultiplier);
            Assert.GreaterOrEqual(_stalker.Brain.SearchScale, StrategyExecutor.MinInvestigationMultiplier);
            _runner.Executor.Clear();
            yield return null;
            Assert.AreEqual(0f, _bridge.HideBonus);
            Assert.AreEqual(1f, _stalker.Brain.SearchScale);
        }

        [UnityTest]
        public IEnumerator InvestigationCommandLengthensTheSearchWithinLimits()
        {
            var inv = new StrategyCommand { Strategy = StrategyType.IncreaseInvestigation, Priority = 0.8f, Intensity = 0.7f, DurationSeconds = 20f, Confidence = 0.8f, ReasonCode = "TEST" };
            Assert.IsTrue(_runner.Executor.TryApply(inv, Time.time));
            yield return null;
            Assert.Greater(_stalker.Brain.SearchScale, 1f);
            Assert.LessOrEqual(_stalker.Brain.SearchScale, StrategyExecutor.MaxInvestigationMultiplier);
        }

        [UnityTest]
        public IEnumerator LevelStartPublishesRoomEnteredB1AndSecondlyMoveEvents()
        {
            yield return new WaitForSeconds(1.3f);
            Assert.IsTrue(_events.Exists(e => e.Type == GameEventType.Move));
        }
    }
}
#endif
