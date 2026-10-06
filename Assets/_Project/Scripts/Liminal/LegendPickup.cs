using UnityEngine;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.Liminal
{
    /// <summary>A note that is also a legend entry: reading it shows the text and gives the level's entity evidence, once.</summary>
    public class LegendPickup : Note
    {
        public string EntryId = "";
        public int EvidencePoints = 1;
        public LiminalDirector Director;

        public override void Interact(Interactor interactor)
        {
            base.Interact(interactor);
            if (Director == null) Director = FindFirstObjectByType<LiminalDirector>();
            if (Director != null && !string.IsNullOrEmpty(EntryId)) Director.ReadLegend(EntryId, EvidencePoints);
        }
    }
}
