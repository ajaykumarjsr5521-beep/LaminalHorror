using System;
using System.Collections.Generic;
using NUnit.Framework;
using NocturneAnnex.Strategy;

namespace NocturneAnnex.Tests.EditMode
{
    public class HorrorClientTests
    {
        sealed class FakeTransport : IStrategyTransport
        {
            public readonly List<(string id, string json, Action<TransportResult> done)> Sent = new List<(string, string, Action<TransportResult>)>();
            public void Send(string id, string json, Action<TransportResult> done) => Sent.Add((id, json, done));
        }

        static string Reply(string id, string ev = "LIGHT_FLICKER") => "{\"request_id\":\"" + id + "\",\"event\":\"" + ev + "\",\"tension\":0.5,\"reason_code\":\"X\"}";
        static string Body(string id) => "{\"request_id\":\"" + id + "\"}";

        static (HorrorClient c, FakeTransport t, List<string> got) Make()
        {
            var t = new FakeTransport(); var got = new List<string>();
            return (new HorrorClient(t, got.Add), t, got);
        }

        [Test]
        public void NullTransport_IsOffline_AndSendsNothing()
        {
            var got = new List<string>();
            var c = new HorrorClient(null, got.Add);
            Assert.AreEqual(ServerState.Offline, c.State);
            Assert.IsFalse(c.Ask(Body, 0f));
            Assert.AreEqual(0, c.RequestsSent);
        }

        [Test]
        public void ValidReply_DeliversSuggestion_AndEchoedIdIsUsedInBody()
        {
            var (c, t, got) = Make();
            Assert.IsTrue(c.Ask(Body, 0f));
            StringAssert.Contains(t.Sent[0].id, t.Sent[0].json);
            t.Sent[0].done(new TransportResult { Ok = true, Body = Reply(t.Sent[0].id) });
            Assert.AreEqual(1, got.Count);
            Assert.AreEqual(ServerState.Online, c.State);
        }

        [Test]
        public void OnlyOneInFlight_AndMinimumGapIsEnforced()
        {
            var (c, t, _) = Make();
            Assert.IsTrue(c.Ask(Body, 0f));
            Assert.IsFalse(c.Ask(Body, 1f), "one already pending");
            t.Sent[0].done(new TransportResult { Ok = true, Body = Reply(t.Sent[0].id) });
            Assert.IsFalse(c.Ask(Body, 44f), "inside the 45 s gap");
            Assert.IsTrue(c.Ask(Body, 45f));
        }

        [Test]
        public void ServerError_Timeout_AndWrongId_DeliverNothing_AndDoNotRetry()
        {
            var (c, t, got) = Make();
            c.Ask(Body, 0f);
            t.Sent[0].done(new TransportResult { Ok = false, Error = "500" });
            Assert.AreEqual(ServerState.Failed, c.State);

            c.Ask(Body, 50f);
            c.Tick(53f);
            Assert.AreEqual("timeout", c.LastError);
            t.Sent[1].done(new TransportResult { Ok = true, Body = Reply(t.Sent[1].id) });   // late answer is ignored

            c.Ask(Body, 100f);
            t.Sent[2].done(new TransportResult { Ok = true, Body = Reply("someone-else") });
            Assert.AreEqual(0, got.Count);
            Assert.AreEqual(3, c.RequestsSent, "no retries");
        }
    }
}
