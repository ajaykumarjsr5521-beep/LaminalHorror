using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using NocturneAnnex.Controls;
using NocturneAnnex.Interaction;

namespace NocturneAnnex.Tests.PlayMode
{
    public class PromptViewTests
    {
        class Probe : MonoBehaviour, IInteractable
        {
            public string Prompt => "Open";
            public void Interact(Interactor i) { }
        }

        [UnityTest]
        public IEnumerator Prompt_ShowsWhenFocused_HidesWhenNot()
        {
            var root = new GameObject("Root");
            var camGo = new GameObject("Cam");
            camGo.transform.SetParent(root.transform);
            camGo.transform.position = Vector3.up * 1.6f;
            var cam = camGo.AddComponent<Camera>();
            var interactor = camGo.AddComponent<Interactor>();
            interactor.ViewCamera = cam;
            interactor.InputProvider = () => default(PlayerInputState);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(root.transform);
            var label = labelGo.AddComponent<TextMeshProUGUI>();
            var viewGo = new GameObject("View");
            viewGo.SetActive(false);
            viewGo.transform.SetParent(root.transform);
            var view = viewGo.AddComponent<InteractionPromptView>();
            view.Interactor = interactor;
            view.Label = label;
            viewGo.SetActive(true);
            Assert.IsFalse(labelGo.activeSelf, "hidden with nothing in focus");

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(root.transform);
            cube.transform.position = new Vector3(0, 1.6f, 1.5f);
            cube.AddComponent<Probe>();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.IsTrue(labelGo.activeSelf);
            Assert.AreEqual("Open", label.text);

            Object.Destroy(cube);
            yield return null;
            yield return null;
            Assert.IsFalse(labelGo.activeSelf);
            Object.Destroy(root);
        }
    }
}
