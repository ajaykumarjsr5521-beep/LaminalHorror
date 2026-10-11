using System.Linq;
using NUnit.Framework;
using NocturneAnnex.Level;

namespace NocturneAnnex.Tests.EditMode
{
    public class RoomCatalogTests
    {
        [Test]
        public void TheCatalogValidates_WithNoProblems()
        {
            var problems = RoomValidator.Validate(RoomCatalog.All);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        [Test]
        public void TenRooms_EachWithExactlyOneProgressionDoor()
        {
            Assert.AreEqual(10, RoomCatalog.All.Count);
            foreach (var r in RoomCatalog.All) Assert.AreEqual(1, r.Doors.Count(d => d.Kind == DoorKind.Progression), r.Id);
        }

        [Test]
        public void Room5Key_OpensRoom7()
        {
            Assert.IsTrue(RoomCatalog.Get(5).Keys.Any(k => k.Id == RoomCatalog.Room5KeyId));
            Assert.AreEqual(RoomCatalog.Room5KeyId, RoomCatalog.Get(7).Progression.UnlockId);
        }

        [Test]
        public void Difficulty_RisesMonotonically()
        {
            for (int i = 2; i <= 10; i++)
            {
                var a = RoomCatalog.Get(i - 1).Difficulty; var b = RoomCatalog.Get(i).Difficulty;
                Assert.GreaterOrEqual(b.Threat, a.Threat, "room " + i);
                Assert.LessOrEqual(b.InspectIntervalSeconds, a.InspectIntervalSeconds, "room " + i);
                Assert.GreaterOrEqual(b.InspectRange, a.InspectRange, "room " + i);
            }
        }

        // The validator must actually catch the mistakes it exists for.
        static System.Collections.Generic.List<RoomDefinition> Fresh()
        {
            // rebuild a mutable copy by re-running the catalog builder through reflection-free cloning of what the tests change
            return RoomCatalog.All.Select(Clone).ToList();
        }

        static RoomDefinition Clone(RoomDefinition r)
        {
            var c = new RoomDefinition
            {
                Index = r.Index, Id = r.Id, Title = r.Title, HideSpots = r.HideSpots, NoiseProps = r.NoiseProps, ReturnChange = r.ReturnChange,
                RevealedKeyId = r.RevealedKeyId, Code = r.Code,
                Difficulty = new DifficultySpec(r.Difficulty.Threat, r.Difficulty.InspectIntervalSeconds, r.Difficulty.InspectRange, r.Difficulty.PatrolPoints)
            };
            c.Doors.AddRange(r.Doors.Select(d => new DoorSpec(d.Id, d.Kind, d.Unlock, d.UnlockId)));
            c.Keys.AddRange(r.Keys.Select(k => new KeySpec(k.Id, k.Placement, k.BehindDoorId)));
            c.Switches.AddRange(r.Switches.Select(s => new SwitchSpec(s.Id, s.RequiredItemId, s.BehindDoorId)));
            return c;
        }

        [Test]
        public void Validator_CatchesASecondProgressionDoor()
        {
            var rooms = Fresh();
            rooms[1].Doors.First(d => d.Id == "r02_fake").Kind = DoorKind.Progression;
            Assert.That(RoomValidator.Validate(rooms), Has.Some.Contains("exactly one progression door"));
        }

        [Test]
        public void Validator_CatchesASoftlock_KeyBehindItsOwnDoor()
        {
            var rooms = Fresh();
            rooms[2].Keys.First(k => k.Id == "key_r3a").Placement = KeyPlacement.BehindDoor;
            rooms[2].Keys.First(k => k.Id == "key_r3a").BehindDoorId = "r03_closet";
            var problems = RoomValidator.Validate(rooms);
            Assert.That(problems, Has.Some.Contains("softlock").Or.Contains("behind the door it opens"));
        }

        [Test]
        public void Validator_CatchesAnOpenDoorOutsideRoom1()
        {
            var rooms = Fresh();
            rooms[3].Doors.First(d => d.Id == "r04_fake").Kind = DoorKind.Open;
            Assert.That(RoomValidator.Validate(rooms), Has.Some.Contains("only room 1"));
        }

        [Test]
        public void Validator_CatchesFallingDifficulty()
        {
            var rooms = Fresh();
            rooms[5].Difficulty.Threat = 0.1f;
            Assert.That(RoomValidator.Validate(rooms), Has.Some.Contains("threat falls"));
        }

        [Test]
        public void Validator_CatchesABrokenRoom5To7Link()
        {
            var rooms = Fresh();
            rooms[6].Doors.First(d => d.Id == "prog").UnlockId = "key_other";
            Assert.That(RoomValidator.Validate(rooms), Has.Some.Contains("room 5 key"));
        }

        [Test]
        public void ItemIds_IncludeEveryKeyAndTheFuse()
        {
            var ids = RoomCatalog.AllItemIds().ToList();
            CollectionAssert.Contains(ids, RoomCatalog.Room5KeyId);
            CollectionAssert.Contains(ids, RoomCatalog.FuseId);
            CollectionAssert.Contains(ids, "key_store");
        }
    }
}
