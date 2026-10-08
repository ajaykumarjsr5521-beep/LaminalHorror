using System;
using System.Collections.Generic;
using NUnit.Framework;
using NocturneAnnex.Strategy;

namespace NocturneAnnex.Tests.EditMode
{
    public class StrategyClientTests
    {
        sealed class FakeTransport : IStrategyTransport
        {
            public readonly List<(string id, string json, Action<TransportResult> done)> Sent = new List<(string, string, Action<TransportResult>)>();
            public void Send(string id, string json, Action<TransportResult> done) => Sent.Add((id, json, done));
            public void Reply(int i, string body) => Sent[i].done(new TransportResult { Ok = true, Body = body });
            public void Fail(int i, string err) => Sent[i].done(new TransportResult { Ok = false, Error = err });
        }

        static string Body(string id, string strategy = "INCREASE_HIDING_PRESSURE", float pr = 0.8f, float dur = 90f) =>
            "{\"request_id\":\"" + id + "\",\"strategy\":\"" + strategy + "\",\"priority\":" + pr.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            ",\"intensity\":0.65,\"duration_seconds\":" + dur.ToString(System.Globalization.CultureInfo.InvariantCulture) +
            ",\"confidence\":0.87,\"reason_code\":\"REPEATED_SUCCESSFUL_HIDING\"}";

        static PlayerProfile Profile() => new PlayerBehaviorTracker().Snapshot();

        static (StrategyClient c, StrategyExecutor e, FakeTransport t) Make()
        {
            var t = new FakeTransport(); var e = new StrategyExecutor();
            return (new StrategyClient(t, e), e, t);
        }

        [Test] public void Offline_client_does_nothing()
        {
            var e = new StrategyExecutor(); var c = new StrategyClient(null, e);
            Assert.IsFalse(c.Request(Profile(), "NEW_ROOM", 0f));
            Assert.AreEqual(ServerState.Offline, c.State);
            Assert.AreEqual(0, c.RequestsSent);
        }

        [Test] public void Valid_reply_applies_strategy()
        {
            var (c, e, t) = Make();
            Assert.IsTrue(c.Request(Profile(), "NEW_ROOM", 10f));
            StringAssert.Contains("\"trigger\":\"NEW_ROOM\"", t.Sent[0].json);
            t.Reply(0, Body("req-1"));
            Assert.IsTrue(e.HasActive);
            Assert.AreEqual(StrategyType.IncreaseHidingPressure, e.Active.Strategy);
            Assert.AreEqual(ServerState.Online, c.State);
            Assert.AreEqual(1, c.CommandsApplied);
        }

        [Test] public void Timeout_with_retry_then_failure_keeps_current_strategy()
        {
            var (c, e, t) = Make();
            e.TryApply(new StrategyCommand { Strategy = StrategyType.RelaxPressure, Priority = 1, Intensity = 1, DurationSeconds = 100, Confidence = 1 }, 0f);
            c.Request(Profile(), "DEATH", 10f);
            c.Tick(11f); Assert.AreEqual(1, t.Sent.Count);
            c.Tick(12.1f); Assert.AreEqual(2, t.Sent.Count);   // one retry
            c.Tick(14.2f);
            Assert.AreEqual(ServerState.Failed, c.State);
            Assert.AreEqual("timeout", c.LastError);
            Assert.AreEqual(StrategyType.RelaxPressure, e.Active.Strategy);
        }

        [Test] public void Late_reply_after_failure_is_ignored()
        {
            var (c, e, t) = Make();
            c.MaxRetries = 0;
            c.Request(Profile(), "DEATH", 0f);
            c.Tick(3f);
            t.Reply(0, Body("req-1"));
            Assert.IsFalse(e.HasActive);
            Assert.AreEqual(ServerState.Failed, c.State);
        }

        [TestCase("not json")] [TestCase("")] [TestCase("{\"request_id\":\"req-1\"}")]
        [TestCase("{\"request_id\":\"req-1\",\"strategy\":\"TELEPORT_TO_PLAYER\",\"priority\":1,\"intensity\":1,\"duration_seconds\":30,\"confidence\":1}")]
        public void Bad_reply_is_rejected(string body)
        {
            var (c, e, t) = Make();
            c.Request(Profile(), "NEW_ROOM", 0f);
            t.Reply(0, body);
            Assert.IsFalse(e.HasActive);
            Assert.AreEqual(1, c.CommandsRejected);
            Assert.IsNull(c.PendingRequestId);
        }

        [Test] public void Out_of_range_values_are_rejected_by_validator()
        {
            var (c, e, t) = Make();
            c.Request(Profile(), "NEW_ROOM", 0f);
            t.Reply(0, Body("req-1", pr: 3f));
            Assert.IsFalse(e.HasActive);
            Assert.AreEqual(1, c.CommandsRejected);
            StringAssert.Contains("OutOfRange", c.LastError);
        }

        [Test] public void Mismatched_request_id_is_rejected()
        {
            var (c, e, t) = Make();
            c.Request(Profile(), "NEW_ROOM", 0f);
            t.Reply(0, Body("req-99"));
            Assert.IsFalse(e.HasActive);
            Assert.AreEqual(1, c.CommandsRejected);
        }

        [Test] public void Cooldown_and_single_flight_limit_calls()
        {
            var (c, e, t) = Make();
            Assert.IsTrue(c.Request(Profile(), "A", 0f));
            Assert.IsFalse(c.Request(Profile(), "B", 1f));      // in flight
            t.Reply(0, Body("req-1"));
            Assert.IsFalse(c.Request(Profile(), "C", 30f));     // cooldown
            Assert.IsTrue(c.Request(Profile(), "D", 61f));
            Assert.AreEqual(2, t.Sent.Count);
        }

        [Test] public void Transport_error_retries_once_then_fails()
        {
            var (c, e, t) = Make();
            c.Request(Profile(), "A", 0f);
            t.Fail(0, "refused");
            Assert.AreEqual(2, t.Sent.Count);
            t.Fail(1, "refused");
            Assert.AreEqual(ServerState.Failed, c.State);
            Assert.AreEqual("refused", c.LastError);
            Assert.IsFalse(e.HasActive);
        }

        [Test] public void Request_body_has_no_positions_or_spot_ids()
        {
            var tr = new PlayerBehaviorTracker(); var bus = new GameEventBus(); tr.Attach(bus);
            bus.Publish(new GameEvent(GameEventType.Hide, 5f, 1f, "cupboard"));
            var (c, e, t) = Make();
            c.Request(tr.Snapshot(), "NEW_ROOM", 6f);
            string lower = t.Sent[0].json.ToLowerInvariant();
            StringAssert.DoesNotContain("position", lower);
            StringAssert.DoesNotContain("spot_id", lower);
            StringAssert.Contains("cupboard", lower);
        }
    }
}
