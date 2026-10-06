using System;
using System.Collections.Generic;
using NUnit.Framework;
using NocturneAnnex.Liminal;

namespace NocturneAnnex.Tests.EditMode
{
    public class AnomalyTests
    {
        static AnomalyDefinition A(string id, AnomalyTrigger t = AnomalyTrigger.FirstVisit, EntityPhase min = EntityPhase.Phenomenon, string link = "legend_a") =>
            new AnomalyDefinition { Id = id, EntityId = "guest", EvidenceLinkId = link, Trigger = t, MinPhase = min };

        [Test]
        public void Anomaly_FiresOnlyForItsTriggerAndPhase_AndOnlyOnce()
        {
            var d = new AnomalyDirector(new[] { A("mirror", AnomalyTrigger.LookedAway, EntityPhase.Sighting), A("door", AnomalyTrigger.OnReturn) });
            Assert.IsFalse(d.TryTrigger(AnomalyTrigger.FirstVisit, EntityPhase.Hunt, out _), "nothing authored for that trigger");
            Assert.IsFalse(d.TryTrigger(AnomalyTrigger.LookedAway, EntityPhase.Phenomenon, out _), "phase too early");
            Assert.IsTrue(d.TryTrigger(AnomalyTrigger.LookedAway, EntityPhase.Sighting, out var a));
            Assert.AreEqual("mirror", a.Id);
            Assert.IsFalse(d.TryTrigger(AnomalyTrigger.LookedAway, EntityPhase.Sighting, out _), "never twice");
            Assert.IsTrue(d.TryTrigger(AnomalyTrigger.OnReturn, EntityPhase.Phenomenon, out _));
        }

        [Test]
        public void AnomalyWithoutEvidenceLink_OrDuplicateId_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new AnomalyDirector(new[] { A("x", link: "") }));
            Assert.Throws<ArgumentException>(() => new AnomalyDirector(new[] { A("x"), A("x") }));
            Assert.Throws<ArgumentException>(() => new AnomalyDirector(new[] { A("") }));
            Assert.Throws<ArgumentNullException>(() => new AnomalyDirector(null));
        }

        [Test]
        public void Fired_SnapshotRestore_IgnoresUnknownIds()
        {
            var defs = new[] { A("a"), A("b") };
            var d = new AnomalyDirector(defs);
            d.TryTrigger(AnomalyTrigger.FirstVisit, EntityPhase.Hunt, out _);
            var snap = d.SnapshotFired();
            var d2 = new AnomalyDirector(defs);
            d2.RestoreFired(new List<string>(snap) { "gone" });
            Assert.IsTrue(d2.TryTrigger(AnomalyTrigger.FirstVisit, EntityPhase.Hunt, out var next));
            Assert.AreEqual("b", next.Id, "the restored one stays fired");
            Assert.AreEqual(1, snap.Length);
        }

        // ---------- RoomMemory ----------

        static Dictionary<string, string> State(params (string k, string v)[] kv)
        {
            var d = new Dictionary<string, string>();
            foreach (var (k, v) in kv) d[k] = v;
            return d;
        }

        [Test]
        public void RoomMemory_ReportsExactlyTheChangedObjects()
        {
            var m = new RoomMemory();
            m.Record("lobby", State(("chair", "north"), ("clock", "3:00"), ("lamp", "on")), 40f);
            var now = State(("chair", "south"), ("clock", "3:00"), ("vase", "left"));
            CollectionAssert.AreEquivalent(new[] { "chair", "lamp", "vase" }, m.Changed("lobby", now));
            Assert.IsEmpty(m.Changed("lobby", State(("chair", "north"), ("clock", "3:00"), ("lamp", "on"))));
            Assert.IsEmpty(m.Changed("unknown", now));
        }

        [Test]
        public void RoomMemory_ChangeIsFair_OnlyAfterEnoughDwell()
        {
            var m = new RoomMemory();
            m.Record("short", State(("a", "1")), 10f);
            m.Record("long", State(("a", "1")), 31f);
            Assert.IsFalse(m.FairToChange("short"));
            Assert.IsTrue(m.FairToChange("long"));
            Assert.IsFalse(m.FairToChange("never visited"));
            Assert.IsTrue(m.HasRoom("short"));
        }

        // ---------- LevelSpecValidator ----------

        static LevelSpec Good() => new LevelSpec
        {
            Name = "Hotel", HasHeroVista = true, LongestRoomDoorChain = 1, MinPublicCeilingMetres = 5f, ShortestPublicCeilingMetres = 6f, RealtimeLights = 2
        };

        [Test]
        public void Validator_PassesAGoodLevel()
        {
            CollectionAssert.IsEmpty(LevelSpecValidator.Validate(Good()));
        }

        [Test]
        public void Validator_ReportsEachBrokenRuleSeparately()
        {
            var l = Good();
            l.HasHeroVista = false;
            l.LongestRoomDoorChain = 5;
            l.ShortestPublicCeilingMetres = 3f;
            l.RealtimeLights = 6;
            l.EntityCuesWithoutCaption.Add("guest.keycard");
            l.MissingLegendEntries.Add("legend_x");
            var problems = LevelSpecValidator.Validate(l);
            Assert.AreEqual(6, problems.Count);
            StringAssert.Contains("hero vista", problems[0]);
            StringAssert.Contains("room-door-room", problems[1]);
            StringAssert.Contains("ceiling", problems[2]);
            StringAssert.Contains("realtime", problems[3]);
            StringAssert.Contains("guest.keycard", problems[4]);
            StringAssert.Contains("legend_x", problems[5]);
        }
    }
}
