#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using NocturneAnnex.Controls;
using NocturneAnnex.Core;
using NocturneAnnex.Flow;
using NocturneAnnex.Level;
using NocturneAnnex.Puzzle;

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>Drives the touch controls on the real Level_B1 HUD with simulated pointer events (no real fingers).</summary>
    public class LevelTouchControlsTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Level_B1.unity";

        TouchControlsVisibility _visibility;
        GameObject _stick, _look, _interact, _sprint, _crouch, _buttons;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            LevelLaunch.RequestNewGame();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _visibility = Object.FindFirstObjectByType<TouchControlsVisibility>(FindObjectsInactive.Include);
            Assert.IsNotNull(_visibility, "the level HUD must contain the touch controls");
            _visibility.Override = true;
            _visibility.Refresh();
            _stick = Find<VirtualStick>();
            _look = Find<TouchLookArea>();
            _buttons = Find<TouchControlsScaler>();
            var buttons = _visibility.GetComponentsInChildren<TouchButton>(true);
            _interact = buttons.First(b => b.name == "Interact").gameObject;
            _sprint = buttons.First(b => b.name == "Sprint").gameObject;
            _crouch = buttons.First(b => b.name == "Crouch").gameObject;
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            ControlsLayout.Scale = 1f;
            ModalGate.Reset();
            Time.timeScale = 1f;
        }

        static GameObject Find<T>() where T : Component =>
            Object.FindFirstObjectByType<T>(FindObjectsInactive.Include).gameObject;

        static PointerEventData Pointer(int id, Vector2 pos, Vector2 delta = default) =>
            new PointerEventData(EventSystem.current) { pointerId = id, position = pos, delta = delta };

        static PlayerInputState Current => InputRouter.Instance.Current;

        [UnityTest]
        public IEnumerator MoveLookAndSprint_HeldTogether_AllRegister()
        {
            var stickPointer = Pointer(1, new Vector2(300f, 300f));
            ExecuteEvents.Execute(_stick, stickPointer, ExecuteEvents.pointerDownHandler);
            stickPointer.position = new Vector2(600f, 300f);   // well past the radius, so full deflection
            ExecuteEvents.Execute(_stick, stickPointer, ExecuteEvents.dragHandler);

            var lookPointer = Pointer(2, new Vector2(1500f, 500f));
            ExecuteEvents.Execute(_look, lookPointer, ExecuteEvents.pointerDownHandler);
            lookPointer.delta = new Vector2(80f, 0f);
            ExecuteEvents.Execute(_look, lookPointer, ExecuteEvents.dragHandler);

            ExecuteEvents.Execute(_sprint, Pointer(3, new Vector2(1800f, 300f)), ExecuteEvents.pointerDownHandler);
            yield return null;

            Assert.Greater(Current.Move.x, 0.9f, "stick");
            Assert.Greater(Current.Look.x, 0f, "look");
            Assert.IsTrue(Current.Sprint, "sprint button");

            ExecuteEvents.Execute(_stick, stickPointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(_sprint, Pointer(3, default), ExecuteEvents.pointerUpHandler);
            yield return null;
            Assert.AreEqual(Vector2.zero, Current.Move);
            Assert.IsFalse(Current.Sprint);
        }

        [UnityTest]
        public IEnumerator InteractButton_IsAOneFramePress()
        {
            ExecuteEvents.Execute(_interact, Pointer(1, default), ExecuteEvents.pointerDownHandler);
            yield return null;
            Assert.IsTrue(Current.InteractPressed);
            yield return null;
            Assert.IsFalse(Current.InteractPressed, "a press lasts one frame");
        }

        [UnityTest]
        public IEnumerator Crouch_IsHeldWhilePressed_AndReleasedWhenTheControlsAreHidden()
        {
            ExecuteEvents.Execute(_crouch, Pointer(1, default), ExecuteEvents.pointerDownHandler);
            yield return null;
            Assert.IsTrue(Current.Crouch);

            _visibility.Override = false;
            _visibility.Refresh();   // hiding must not leave the button stuck down
            yield return null;
            Assert.IsFalse(Current.Crouch);
        }

        [UnityTest]
        public IEnumerator Visibility_FollowsTheRule()
        {
            _visibility.Override = false;
            _visibility.Refresh();
            Assert.IsFalse(_visibility.Content.activeSelf);
            _visibility.Override = true;
            _visibility.Refresh();
            Assert.IsTrue(_visibility.Content.activeSelf);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ControlSizeSetting_RescalesTheButtons_WithinTheClamp()
        {
            ControlsLayout.Scale = 1.3f;
            yield return null;
            Assert.AreEqual(1.3f, _buttons.transform.localScale.x, 0.001f);
            ControlsLayout.Scale = 0.8f;
            yield return null;
            Assert.AreEqual(0.8f, _buttons.transform.localScale.x, 0.001f);
            ControlsLayout.Scale = 5f;
            yield return null;
            Assert.AreEqual(1.3f, _buttons.transform.localScale.x, 0.001f, "the setting is clamped to 130%");
        }

        [UnityTest]
        public IEnumerator OpenKeypad_BlocksTouchesToTheControlsUnderneath()
        {
            Object.FindFirstObjectByType<LevelBootstrap>().FinalLock.Interact(null);
            yield return null;
            var results = new List<RaycastResult>();
            var data = new PointerEventData(EventSystem.current) { position = new Vector2(Screen.width * 0.25f, Screen.height * 0.5f) };
            EventSystem.current.RaycastAll(data, results);
            Assert.IsNotEmpty(results);
            Assert.IsFalse(results[0].gameObject.transform.IsChildOf(_visibility.transform), "the keypad must catch the touch first");
        }
    }
}
#endif
