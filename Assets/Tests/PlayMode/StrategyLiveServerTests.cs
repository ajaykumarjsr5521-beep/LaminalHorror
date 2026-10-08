#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Core;
using NocturneAnnex.Flow;
using NocturneAnnex.Strategy;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>F-15j AC5: needs a running server (uvicorn main:app --port 8765 in Server/). Skipped, not failed, when it is down.</summary>
    [Timeout(120000)]
    public class StrategyLiveServerTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Level_B1.unity";

        [TearDown]
        public void TearDown() { PlayerPrefs.DeleteKey("strategy.online"); ModalGate.Reset(); Time.timeScale = 1f; }

        [UnityTest]
        public IEnumerator NewRoomTriggerGetsAnAnswerFromTheLiveServer()
        {
            using (var ping = UnityWebRequest.Get("http://127.0.0.1:8765/health"))
            {
                ping.timeout = 2;
                yield return ping.SendWebRequest();
                if (ping.result != UnityWebRequest.Result.Success) Assert.Ignore("Server not running on 127.0.0.1:8765");
            }

            Time.timeScale = 1f;
            ModalGate.Reset();
            PlayerPrefs.SetInt("strategy.online", 1);
            LevelLaunch.RequestNewGame();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            var runner = Object.FindFirstObjectByType<StrategyRunner>();
            Assert.IsTrue(runner.Online);

            float start = Time.time;
            while (Time.time - start < 8f && runner.Client.State != ServerState.Online && runner.Client.State != ServerState.Failed) yield return null;
            Debug.Log($"[live] state={runner.Client.State} sent={runner.Client.RequestsSent} applied={runner.Client.CommandsApplied} rejected={runner.Client.CommandsRejected} err={runner.Client.LastError} after={Time.time - start:F1}s");
            Assert.GreaterOrEqual(runner.Client.RequestsSent, 1);
            Assert.AreEqual(ServerState.Online, runner.Client.State, runner.Client.LastError);
        }
    }
}
#endif
