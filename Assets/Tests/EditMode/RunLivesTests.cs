using NUnit.Framework;
using UnityEngine;
using NocturneAnnex.Save;

namespace NocturneAnnex.Tests.EditMode
{
    public class RunLivesTests
    {
        static DeathRecord Rec(string cp = "hall", string cause = "caught") =>
            new DeathRecord { Checkpoint = cp, Cause = cause, Position = new Vector3(1, 0, 2), RunSeconds = 33f };

        [Test]
        public void StartsWithTwoLives()
        {
            var l = new RunLives();
            Assert.AreEqual(2, l.Left);
            Assert.AreEqual(0, l.Deaths);
        }

        [Test]
        public void FirstDeathRespawnsSecondEndsTheRun()
        {
            var l = new RunLives();
            Assert.AreEqual(LifeOutcome.Respawn, l.LoseLife(Rec()));
            Assert.AreEqual(1, l.Left);
            Assert.AreEqual(LifeOutcome.RunOver, l.LoseLife(Rec()));
            Assert.AreEqual(0, l.Left);
            Assert.AreEqual(2, l.Deaths);
        }

        [Test]
        public void LivesNeverGoNegative()
        {
            var l = new RunLives();
            l.LoseLife(); l.LoseLife(); l.LoseLife();
            Assert.AreEqual(0, l.Left);
            Assert.AreEqual(3, l.Deaths);
        }

        [Test]
        public void NewRunRefillsLivesButKeepsTheLog()
        {
            var l = new RunLives();
            l.LoseLife(Rec()); l.LoseLife(Rec());
            l.NewRun();
            Assert.AreEqual(2, l.Left);
            Assert.AreEqual(0, l.Deaths);
            Assert.AreEqual(2, l.Log.Count);
            l.ClearLog();
            Assert.AreEqual(0, l.Log.Count);
        }

        [Test]
        public void LogIsCapped()
        {
            var l = new RunLives();
            for (int i = 0; i < RunLives.MaxLog + 5; i++) l.LoseLife(Rec("cp" + i));
            Assert.AreEqual(RunLives.MaxLog, l.Log.Count);
            Assert.AreEqual("cp" + 5, l.Log[0].Checkpoint);
        }

        [Test]
        public void RestoreHandlesOldAndBadValues()
        {
            var l = new RunLives();
            l.Restore(-1, 0, null);                 // an older save has no life count
            Assert.AreEqual(2, l.Left);
            l.Restore(99, -4, null);
            Assert.AreEqual(2, l.Left);
            Assert.AreEqual(0, l.Deaths);
            l.Restore(1, 1, new[] { Rec(), null });
            Assert.AreEqual(1, l.Left);
            Assert.AreEqual(1, l.Log.Count);
        }

        [Test]
        public void SavedCheckpointKeepsLivesAndDeathLog()
        {
            var data = new SaveData
            {
                CheckpointId = "records", LivesLeft = 1, DeathCount = 1,
                DeathLog = new[] { Rec("hall", "found_hiding") }
            };
            data.DeathLog[0].HidingSpot = "Cupboard_Hall";
            var result = SaveSerializer.FromJson(SaveSerializer.ToJson(data));
            Assert.IsTrue(result.Ok);
            Assert.AreEqual(1, result.Data.LivesLeft);
            Assert.AreEqual(1, result.Data.DeathCount);
            Assert.AreEqual("Cupboard_Hall", result.Data.DeathLog[0].HidingSpot);
            Assert.AreEqual(new Vector3(1, 0, 2), result.Data.DeathLog[0].Position);
        }

        [Test]
        public void OlderSaveWithoutLivesReadsAsFullLives()
        {
            const string old = "{\"Version\":1,\"CheckpointId\":\"hall\",\"InventoryIds\":[],\"SolvedPuzzleIds\":[],\"FiredEventIds\":[]}";
            var result = SaveSerializer.FromJson(old);
            Assert.IsTrue(result.Ok);
            Assert.AreEqual(-1, result.Data.LivesLeft);
            var l = new RunLives();
            l.Restore(result.Data.LivesLeft, result.Data.DeathCount, result.Data.DeathLog);
            Assert.AreEqual(2, l.Left);
        }
    }
}
