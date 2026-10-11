using UnityEngine;
using UnityEngine.UI;
using NocturneAnnex.Audio;
using NocturneAnnex.Core;

namespace NocturneAnnex.Entity
{
    /// <summary>
    /// Level host for F-14h: turns entity distance and state into DangerLevel, drives the heartbeat and ambience mix, and shows a vignette
    /// and a small camera shake that honour the reduce-motion and reduce-flicker settings. Only the level leaves this class.
    /// </summary>
    public sealed class DangerController : MonoBehaviour
    {
        public StalkerAgent Stalker;
        public Transform Player;
        public HeartbeatPlayer Heartbeat;
        public Camera ShakeCamera;

        public DangerModel Model { get; } = new DangerModel();
        public int Level => Model.Level;
        /// <summary>0..1 estimate for the strategic layer.</summary>
        public float Stress => Level / 4f;
        public float VignetteAlpha { get; private set; }
        public float ShakeApplied { get; private set; }

        HideSpot[] _spots = new HideSpot[0];
        Image _vignette;
        Vector3 _shakeOffset;
        float _phase;

        void Start()
        {
            _spots = FindObjectsByType<HideSpot>(FindObjectsSortMode.None);
            BuildVignette();
        }

        void Update() => Tick(Time.deltaTime);

        /// <summary>One frame. Public so tests can drive it.</summary>
        public void Tick(float dt)
        {
            if (Stalker == null || Player == null) return;
            float distance = Vector3.Distance(Stalker.transform.position, Player.position);
            Model.Step(dt, distance, Stalker.Brain.State);
            bool hiding = false;
            foreach (var s in _spots) if (s != null && s.Occupied) { hiding = true; break; }
            if (Heartbeat != null) { Heartbeat.Level = Level; Heartbeat.Hiding = hiding; }

            float bpm = Heartbeat != null ? Heartbeat.Mix.Bpm : 60f;
            _phase = Mathf.Repeat(_phase + dt * bpm / 60f, 1f);
            VignetteAlpha = DangerPulseModel.VignetteAlpha(Level, _phase, Accessibility.ReduceMotion, Accessibility.ReduceFlicker);
            if (_vignette != null) { var c = _vignette.color; c.a = VignetteAlpha; _vignette.color = c; }
        }

        void LateUpdate()
        {
            if (ShakeCamera == null) return;
            ShakeCamera.transform.localPosition -= _shakeOffset;   // remove last frame's shake before adding the new one
            ShakeApplied = DangerPulseModel.ShakeAmplitude(Level, Accessibility.ReduceMotion);
            _shakeOffset = ShakeApplied > 0f ? Random.insideUnitSphere * ShakeApplied : Vector3.zero;
            ShakeCamera.transform.localPosition += _shakeOffset;
        }

        void BuildVignette()
        {
            var canvasGo = new GameObject("DangerVignette", typeof(Canvas));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
            var imgGo = new GameObject("Tint", typeof(RectTransform), typeof(Image));
            imgGo.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)imgGo.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            _vignette = imgGo.GetComponent<Image>();
            _vignette.raycastTarget = false;
            _vignette.color = new Color(0.35f, 0f, 0f, 0f);
        }
    }
}
