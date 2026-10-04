using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.Tests.PlayMode
{
    public class InteractableTests
    {
        class FakeInventory : MonoBehaviour, IKeyProvider, IItemReceiver
        {
            public string Key = "";
            public bool Accepts = true;
            public string Added;
            public string Consumed;
            public bool HasKey(string keyId) => keyId == Key;
            public bool ConsumeKey(string keyId) { Consumed = keyId; return keyId == Key; }
            public bool TryAdd(string itemId) { if (Accepts) Added = itemId; return Accepts; }
        }

        GameObject _root;

        [SetUp] public void SetUp() => _root = new GameObject("Root");
        [TearDown] public void TearDown() => Object.Destroy(_root);

        Interactor SpawnInteractor(out FakeInventory inv)
        {
            var go = new GameObject("Player");
            go.transform.SetParent(_root.transform);
            inv = go.AddComponent<FakeInventory>();
            return go.AddComponent<Interactor>();   // Awake finds the fake inventory
        }

        Door SpawnDoor(string key)
        {
            var go = new GameObject("Door");
            go.SetActive(false);                      // set fields before Awake
            go.transform.SetParent(_root.transform);
            var d = go.AddComponent<Door>();
            d.RequiredKeyId = key;
            go.SetActive(true);
            return d;
        }

        [Test]
        public void UnlockedDoor_TogglesOpenAndClosed()
        {
            var d = SpawnDoor("");
            Assert.AreEqual("Open", d.Prompt);
            d.Interact(null);
            Assert.IsTrue(d.IsOpen);
            Assert.AreEqual("Close", d.Prompt);
            d.Interact(null);
            Assert.IsFalse(d.IsOpen);
        }

        [Test]
        public void LockedDoor_WithoutKey_StaysClosedAndGivesGenericMessage()
        {
            var d = SpawnDoor("brass_key");
            var i = SpawnInteractor(out var inv);
            string msg = null;
            d.OnMessage.AddListener(m => msg = m);
            d.Interact(i);
            Assert.IsFalse(d.IsOpen);
            Assert.IsTrue(d.IsLocked);
            Assert.AreEqual(Door.LockedMessage, msg);
            StringAssert.DoesNotContain("brass", msg);
            StringAssert.DoesNotContain("key", msg.ToLowerInvariant());
        }

        [Test]
        public void LockedDoor_WithKey_UnlocksAndOpens()
        {
            var d = SpawnDoor("brass_key");
            var i = SpawnInteractor(out var inv);
            inv.Key = "brass_key";
            d.Interact(i);
            Assert.IsFalse(d.IsLocked);
            Assert.IsTrue(d.IsOpen);
        }

        [Test]
        public void LockedDoor_Unlocking_ConsumesTheKey()
        {
            var d = SpawnDoor("brass_key");
            var i = SpawnInteractor(out var inv);
            inv.Key = "brass_key";
            d.Interact(i);
            Assert.AreEqual("brass_key", inv.Consumed);
        }

        [Test]
        public void Unlock_RemovesLock_WithoutOpening()
        {
            var d = SpawnDoor("brass_key");
            d.Unlock();
            Assert.IsFalse(d.IsLocked);
            Assert.IsFalse(d.IsOpen);
            d.Interact(null);
            Assert.IsTrue(d.IsOpen);
        }

        [Test]
        public void LockedDoor_WithWrongKey_StaysLocked()
        {
            var d = SpawnDoor("brass_key");
            var i = SpawnInteractor(out var inv);
            inv.Key = "other";
            d.Interact(i);
            Assert.IsTrue(d.IsLocked);
            Assert.IsFalse(d.IsOpen);
        }

        [UnityTest]
        public IEnumerator OpenDoor_RotatesHingeTowardOpenAngle()
        {
            var d = SpawnDoor("");
            d.Interact(null);
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(d.OpenAngle, d.Hinge.localEulerAngles.y, 0.5f);
        }

        [Test]
        public void Pickup_AddsToInventoryAndDeactivates()
        {
            var go = new GameObject("Pickup");
            go.transform.SetParent(_root.transform);
            var p = go.AddComponent<Pickup>();
            p.ItemId = "brass_key";
            var i = SpawnInteractor(out var inv);
            p.Interact(i);
            Assert.AreEqual("brass_key", inv.Added);
            Assert.IsFalse(go.activeSelf);
        }

        [Test]
        public void Pickup_WhenInventoryRefuses_StaysInWorld()
        {
            var go = new GameObject("Pickup");
            go.transform.SetParent(_root.transform);
            var p = go.AddComponent<Pickup>();
            var i = SpawnInteractor(out var inv);
            inv.Accepts = false;
            string refusal = null;
            p.Refused += (_, m) => refusal = m;
            p.Interact(i);
            Assert.IsTrue(go.activeSelf);
            Assert.IsNotNull(refusal);
        }

        [Test]
        public void Note_RaisesOpenedEvent()
        {
            var go = new GameObject("Note");
            go.transform.SetParent(_root.transform);
            var n = go.AddComponent<Note>();
            n.Title = "Memo";
            Note got = null;
            void Handler(Note x) => got = x;
            Note.Opened += Handler;
            try { n.Interact(null); }
            finally { Note.Opened -= Handler; }
            Assert.AreSame(n, got);
        }
    }
}
