using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NocturneAnnex.Core;
using NocturneAnnex.Level;
using NocturneAnnex.Player;
using NocturneAnnex.Save;

namespace NocturneAnnex.Entity
{
    /// <summary>
    /// The ending (F-14l). Leaving the last room raises RoomFlow.Escaped: fade to black, a silence beat, the door slam, then the card with
    /// the run's numbers and a Menu button, and after a few seconds the closing shot of two eyes in an upper window. Everything fades
    /// steadily (no flashing), so it is safe with Reduce Flicker and Reduce Motion. The card holds the modal gate only while it is up;
    /// Menu clears the save (a finished run cannot be continued), releases the gate and restores time.
    /// </summary>
    public sealed class EndingController : MonoBehaviour
    {
        const string MenuScene = "MainMenu";

        public SaveGame Save;
        public Transform Player;
        public float FadeSeconds = 1.5f, SilenceSeconds = 1.5f, EyesDelaySeconds = 4f, EyesFadeSeconds = 3f;

        /// <summary>Overridable for tests; defaults to loading the main menu.</summary>
        public Action<string> SceneLoader = name => SceneManager.LoadScene(name);

        public bool Showing { get; private set; }
        public bool CardVisible { get; private set; }
        public float EyesAlpha => _eyes != null ? _eyes.alpha : 0f;
        public float Black => _black != null ? _black.color.a : 0f;
        public string[] StatLines { get; private set; } = new string[0];
        public Button MenuButton { get; private set; }

        Image _black;
        GameObject _card;
        CanvasGroup _eyes;
        ModalGate.Handle _gate;

        void Awake() => Build();
        void OnEnable() => RoomFlow.Escaped += OnEscaped;
        void OnDisable() { RoomFlow.Escaped -= OnEscaped; ReleaseGate(); }

        void OnEscaped()
        {
            if (Showing) return;
            StartCoroutine(Sequence());
        }

        /// <summary>The numbers for the card, taken from the save.</summary>
        public EndingStats CurrentStats() => new EndingStats(
            Save.Stats.Seconds, Save.Lives.Deaths, Save.Lives.Left, RunLives.MaxLives,
            Save.Rooms.HighestRoom, RoomCatalog.Count, Save.Stats.SecretCount);

        IEnumerator Sequence()
        {
            Showing = true;
            if (Player != null)
            {
                var motor = Player.GetComponent<PlayerMotor>(); if (motor != null) motor.enabled = false;
                var look = Player.GetComponent<PlayerLook>(); if (look != null) look.enabled = false;
            }
            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime) { SetBlack(t / FadeSeconds); yield return null; }
            SetBlack(1f);
            yield return new WaitForSecondsRealtime(SilenceSeconds);
            Cues.Play("event.door_slam", Player != null ? Player.position : (Vector3?)null);
            yield return new WaitForSecondsRealtime(0.8f);
            ShowCard();
            yield return new WaitForSecondsRealtime(EyesDelaySeconds);
            for (float t = 0f; t < EyesFadeSeconds; t += Time.unscaledDeltaTime) { _eyes.alpha = t / EyesFadeSeconds * 0.9f; yield return null; }
            _eyes.alpha = 0.9f;
        }

        /// <summary>Shows the card now. Public so a test can skip the timing.</summary>
        public void ShowCard()
        {
            SetBlack(1f);
            StatLines = EndingText.Lines(CurrentStats());
            _card.transform.Find("Stats").GetComponent<TextMeshProUGUI>().text = string.Join("\n", StatLines);
            _card.SetActive(true);
            CardVisible = true;
            Showing = true;
            if (_gate == null) _gate = ModalGate.Open();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }

        /// <summary>Menu button: a finished run is cleared, the gate and time are restored, the main menu loads.</summary>
        public void GoToMenu()
        {
            if (Save != null) Save.StartNewGame();
            ReleaseGate();
            ModalGate.Reset();
            Time.timeScale = 1f;
            CardVisible = false;
            Showing = false;
            SceneLoader(MenuScene);
        }

