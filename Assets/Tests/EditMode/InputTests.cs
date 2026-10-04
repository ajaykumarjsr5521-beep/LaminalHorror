using NUnit.Framework;
using UnityEngine;
using NocturneAnnex.Controls;

namespace NocturneAnnex.Tests.EditMode
{
    public class InputTests
    {
        [Test]
        public void Deadzone_InsideZone_IsZero() =>
            Assert.AreEqual(Vector2.zero, InputMath.ApplyDeadzone(new Vector2(0.1f, 0.1f), 0.2f));

        [Test]
        public void Deadzone_FullDeflection_IsOne() =>
            Assert.AreEqual(1f, InputMath.ApplyDeadzone(Vector2.right, 0.2f).magnitude, 1e-4f);

        [Test]
        public void Stick_ClampsToUnitLength() =>
            Assert.AreEqual(1f, InputMath.StickFromOffset(new Vector2(400, 0), 100f).magnitude, 1e-4f);

        [Test]
        public void Stick_ZeroRadius_IsZero() =>
            Assert.AreEqual(Vector2.zero, InputMath.StickFromOffset(Vector2.one, 0f));

        [TestCase(0.5f, 0.8f)]
        [TestCase(1f, 1f)]
        [TestCase(2f, 1.3f)]
        public void ControlsScale_IsClampedTo80To130Percent(float input, float expected) =>
            Assert.AreEqual(expected, InputMath.ClampControlsScale(input), 1e-4f);

        class FixedSource : IInputSource
        {
            public PlayerInputState State;
            public bool WasReset;
            public void Poll(ref PlayerInputState s) => s = State;
            public void ResetState() => WasReset = true;
        }

        [Test]
        public void Aggregator_MergesMoveClampedAndButtonsOr()
        {
            var a = new FixedSource { State = new PlayerInputState { Move = Vector2.up, Sprint = true } };
            var b = new FixedSource { State = new PlayerInputState { Move = Vector2.up, InteractPressed = true } };
            var agg = new InputAggregator();
            agg.Add(a);
            agg.Add(b);
            var r = agg.Poll();
            Assert.LessOrEqual(r.Move.magnitude, 1f + 1e-4f);
            Assert.IsTrue(r.Sprint);
            Assert.IsTrue(r.InteractPressed);
        }

        [Test]
        public void Aggregator_ResetAll_ResetsEverySource()
        {
            var a = new FixedSource();
            var b = new FixedSource();
            var agg = new InputAggregator();
            agg.Add(a);
            agg.Add(b);
            agg.ResetAll();
            Assert.IsTrue(a.WasReset && b.WasReset);
        }

        [Test]
        public void Touch_InteractIsOneFrame_LookIsConsumed()
        {
            var t = new TouchInputSource();
            t.PressInteract();
            t.AddLook(new Vector2(3, 4));
            var s1 = new PlayerInputState();
            t.Poll(ref s1);
            var s2 = new PlayerInputState();
            t.Poll(ref s2);
            Assert.IsTrue(s1.InteractPressed);
            Assert.AreEqual(new Vector2(3, 4), s1.Look);
            Assert.IsFalse(s2.InteractPressed);
            Assert.AreEqual(Vector2.zero, s2.Look);
        }

        [Test]
        public void Touch_ResetClearsHeldInput()
        {
            var t = new TouchInputSource();
            t.SetMove(Vector2.one);
            t.SetSprint(true);
            t.SetCrouch(true);
            t.ResetState();
            var s = new PlayerInputState();
            t.Poll(ref s);
            Assert.AreEqual(Vector2.zero, s.Move);
            Assert.IsFalse(s.Sprint);
            Assert.IsFalse(s.Crouch);
        }
    }
}
