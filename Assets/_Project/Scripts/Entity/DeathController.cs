using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NocturneAnnex.Core;
using NocturneAnnex.Level;
using NocturneAnnex.Player;
using NocturneAnnex.Save;

namespace NocturneAnnex.Entity
{
    /// <summary>
    /// What happens when the entity gets the player: a short death sequence, then a life is spent. The first death restores the
    /// last checkpoint with progress kept (the save is refreshed first, so puzzles solved since the checkpoint survive). The
    /// second death ends the run: lives refill, the save is cleared and the level starts again. Lives live in SaveGame.
    /// </summary>
    public class DeathController : MonoBehaviour
    {
        public StalkerAgent Stalker;
        public LevelBootstrap Level;
        public SaveGame Save;
        [Tooltip("The 1 s black-only version, for players who skip the sequence.")] public bool ShortSequence;
        public float RunOverTextSeconds = 3f;

        public bool Running { get; private set; }
        public int DeathsHandled { get; private set; }
        public LifeOutcome LastOutcome { get; private set; }

        Image _black;
        TextMeshProUGUI _text;
        float _runClock;

        void Awake() => BuildOverlay();

        void OnEnable() { if (Stalker != null) Stalker.CaughtPlayer += OnCaught; }
        void OnDisable() { if (Stalker != null) Stalker.CaughtPlayer -= OnCaught; }

        void Update() { if (!Running) _runClock += Time.deltaTime; }

        void OnCaught()
        {
            if (Running) return;
            StartCoroutine(Sequence());
        }

        IEnumerator Sequence()
        {
            Running = true;
            var player = Level.Player;
            var motor = player.GetComponent<PlayerMotor>();
            var look = player.GetComponent<PlayerLook>();
            if (motor != null) motor.enabled = false;
            if (look != null) look.enabled = false;

            var spot = FindOccupiedSpot();
            var record = new DeathRecord
            {
                Checkpoint = Level.Progress != null ? Level.Progress.CurrentId : "",
                Cause = spot != null ? "found_hiding" : "caught",
                HidingSpot = spot != null ? spot.name : "",
                Position = player.position,
                RunSeconds = _runClock
            };

            var seq = new DeathSequence(ShortSequence);
            for (float t = 0f; t < seq.Total; t += Time.unscaledDeltaTime)
            {
                SetBlack(seq.BlackAt(t));
                if (!Accessibility.ReduceMotion && Stalker != null && !ShortSequence) FaceEntity(player);
                yield return null;
            }
            SetBlack(1f);

            if (spot != null) spot.Leave();
            LastOutcome = Save.Lives.LoseLife(record);
            DeathsHandled++;
            if (LastOutcome == LifeOutcome.Respawn)
            {
                Save.SaveCheckpoint(Level.Progress.CurrentId);   // keeps puzzles and items gained since the checkpoint
                Level.Begin(true);
                Captions.Post("run.lives_left_1");
            }
            else
            {
                SetText(Loc.Get("run.over"));
                yield return new WaitForSecondsRealtime(RunOverTextSeconds);
                SetText("");
                Save.StartNewGame();
                Level.Begin(false);
                _runClock = 0f;
            }
            Stalker.ResetToHome();
            yield return new WaitForSecondsRealtime(0.4f);

            if (motor != null) motor.enabled = true;
            if (look != null) look.enabled = true;
            for (float a = 1f; a > 0f; a -= Time.unscaledDeltaTime) { SetBlack(a); yield return null; }
            SetBlack(0f);
            Running = false;
        }

        HideSpot FindOccupiedSpot()
        {
            foreach (var s in FindObjectsByType<HideSpot>(FindObjectsSortMode.None)) if (s.Occupied) return s;
            return null;
        }

        void FaceEntity(Transform player)
        {
            var d = Stalker.transform.position - player.position; d.y = 0f;
            if (d.sqrMagnitude < 0.01f) return;
            var want = Quaternion.LookRotation(d);
            player.rotation = Quaternion.RotateTowards(player.rotation, want, 240f * Time.unscaledDeltaTime);
        }

        void BuildOverlay()
        {
            var canvasGo = new GameObject("DeathOverlay", typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var imgGo = new GameObject("Black", typeof(Image));
            imgGo.transform.SetParent(canvasGo.transform, false);
            _black = imgGo.GetComponent<Image>();
            _black.color = new Color(0, 0, 0, 0);
            _black.raycastTarget = false;
            var rt = _black.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var txtGo = new GameObject("Text", typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(canvasGo.transform, false);
            _text = txtGo.GetComponent<TextMeshProUGUI>();
            _text.alignment = TextAlignmentOptions.Center;
            _text.fontSize = 36f * Accessibility.TextScale;
            _text.color = new Color(0.75f, 0.75f, 0.75f);
            _text.raycastTarget = false;
            var trt = _text.rectTransform;
            trt.anchorMin = new Vector2(0.1f, 0.4f); trt.anchorMax = new Vector2(0.9f, 0.6f); trt.offsetMin = trt.offsetMax = Vector2.zero;
            canvasGo.SetActive(true);
        }

        void SetBlack(float a) { var c = _black.color; c.a = a; _black.color = c; _black.raycastTarget = a > 0.01f; }

        void SetText(string s) => _text.text = s;

        /// <summary>Current screen blackness, for tests.</summary>
        public float Black => _black != null ? _black.color.a : 0f;
    }
}
