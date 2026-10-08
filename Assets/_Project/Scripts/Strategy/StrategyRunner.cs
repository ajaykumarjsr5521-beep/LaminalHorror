using UnityEngine;

namespace NocturneAnnex.Strategy
{
    /// <summary>
    /// Scene host for the strategic layer. It ticks once per second (not per frame), asks the server only on triggers, and works
    /// with no server at all: set Online to false (default) and the executor simply stays at baseline.
    /// Real-time AI reads Executor.HideBonus, InvestigationMultiplier, PatrolBias and SearchBias; nothing else crosses over.
    /// </summary>
    public sealed class StrategyRunner : MonoBehaviour
    {
        public bool Online = false;
        public string ServerUrl = "http://127.0.0.1:8765";
        public bool ShowDebugPanel = false;

        public GameEventBus Bus { get; private set; }
        public PlayerBehaviorTracker Tracker { get; private set; }
        public StrategyExecutor Executor { get; private set; }
        public StrategyClient Client { get; private set; }

        void Awake()
        {
            Online = PlayerPrefs.GetInt("strategy.online", Online ? 1 : 0) == 1;
            ServerUrl = PlayerPrefs.GetString("strategy.url", ServerUrl);
            Bus = new GameEventBus();
            Tracker = new PlayerBehaviorTracker();
            Tracker.Attach(Bus);
            Executor = new StrategyExecutor();
            Client = new StrategyClient(Online ? new HttpStrategyTransport(this, ServerUrl) : null, Executor);
        }

        void OnEnable() { InvokeRepeating(nameof(SlowTick), 1f, 1f); }
        void OnDisable() { CancelInvoke(nameof(SlowTick)); }

        void SlowTick()
        {
            float now = Time.time;
            Executor.Tick(now);
            Client.Tick(now);
        }

        /// <summary>Call on meaningful triggers: NEW_ROOM, DEATH, ENCOUNTER_ENDED, STRATEGY_EXPIRED, BEHAVIOR_SHIFT.</summary>
        public void Trigger(string trigger) => Client.Request(Tracker.Snapshot(), trigger, Time.time);

        void OnGUI()
        {
            if (!ShowDebugPanel) return;
            GUI.Box(new Rect(10, 10, 360, 220), StrategyDebugText.Build(Executor, Client, Time.time));
        }
    }
}
