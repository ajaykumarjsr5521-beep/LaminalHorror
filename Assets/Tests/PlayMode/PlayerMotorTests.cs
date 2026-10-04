using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NocturneAnnex.Controls;
using NocturneAnnex.Player;

namespace NocturneAnnex.Tests.PlayMode
{
    public class PlayerMotorTests
    {
        GameObject _root;
        PlayerInputState _input;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TestRoot");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(_root.transform);
            floor.transform.position = new Vector3(0, -0.5f, 100f);
            floor.transform.localScale = new Vector3(40, 1, 400);
            _input = default;
        }

        [TearDown]
        public void TearDown() => Object.Destroy(_root);

        PlayerMotor SpawnPlayer()
        {
            var go = new GameObject("Player");
            go.transform.SetParent(_root.transform);
            go.transform.position = new Vector3(0, 0.1f, 0);
            go.AddComponent<CharacterController>();
            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(go.transform, false);
            var m = go.AddComponent<PlayerMotor>();
            m.CameraPivot = pivot;
            m.InputProvider = () => _input;
            return m;
        }

        [UnityTest]
        public IEnumerator Walk_ReachesAboutThreeMetersPerSecond()
        {
            var m = SpawnPlayer();
            _input.Move = Vector2.up;
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(3.0f, m.HorizontalVelocity.magnitude, 0.15f);
        }

        [UnityTest]
        public IEnumerator Sprint_ReachesAboutFiveMetersPerSecond()
        {
            var m = SpawnPlayer();
            _input.Move = Vector2.up;
            _input.Sprint = true;
            yield return new WaitForSeconds(0.8f);
            Assert.AreEqual(5.0f, m.HorizontalVelocity.magnitude, 0.2f);
        }

        [UnityTest]
        public IEnumerator Crouch_SlowsToAboutOnePointFiveAndShrinks()
        {
            var m = SpawnPlayer();
            _input.Move = Vector2.up;
            _input.Crouch = true;
            yield return new WaitForSeconds(0.8f);
            Assert.IsTrue(m.IsCrouched);
            Assert.AreEqual(1.5f, m.HorizontalVelocity.magnitude, 0.1f);
            Assert.AreEqual(m.CrouchHeight, m.GetComponent<CharacterController>().height, 0.05f);
        }

        [UnityTest]
        public IEnumerator Sprint_IsIgnoredWhileCrouched()
        {
            var m = SpawnPlayer();
            _input.Move = Vector2.up;
            _input.Crouch = true;
            _input.Sprint = true;
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(m.IsSprinting);
        }

        [UnityTest]
        public IEnumerator CannotStandUp_UnderLowCeiling()
        {
            var m = SpawnPlayer();
            _input.Crouch = true;
            yield return new WaitForSeconds(0.5f);

            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.transform.SetParent(_root.transform);
            ceiling.transform.position = new Vector3(0, 1.4f, 0);   // lower than 1.8 m standing height
            ceiling.transform.localScale = new Vector3(4, 0.2f, 4);
            yield return new WaitForFixedUpdate();

            _input.Crouch = false;
            yield return new WaitForSeconds(0.7f);
            Assert.IsTrue(m.IsCrouched, "must stay crouched under a low ceiling");

            Object.Destroy(ceiling);
            yield return new WaitForSeconds(0.7f);
            Assert.IsFalse(m.IsCrouched, "should stand once headroom is clear");
        }

        [UnityTest]
        public IEnumerator Walking_IntoWall_DoesNotClip()
        {
            var m = SpawnPlayer();
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.SetParent(_root.transform);
            wall.transform.position = new Vector3(0, 1.5f, 6f);
            wall.transform.localScale = new Vector3(10, 3, 0.4f);   // front face at z = 5.8
            yield return new WaitForFixedUpdate();

            _input.Move = Vector2.up;
            _input.Sprint = true;
            yield return new WaitForSeconds(3f);
            Assert.Less(m.transform.position.z + 0.5f, 5.8f + 0.05f, "player capsule passed through wall");
        }

        [UnityTest]
        public IEnumerator Look_ZeroInput_DoesNotDrift()
        {
            var go = new GameObject("Look");
            go.transform.SetParent(_root.transform);
            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(go.transform, false);
            var look = go.AddComponent<PlayerLook>();
            look.CameraPivot = pivot;
            look.InputProvider = () => _input;
            var yaw0 = go.transform.rotation;
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(yaw0, go.transform.rotation);
            Assert.AreEqual(Quaternion.identity, pivot.localRotation);
        }

        [UnityTest]
        public IEnumerator Look_PitchIsClamped()
        {
            var go = new GameObject("Look");
            go.transform.SetParent(_root.transform);
            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(go.transform, false);
            var look = go.AddComponent<PlayerLook>();
            look.CameraPivot = pivot;
            look.InputProvider = () => new PlayerInputState { Look = new Vector2(0, -500f) };
            yield return null;
            yield return null;
            Assert.AreEqual(look.MaxPitch, pivot.localEulerAngles.x, 0.01f);
        }
    }
}
