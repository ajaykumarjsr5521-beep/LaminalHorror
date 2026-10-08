using System.Linq;
using NUnit.Framework;
using NocturneAnnex.Strategy;

namespace NocturneAnnex.Tests.EditMode
{
    public class PlayerBehaviorTrackerTests
    {
        static GameEventBus Script(PlayerBehaviorTracker t)
        {
            var bus = new GameEventBus(); t.Attach(bus);
            bus.Publish(new GameEvent(GameEventType.RoomEntered, 0f, 0f, "ROOM_1"));
            bus.Publish(new GameEvent(GameEventType.Move, 10f, 30f, null, MoveMode.Run));
            bus.Publish(new GameEvent(GameEventType.Move, 20f, 60f, null, MoveMode.Walk));
            bus.Publish(new GameEvent(GameEventType.Move, 30f, 10f, null, MoveMode.Crouch));
            bus.Publish(new GameEvent(GameEventType.Noise, 31f, 0.7f));
            bus.Publish(new GameEvent(GameEventType.Hide, 40f, 1f, "cupboard"));
            bus.Publish(new GameEvent(GameEventType.Hide, 50f, 1f, "cupboard"));
            bus.Publish(new GameEvent(GameEventType.Hide, 60f, 0f, "bed"));
            bus.Publish(new GameEvent(GameEventType.RouteUsed, 61f, 0f, "WEST"));
            bus.Publish(new GameEvent(GameEventType.RoomEntered, 70f, 0f, "ROOM_2"));
            bus.Publish(new GameEvent(GameEventType.ChaseEnded, 80f, 12f));
            bus.Publish(new GameEvent(GameEventType.Death, 90f));
            return bus;
        }

        [Test] public void Movement_ratios_sum_to_one()
        {
            var t = new PlayerBehaviorTracker(); Script(t);
            var p = t.Snapshot();
            Assert.AreEqual(1f, p.RunFrequency + p.WalkFrequency + p.CrouchFrequency, 0.0001f);
            Assert.AreEqual(0.3f, p.RunFrequency, 0.0001f);
        }

        [Test] public void Empty_session_is_all_zero_not_nan()
        {
            var p = new PlayerBehaviorTracker().Snapshot();
            Assert.AreEqual(0f, p.RunFrequency + p.WalkFrequency + p.CrouchFrequency);
            Assert.IsFalse(float.IsNaN(p.RiskTolerance) || float.IsNaN(p.NoiseFrequency));
        }

        [Test] public void Snapshot_is_deterministic_for_same_script()
        {
            var a = new PlayerBehaviorTracker(); Script(a);
            var b = new PlayerBehaviorTracker(); Script(b);
            Assert.AreEqual(UnityEngine.JsonUtility.ToJson(a.Snapshot()), UnityEngine.JsonUtility.ToJson(b.Snapshot()));
            var pa = a.Snapshot(); var pb = b.Snapshot();
            CollectionAssert.AreEqual(pa.HideUsageByType.ToList(), pb.HideUsageByType.ToList());
            CollectionAssert.AreEqual(pa.RoomTimeShare.ToList(), pb.RoomTimeShare.ToList());
        }

        [Test] public void Hide_and_route_shares_are_by_type_and_sum_to_one()
        {
            var t = new PlayerBehaviorTracker(); Script(t);
            var p = t.Snapshot();
            Assert.AreEqual(2f / 3f, p.HideUsageByType["cupboard"], 0.0001f);
            Assert.AreEqual(1f, p.HideUsageByType.Values.Sum(), 0.0001f);
            Assert.AreEqual(1f, p.RouteUsage["WEST"], 0.0001f);
            Assert.AreEqual(2f / 3f, p.HideSuccessRate, 0.0001f);
        }

        [Test] public void Counts_deaths_chases_and_room_time()
        {
            var t = new PlayerBehaviorTracker(); Script(t);
            var p = t.Snapshot();
            Assert.AreEqual(1, p.DeathCount);
            Assert.AreEqual(12f, p.AverageChaseSeconds, 0.0001f);
            Assert.AreEqual(1f, p.RoomTimeShare.Values.Sum(), 0.0001f);
            Assert.AreEqual(70f / 90f, p.RoomTimeShare["ROOM_1"], 0.0001f);
        }

        [Test] public void Profile_contains_no_positions_or_spot_ids()
        {
            var t = new PlayerBehaviorTracker(); Script(t);
            string json = UnityEngine.JsonUtility.ToJson(t.Snapshot());
            StringAssert.DoesNotContain("position", json.ToLowerInvariant());
            foreach (var f in typeof(PlayerProfile).GetFields())
            {
                Assert.AreNotEqual(typeof(UnityEngine.Vector3), f.FieldType);
                StringAssert.DoesNotContain("Spot", f.Name);
            }
            CollectionAssert.DoesNotContain(t.Snapshot().HideUsageByType.Keys, "CUPBOARD_6A");
        }

        [Test] public void Risk_rises_with_running_and_noise()
        {
            var calm = new PlayerBehaviorTracker(); var bus = new GameEventBus(); calm.Attach(bus);
            bus.Publish(new GameEvent(GameEventType.Move, 60f, 60f, null, MoveMode.Crouch));
            var wild = new PlayerBehaviorTracker(); var bus2 = new GameEventBus(); wild.Attach(bus2);
            bus2.Publish(new GameEvent(GameEventType.Move, 60f, 60f, null, MoveMode.Run));
            for (int i = 0; i < 10; i++) bus2.Publish(new GameEvent(GameEventType.Noise, 60f, 0.8f));
            Assert.Greater(wild.Snapshot().RiskTolerance, calm.Snapshot().RiskTolerance);
        }
    }
}