        void ReleaseGate() { if (_gate != null) { _gate.Dispose(); _gate = null; } }

        void SetBlack(float a) { var c = _black.color; c.a = a; _black.color = c; _black.raycastTarget = a > 0.01f; }

        void Build()
        {
            var go = new GameObject("EndingOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;
            _black = Stretch(NewImage(go.transform, "Black", new Color(0, 0, 0, 0)));
            _black.raycastTarget = false;

            _eyes = new GameObject("Eyes", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<CanvasGroup>();
            _eyes.transform.SetParent(go.transform, false);
            _eyes.alpha = 0f; _eyes.blocksRaycasts = false; _eyes.interactable = false;
            var rt = (RectTransform)_eyes.transform;
            rt.anchorMin = new Vector2(0.4f, 0.82f); rt.anchorMax = new Vector2(0.6f, 0.88f); rt.offsetMin = rt.offsetMax = Vector2.zero;
            for (int i = 0; i < 2; i++)
            {
                var e = NewImage(_eyes.transform, "Eye_" + i, new Color(0.9f, 0.9f, 0.75f, 1f));
                var er = e.rectTransform;
                er.anchorMin = new Vector2(i == 0 ? 0.1f : 0.65f, 0.3f); er.anchorMax = new Vector2(i == 0 ? 0.35f : 0.9f, 0.7f);
                er.offsetMin = er.offsetMax = Vector2.zero;
                e.raycastTarget = false;
            }

            _card = new GameObject("Card", typeof(RectTransform));
            _card.transform.SetParent(go.transform, false);
            var crt = (RectTransform)_card.transform;
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one; crt.offsetMin = crt.offsetMax = Vector2.zero;
            Label(_card.transform, "Congrats", Loc.Get("ending.congrats"), 40f, new Vector2(0.1f, 0.68f), new Vector2(0.9f, 0.82f));
            Label(_card.transform, "Title", Loc.Get("ending.title"), 56f, new Vector2(0.1f, 0.56f), new Vector2(0.9f, 0.68f));
            Label(_card.transform, "Stats", "", 30f, new Vector2(0.2f, 0.26f), new Vector2(0.8f, 0.54f));
            var btnImg = NewImage(_card.transform, "Menu", new Color(0.18f, 0.18f, 0.18f, 0.95f));
            var brt = btnImg.rectTransform;
            brt.anchorMin = new Vector2(0.38f, 0.12f); brt.anchorMax = new Vector2(0.62f, 0.2f); brt.offsetMin = brt.offsetMax = Vector2.zero;
            MenuButton = btnImg.gameObject.AddComponent<Button>();
            MenuButton.targetGraphic = btnImg;
            MenuButton.onClick.AddListener(GoToMenu);
            Label(btnImg.transform, "Text", Loc.Get("ending.menu"), 28f, Vector2.zero, Vector2.one);
            _card.SetActive(false);
        }

        static Image NewImage(Transform parent, string name, Color c)
        {
            var g = new GameObject(name, typeof(Image));
            g.transform.SetParent(parent, false);
            var img = g.GetComponent<Image>(); img.color = c;
            return img;
        }

        static Image Stretch(Image img)
        {
            var rt = img.rectTransform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        static void Label(Transform parent, string name, string text, float size, Vector2 min, Vector2 max)
        {
            var g = new GameObject(name, typeof(TextMeshProUGUI));
            g.transform.SetParent(parent, false);
            var t = g.GetComponent<TextMeshProUGUI>();
            t.text = text; t.alignment = TextAlignmentOptions.Center; t.fontSize = size * Accessibility.TextScale;
            t.color = new Color(0.85f, 0.85f, 0.85f); t.raycastTarget = false;
            var rt = t.rectTransform; rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
