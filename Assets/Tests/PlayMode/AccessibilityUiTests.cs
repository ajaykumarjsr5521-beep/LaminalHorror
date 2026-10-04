#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using NocturneAnnex.Core;
using NocturneAnnex.UI;

namespace NocturneAnnex.Tests.PlayMode
{
    public class AccessibilityUiTests
    {
        const string CaptionsPath = "Assets/_Project/Prefabs/UI/Captions.prefab";

        GameObject _root;

        [SetUp]
        public void SetUp()
        {
            Accessibility.Reset();
            _root = new GameObject("Root", typeof(Canvas));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Accessibility.Reset();
            Captions.Service.Clear();
        }

        TMP_Text MakeScalable(float size, bool autosize = false)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(_root.transform);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.fontSize = size;
            if (autosize)
            {
                t.enableAutoSizing = true;
                t.fontSizeMin = 20;
                t.fontSizeMax = 40;
            }
            go.AddComponent<ScalableText>();
            return t;
        }

        // ---------- ScalableText ----------

        [Test]
        public void ScalableText_FollowsTextSize_AndNeverCompounds()
        {
            var t = MakeScalable(40f);
            Assert.AreEqual(40f, t.fontSize, 1e-3f);

            Accessibility.Set(true, Accessibility.LargeText, false, false);
            Assert.AreEqual(50f, t.fontSize, 1e-3f);
            Accessibility.Set(true, Accessibility.SmallText, false, false);
            Assert.AreEqual(34f, t.fontSize, 1e-3f);
            Accessibility.Set(true, Accessibility.LargeText, false, false);
            Accessibility.Set(true, Accessibility.LargeText, true, false);   // unrelated change: size must stay 50
            Assert.AreEqual(50f, t.fontSize, 1e-3f);
            Accessibility.Set(true, Accessibility.MediumText, false, false);
            Assert.AreEqual(40f, t.fontSize, 1e-3f, "back to the authored size exactly");
        }

        [Test]
        public void ScalableText_AlsoScalesAutosizeLimits()
        {
            var t = MakeScalable(40f, autosize: true);
            Accessibility.Set(true, Accessibility.LargeText, false, false);
            Assert.AreEqual(25f, t.fontSizeMin, 1e-3f);
            Assert.AreEqual(50f, t.fontSizeMax, 1e-3f);
        }

        [Test]
        public void ScalableText_PicksUpTheCurrentSizeWhenItAppearsLate()
        {
            Accessibility.Set(true, Accessibility.LargeText, false, false);
            var t = MakeScalable(40f);
            Assert.AreEqual(50f, t.fontSize, 1e-3f);
        }

        // ---------- CaptionView ----------

        CaptionView MakeView(CaptionService service)
        {
            var go = new GameObject("Captions");
            go.SetActive(false);
            go.transform.SetParent(_root.transform);
            var view = go.AddComponent<CaptionView>();
            view.Service = service;
            view.Panel = new GameObject("Panel");
            view.Panel.transform.SetParent(go.transform);
            view.Label = view.Panel.AddComponent<TextMeshProUGUI>();
            go.SetActive(true);
            return view;
        }

        [Test]
        public void CaptionView_HiddenWhenEmpty_ShowsPostedLines()
        {
            var service = new CaptionService();
            var view = MakeView(service);
            Assert.IsFalse(view.Panel.activeSelf);

            service.Post("door.message.locked", Time.unscaledTime, 5f);
            Assert.IsTrue(view.Panel.activeSelf);
            Assert.AreEqual(Loc.Get("door.message.locked"), view.Label.text);
        }

        [UnityTest]
        public IEnumerator CaptionView_HidesAgainWhenTheCueExpires_EvenWhilePaused()
        {
            var service = new CaptionService();
            var view = MakeView(service);
            float oldScale = Time.timeScale;
            Time.timeScale = 0f;   // game paused: captions must still expire (unscaled time)
            try
            {
                service.Post("door.message.locked", Time.unscaledTime, 0.3f);
                Assert.IsTrue(view.Panel.activeSelf);
                yield return new WaitForSecondsRealtime(0.7f);
                Assert.IsFalse(view.Panel.activeSelf);
            }
            finally { Time.timeScale = oldScale; }
        }

        [Test]
        public void CaptionView_StopsListeningWhenDisabled()
        {
            var service = new CaptionService();
            var view = MakeView(service);
            view.gameObject.SetActive(false);
            service.Post("door.message.locked", Time.unscaledTime, 5f);
            Assert.IsFalse(view.Panel.activeSelf, "a disabled view must not react to the service");
        }

        [Test]
        public void CaptionsPrefab_ShowsViaTheSharedFacade_AndRespectsTheSetting()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CaptionsPath);
            Assert.IsNotNull(prefab, "missing prefab: run GameplayUiBuilder.CreateAll");
            var view = Object.Instantiate(prefab, _root.transform).GetComponent<CaptionView>();
            Assert.IsFalse(view.Panel.activeSelf);

            Captions.Post("pickup.refused", 5f);
            Assert.IsTrue(view.Panel.activeSelf);
            Assert.AreEqual(Loc.Get("pickup.refused"), view.Label.text);

            Captions.Service.Clear();
            Accessibility.Set(false, Accessibility.MediumText, false, false);
            Captions.Post("pickup.refused", 5f);
            Assert.IsFalse(view.Panel.activeSelf, "captions off means nothing is shown");
        }
    }
}
#endif
