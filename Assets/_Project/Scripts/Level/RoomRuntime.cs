using System.Collections;
using UnityEngine;
using NocturneAnnex.Core;
using NocturneAnnex.Interaction;
using NocturneAnnex.Save;

namespace NocturneAnnex.Level
{
    /// <summary>
    /// Per-room glue, run once a frame after the level has started (so the save is already applied): puts the player at the back door when
    /// they came from the next room, keeps opened doors open to the key and taken items gone, and shows the room's one return change.
    /// Opened doors and taken items are saved because a used key is gone for good and the scene reloads with everything locked.
    /// </summary>
    public sealed class RoomRuntime : MonoBehaviour
    {
        public SaveGame Save;
        public Transform Player;
        public int RoomIndex = 1;
        public Transform BackSpawn;

        [Header("Return change (see RoomDefinition.ReturnChange)")]
        public ReturnChangeKind Change;
        public Transform Chair;
        public Vector3 ChairOffset = new Vector3(0.9f, 0f, 0.7f);
        public Light DeadLight;
        public GameObject RevealedObject;

        public bool Applied { get; private set; }

        IEnumerator Start()
        {
            yield return null;
            Apply();
        }

        /// <summary>Public so tests can run it without waiting for the frame.</summary>
        public void Apply()
        {
            if (Applied) return;
            Applied = true;
            var rooms = Save.Rooms;

            foreach (var door in FindObjectsByType<Door>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string id = door.name;
                if (rooms.IsDoorOpened(RoomIndex, id)) door.Unlock();
                door.OnOpened.AddListener(() => rooms.MarkDoorOpened(RoomIndex, id));
                door.OnMessage.AddListener(text => { if (!string.IsNullOrEmpty(text)) Captions.Post("door.message.locked"); });
            }
            foreach (var sw in FindObjectsByType<UnlockSwitch>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (sw.TargetDoor != null && rooms.IsDoorOpened(RoomIndex, sw.TargetDoor.name)) sw.RestoreUsed();
            foreach (var pickup in FindObjectsByType<Pickup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string id = pickup.ItemId;
                if (rooms.IsItemTaken(RoomIndex, id)) pickup.gameObject.SetActive(false);
                pickup.Collected += (p, i) => rooms.MarkItemTaken(RoomIndex, id);
            }

            ApplyChange(rooms.HasChanged(RoomIndex), RoomFlow.ConsumePendingChange());

            if (RoomFlow.ConsumeArrivedFromBack() && BackSpawn != null && Player != null)
            {
                var cc = Player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;   // a CharacterController overrides transform changes unless it is off while moving
                Player.SetPositionAndRotation(BackSpawn.position, BackSpawn.rotation);
                if (cc != null) cc.enabled = true;
            }
        }

        void ApplyChange(bool changed, bool justFired)
        {
            switch (Change)
            {
                case ReturnChangeKind.ChairMoved:
                    if (changed && Chair != null) Chair.position += ChairOffset;
                    break;
                case ReturnChangeKind.LightDead:
                    if (changed && DeadLight != null) DeadLight.enabled = false;
                    if (justFired) Captions.Post("room.change.light");
                    break;
                case ReturnChangeKind.NewSound:
                    if (justFired) Captions.Post("room.change.sound");
                    break;
                case ReturnChangeKind.RevealKey:
                    if (RevealedObject != null)
                    {
                        var pickup = RevealedObject.GetComponent<Pickup>();
                        bool taken = pickup != null && Save.Rooms.IsItemTaken(RoomIndex, pickup.ItemId);
                        RevealedObject.SetActive(changed && !taken);
                    }
                    if (justFired) Captions.Post("room.change.key");
                    break;
            }
        }
    }
}
