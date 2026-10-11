using System;
using UnityEngine;
using NocturneAnnex.Horror;

namespace NocturneAnnex.Entity
{
    /// <summary>
    /// Makes entity-linked scares real: when one of the listed events plays, a noise is emitted where it happened, so the stalker may come to
    /// investigate. The noise kind is Machinery, which the strategic layer ignores (a scare is not something the player did).
    /// </summary>
    public sealed class ScareNoiseLink : MonoBehaviour
    {
        [Serializable]
        public class Link
        {
            public string EventId = "";
            public Transform At;
            public float Scale = 1f;
        }

        public HorrorEventRunner Runner;
        public NoiseHub Hub;
        public Link[] Links = new Link[0];

        void OnEnable() { if (Runner != null) Runner.EventFired += OnEvent; }
        void OnDisable() { if (Runner != null) Runner.EventFired -= OnEvent; }

        void OnEvent(string id)
        {
            if (Hub == null) return;
            foreach (var l in Links)
                if (l != null && l.EventId == id && l.At != null) Hub.Emit(l.At.position, NoiseKind.Machinery, l.Scale);
        }
    }
}
