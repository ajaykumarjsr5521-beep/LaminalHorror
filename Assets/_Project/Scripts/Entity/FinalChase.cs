using UnityEngine;
using NocturneAnnex.Core;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.Entity
{
    /// <summary>
    /// The last room's sting (F-14l): opening the exit door is loud, so the stalker comes for the player right as they run for the way out.
    /// It uses the ordinary noise path, so the entity's behaviour is the same as for any other loud door.
    /// </summary>
    public sealed class FinalChase : MonoBehaviour
    {
        public Door ExitDoor;
        public NoiseHub Hub;
        public float Scale = 2f;

        public bool Triggered { get; private set; }

        void OnEnable() { if (ExitDoor != null) ExitDoor.OnOpened.AddListener(Trigger); }
        void OnDisable() { if (ExitDoor != null) ExitDoor.OnOpened.RemoveListener(Trigger); }

        public void Trigger()
        {
            if (Triggered) return;
            Triggered = true;
            if (ExitDoor != null && Hub != null) Hub.Emit(ExitDoor.transform.position, NoiseKind.DoorSlam, Scale);
            Captions.Post("ending.run");
        }
    }
}
