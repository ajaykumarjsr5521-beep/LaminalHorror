using NUnit.Framework;
using UnityEngine;
using NocturneAnnex.Entity;

namespace NocturneAnnex.Tests.EditMode
{
    public class EntityBrainTests
    {
        static BrainInput Noise(float x, float score = 10f, float roll = 0.9f) =>
            new BrainInput { Heard = true, NoisePos = new Vector3(x, 0, 0), NoiseScore = score, Roll = roll };

        static BrainInput Sight(float x, float roll = 0.9f) =>
            new BrainInput { Sees = true, SeenPos = new Vector3(x, 0, 0), Roll = roll };

        static void Run(EntityBrain b, float seconds, BrainInput input = default)
        {
            for (float t = 0; t < seconds; t += 0.1f) b.Step(0.1f, input);
        }

        [Test]
        public void NoiseLeadsToListenThenInvestigateSearchCooldownPatrol()
        {
            var b = new EntityBrain();
            b.Step(0.1f, Noise(12f));
            Assert.AreEqual(EntityState.Listen, b.State);
            Run(b, EntityBrain.ListenSeconds + 0.2f);
            Assert.AreEqual(EntityState.Investigate, b.State);
            Assert.AreEqual(12f, b.Target.x, 1e-4f);
            b.Step(0.1f, new BrainInput { Arrived = true });
            Assert.AreEqual(EntityState.Search, b.State);
            Run(b, EntityBrain.SearchSeconds + 0.3f);
            Assert.AreEqual(EntityState.Cooldown, b.State);
            Run(b, EntityBrain.CooldownSeconds + 0.3f);
            Assert.AreEqual(EntityState.Patrol, b.State);
        }

        [Test]
        public void NoInputNeverLeavesPatrol()
        {
            var b = new EntityBrain();
            Run(b, 600f);
            Assert.AreEqual(EntityState.Patrol, b.State);
        }

        [Test]
        public void SightingGoesThroughListenBeforeChase()
        {
            var b = new EntityBrain();
            b.Step(0.1f, Sight(4f));
            Assert.AreEqual(EntityState.Listen, b.State);
            Run(b, EntityBrain.ListenSeconds + 0.2f, Sight(4f));
            Assert.AreEqual(EntityState.Chase, b.State);
        }

        [Test]
        public void CalmStatesNeverSkipListen()
        {
            var rng = new System.Random(7);
            var b = new EntityBrain();
            var prev = b.State;
            b.StateChanged += (from, to) =>
            {
                if (from == EntityState.Patrol || from == EntityState.Cooldown)
                    Assert.That(to == EntityState.Listen || to == EntityState.Patrol, $"{from} -> {to}");
            };
            for (int i = 0; i < 20000; i++)
            {
                var input = new BrainInput
                {
                    Heard = rng.NextDouble() < 0.02, NoisePos = new Vector3((float)rng.NextDouble() * 20, 0, 0),
                    NoiseScore = (float)rng.NextDouble() * 15, Sees = rng.NextDouble() < 0.01,
                    SeenPos = Vector3.zero, Arrived = rng.NextDouble() < 0.05, Roll = (float)rng.NextDouble()
                };
                b.Step(0.1f, input);
            }
        }

        [Test]
        public void WatchNeverTurnsIntoChaseWithoutNewTrigger()
        {
            var b = new EntityBrain();
            b.Step(0.1f, Sight(4f, roll: 0.05f));
            Run(b, EntityBrain.ListenSeconds + 0.2f, Sight(4f, roll: 0.05f));
            Assert.AreEqual(EntityState.Watch, b.State);
            Run(b, EntityBrain.WatchSeconds + 0.3f);
            Assert.AreEqual(EntityState.Cooldown, b.State);
        }

        [Test]
        public void LoudNoiseDuringWatchRestartsListen()
        {
            var b = new EntityBrain();
            b.Step(0.1f, Sight(4f, roll: 0.05f));
            Run(b, EntityBrain.ListenSeconds + 0.2f, Sight(4f, roll: 0.05f));
            b.Step(0.1f, Noise(3f, score: 12f));
            Assert.AreEqual(EntityState.Listen, b.State);
        }

        [Test]
        public void ChaseEndsAfterTwentySecondsWithoutContact()
        {
            var b = new EntityBrain();
            b.Step(0.1f, Sight(4f));
            Run(b, EntityBrain.ListenSeconds + 0.2f, Sight(4f));
            Run(b, 15f, Sight(4f));                       // contact keeps the chase alive
            Assert.AreEqual(EntityState.Chase, b.State);
            Run(b, EntityBrain.ChaseLoseSeconds - 1f);
            Assert.AreEqual(EntityState.Chase, b.State);
            Run(b, 2f);
            Assert.AreEqual(EntityState.Search, b.State);
        }

        [Test]
        public void ChaseIsSlowerThanRunningPlayerButFasterThanWalking()
        {
            const float walk = 1f, run = 1.7f;
            float chase = EntityBrain.SpeedMultiplier(EntityState.Chase) * walk;
            Assert.Greater(chase, walk);
            Assert.Less(chase, 0.9f * run + 0.001f);
        }

        [Test]
        public void SightModelRespectsWallsLightAndRange()
        {
            var eye = Vector3.zero;
            Assert.IsTrue(SightModel.CanSee(eye, new Vector3(5f, 0, 0), true, false));
            Assert.IsFalse(SightModel.CanSee(eye, new Vector3(5f, 0, 0), true, true));    // wall or closed door
            Assert.IsFalse(SightModel.CanSee(eye, new Vector3(5f, 0, 0), false, false));  // dark
            Assert.IsFalse(SightModel.CanSee(eye, new Vector3(7f, 0, 0), true, false));   // too far
        }

        [Test]
        public void StrongNoiseIsNeverIgnoredByWatchRoll()
        {
            var b = new EntityBrain();
            b.Step(0.1f, Noise(10f, score: 12f, roll: 0.05f));
            Run(b, EntityBrain.ListenSeconds + 0.2f);
            Assert.AreEqual(EntityState.Investigate, b.State);
        }
    }
}
