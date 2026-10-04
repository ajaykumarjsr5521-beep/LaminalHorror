using UnityEngine;

namespace NocturneAnnex.Controls
{
    /// <summary>Scene singleton exposing the merged per-frame input. Runs before gameplay scripts.</summary>
    [DefaultExecutionOrder(-100)]
    public class InputRouter : MonoBehaviour
    {
        public static InputRouter Instance { get; private set; }

        /// <summary>Look sensitivity multiplier (settings, F-07).</summary>
        [Range(0.2f, 3f)] public float LookSensitivity = 1f;
        public bool InvertY;

        public TouchInputSource Touch { get; private set; }
        public PlayerInputState Current { get; private set; }

        readonly InputAggregator _aggregator = new InputAggregator();
        DeviceInputSource _device;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _device = new DeviceInputSource();
            Touch = new TouchInputSource();
            _aggregator.Add(_device);
            _aggregator.Add(Touch);
        }

        void Update()
        {
            var s = _aggregator.Poll();
            s.Look *= LookSensitivity;
            if (InvertY) s.Look.y = -s.Look.y;
            Current = s;
        }

        void OnApplicationPause(bool paused) { if (paused) _aggregator.ResetAll(); }
        void OnApplicationFocus(bool focus) { if (!focus) _aggregator.ResetAll(); }

        void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            _device?.Dispose();
        }
    }
}
