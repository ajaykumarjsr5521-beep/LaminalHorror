using UnityEngine;
using NocturneAnnex.Horror;
using NocturneAnnex.Save;

namespace NocturneAnnex.Level
{
    /// <summary>
    /// Feeds the ending's numbers (F-14l): counts play time while the game runs and records the secrets: each hidden switch used, the toy
    /// box key, and the misfile alcove. Time stops with the game (it uses scaled time), so a pause does not count.
    /// </summary>
    public sealed class RunStatsRecorder : MonoBehaviour
    {
        public const string MisfileEventId = "misfile_alcove";

        public SaveGame Save;
        public HorrorEventRunner Runner;

        void Start()
        {
            foreach (var s in FindObjectsByType<UnlockSwitch>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string id = "switch:" + s.name;
                s.Activated += () => Secret(id);
            }
            foreach (var b in FindObjectsByType<ToyBoxSet>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string id = "toybox:" + b.KeyItemId;
                b.Opened += (i, c) => { if (c == ToyBoxContent.Key) Secret(id); };
            }
            if (Runner != null) Runner.EventFired += OnEvent;
        }

        void OnDestroy() { if (Runner != null) Runner.EventFired -= OnEvent; }

        void Update() { if (Save != null) Save.Stats.AddSeconds(Time.deltaTime); }

        void OnEvent(string id) { if (id == MisfileEventId) Secret("misfile"); }

        void Secret(string id) { if (Save != null) Save.Stats.AddSecret(id); }
    }
}
