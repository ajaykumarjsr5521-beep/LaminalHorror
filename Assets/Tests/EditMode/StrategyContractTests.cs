using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using NocturneAnnex.Strategy;

namespace NocturneAnnex.Tests.EditMode
{
    /// <summary>Shared JSON fixtures live in Server/tests/contract and are read by pytest too, so a rename on one side fails both suites.</summary>
    public class StrategyContractTests
    {
        static string Fixture(string name) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "..", "Server", "tests", "contract", name));

        static HashSet<string> TopLevelKeys(string json)
        {
            var keys = new HashSet<string>();
            int depth = 0;
            for (int i = 0; i < json.Length; i++)
            {
                char ch = json[i];
                if (ch == '{' || ch == '[') depth++;
                else if (ch == '}' || ch == ']') depth--;
                else if (ch == '"' && depth == 1)
                {
                    int end = json.IndexOf('"', i + 1);
                    if (end > 0 && end + 1 < json.Length && json[end + 1] == ':') keys.Add(json.Substring(i + 1, end - i - 1));
                    i = end;
                }
                else if (ch == '"') i = json.IndexOf('"', i + 1);
            }
            return keys;
        }

        [Test] public void Request_dto_has_exactly_the_fixture_keys()
        {
            var tr = new PlayerBehaviorTracker();
            string json = StrategyWire.ToJson(StrategyRequestDto.From(tr.Snapshot(), "req-1", "NEW_ROOM"));
            CollectionAssert.AreEquivalent(TopLevelKeys(Fixture("request.json")), TopLevelKeys(json));
        }

        [Test] public void Response_fixture_parses_and_validates()
        {
            Assert.IsTrue(StrategyWire.TryParse(Fixture("response.json"), out var id, out var cmd, out var err), err);
            Assert.AreEqual("req-1", id);
            Assert.AreEqual(StrategyType.IncreaseHidingPressure, cmd.Strategy);
            Assert.IsTrue(StrategyValidator.IsValid(cmd));
        }

        [Test] public void Debug_text_shows_real_time_and_strategic_sections()
        {
            var ex = new StrategyExecutor();
            ex.TryApply(new StrategyCommand { Strategy = StrategyType.IncreaseHidingPressure, Priority = .8f, Intensity = .65f, DurationSeconds = 90, Confidence = .82f, ReasonCode = "REPEATED_SUCCESSFUL_HIDING" }, 0f);
            var client = new StrategyClient(null, ex);
            string t = StrategyDebugText.Build(ex, client, 26f, "INVESTIGATE", 0.72f, 0.34f, "ROOM_5_CORRIDOR");
            StringAssert.Contains("REAL-TIME AI", t);
            StringAssert.Contains("State: INVESTIGATE", t);
            StringAssert.Contains("Strategy Remaining: 64 s", t);
            StringAssert.Contains("STRATEGIC AI", t);
            StringAssert.Contains("Reason: REPEATED_SUCCESSFUL_HIDING", t);
            StringAssert.Contains("OFFLINE", t);
        }

        [Test] public void Debug_text_with_no_strategy_says_baseline()
        {
            var ex = new StrategyExecutor();
            StringAssert.Contains("none (baseline AI)", StrategyDebugText.Build(ex, new StrategyClient(null, ex), 0f));
        }

        [Test] public void Slow_tick_cost_is_negligible()
        {
            var ex = new StrategyExecutor(); var client = new StrategyClient(null, ex);
            ex.TryApply(new StrategyCommand { Strategy = StrategyType.RelaxPressure, Priority = 1, Intensity = 1, DurationSeconds = 180, Confidence = 1 }, 0f);
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100000; i++) { ex.Tick(i * 0.001f); client.Tick(i * 0.001f); }
            sw.Stop();
            Assert.Less(sw.ElapsedMilliseconds, 200, "100k ticks took " + sw.ElapsedMilliseconds + " ms");
        }

        [Test] public void Offline_game_loop_runs_with_no_server()
        {
            var ex = new StrategyExecutor(); var client = new StrategyClient(null, ex);
            var tr = new PlayerBehaviorTracker(); var bus = new GameEventBus(); tr.Attach(bus);
            for (int i = 0; i < 1000; i++) bus.Publish(new GameEvent(GameEventType.Move, i, 1f, null, MoveMode.Walk));
            Assert.IsFalse(client.Request(tr.Snapshot(), "NEW_ROOM", 1000f));
            client.Tick(1001f);
            Assert.IsFalse(ex.HasActive);
            Assert.AreEqual(1f, ex.InvestigationMultiplier);
            Assert.AreEqual(0f, ex.HideBonus);
        }
    }
}
