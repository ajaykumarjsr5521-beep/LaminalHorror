using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NocturneAnnex.Interaction;
using NocturneAnnex.Puzzle;

namespace NocturneAnnex.Tests.PlayMode
{
    public class CodeLockTests
    {
        GameObject _root;
        Door _door;

        [SetUp] public void SetUp() => _root = new GameObject("Root");
        [TearDown] public void TearDown() => Object.Destroy(_root);

        Door SpawnLockedDoor()
        {
            var g = new GameObject("Door");
            g.SetActive(false);
            g.transform.SetParent(_root.transform);
            var d = g.AddComponent<Door>();
            d.RequiredKeyId = "never_in_inventory";
            g.SetActive(true);
            return d;
        }

        CodeLock SpawnLock(string code, Door door)
        {
            var g = new GameObject("Lock");
            g.SetActive(false);
            g.transform.SetParent(_root.transform);
            var l = g.AddComponent<CodeLock>();
            l.Code = code;
            l.TargetDoor = door;
            g.SetActive(true);
            return l;
        }

        static void Type(CodeLock l, string s) { foreach (char c in s) l.Press(c); }

        [Test]
        public void CorrectCode_UnlocksDoor_AndRaisesSolved()
        {
            _door = SpawnLockedDoor();
            var l = SpawnLock("0719", _door);
            bool solved = false;
            l.Solved += () => solved = true;
            Type(l, "0719");
            Assert.AreEqual(SubmitResult.Correct, l.Submit());
            Assert.IsTrue(solved);
            Assert.IsFalse(_door.IsLocked);
            Assert.AreEqual("Unlocked", l.Prompt);
        }

        [Test]
        public void WrongCode_KeepsDoorLocked_RaisesRejected_AndAllowsImmediateRetry()
        {
            _door = SpawnLockedDoor();
            var l = SpawnLock("0719", _door);
            int rejected = 0;
            l.Rejected += () => rejected++;
            Type(l, "1111");
            Assert.AreEqual(SubmitResult.Wrong, l.Submit());
            Assert.AreEqual(1, rejected);
            Assert.IsTrue(_door.IsLocked);
            Assert.AreEqual("", l.Entry);
            Type(l, "0719");
            Assert.AreEqual(SubmitResult.Correct, l.Submit());
        }

        [Test]
        public void Interact_RequestsKeypad_UntilSolved()
        {
            var l = SpawnLock("12", null);
            int requests = 0;
            l.KeypadRequested += _ => requests++;
            l.Interact(null);
            Type(l, "12");
            l.Submit();
            l.Interact(null);
            Assert.AreEqual(1, requests);
        }

        [Test]
        public void EntryChanged_ReportsTypedDigits()
        {
            var l = SpawnLock("1234", null);
            string last = null;
            l.EntryChanged += e => last = e;
            Type(l, "12");
            Assert.AreEqual("12", last);
            l.Backspace();
            Assert.AreEqual("1", last);
        }

        [Test]
        public void InvalidCode_DisablesLock_AndLogsError()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("CodeLock 'Lock' disabled"));
            var l = SpawnLock("12x4", null);
            Assert.IsFalse(l.IsConfigured);
            Assert.AreEqual("", l.Prompt);
            l.Press('1');
            Assert.AreEqual(SubmitResult.Incomplete, l.Submit());
        }

        [Test]
        public void RestoreSolved_UnlocksDoor_AndKeepsLockSolved()
        {
            _door = SpawnLockedDoor();
            var l = SpawnLock("0719", _door);
            l.RestoreSolved(true);
            Assert.IsTrue(l.IsSolved);
            Assert.IsFalse(_door.IsLocked);
        }
    }
}
