using NUnit.Framework;
using NocturneAnnex.Entity;
using NocturneAnnex.Strategy;

namespace NocturneAnnex.Tests.EditMode
{
    public class StrategyExecutorTests
    {
        static StrategyCommand Cmd(StrategyType t = StrategyType.IncreaseHidingPressure, float pr = 0.8f, float inten = 0.65f, float dur = 90f) =>
            new StrategyCommand { Strategy = t, Priority = pr, Intensity = inten, DurationSeconds = dur, Confidence = 0.8f, ReasonCode = "TEST" };

        [Test] public void Rejects_unknown_enum() =>
            Assert.AreEqual(StrategyRejection.UnknownStrategy, StrategyValidator.Check(Cmd((StrategyType)99)));

        [Test] public void Rejects_none_strategy() =>
            Assert.AreEqual(StrategyRejection.NoneStrategy, StrategyValidator.Check(Cmd(StrategyType.None)));

        [TestCase(1.2f, 0.5f)] [TestCase(-0.1f, 0.5f)] [TestCase(0.5f, float.NaN)]
        public void Rejects_out_of_range(float pr, float inten) =>
            Assert.AreEqual(StrategyRejection.OutOfRange, StrategyValidator.Check(Cmd(StrategyType.IncreaseHidingPressure, pr, inten)));

        [TestCase(5f)] [TestCase(500f)] [TestCase(float.NaN)]
        public void Rejects_bad_duration(float dur) =>
            Assert.AreEqual(StrategyRejection.BadDuration, StrategyValidator.Check(Cmd(dur: dur)));

        [Test] public void Invalid_command_keeps_current_strategy()
        {
            var ex = new StrategyExecutor();
            Assert.IsTrue(ex.TryApply(Cmd(), 0f));
            Assert.IsFalse(ex.TryApply(Cmd(pr: 2f), 1f));
            Assert.IsTrue(ex.HasActive);
            Assert.AreEqual(StrategyType.IncreaseHidingPressure, ex.Active.Strategy);
        }

        [Test] public void Hiding_pressure_raises_chance_but_never_to_certainty()
        {
            var ex = new StrategyExecutor();
            ex.TryApply(Cmd(pr: 1f, inten: 1f), 0f);
            float c = HideInspection.Chance(0f, 1f, ex.HideBonus);
            Assert.Less(c, 1f);
            Assert.LessOrEqual(c, HideInspection.MaxChance);
            Assert.Greater(HideInspection.Chance(1f, 0f, 0f), 0f);
            Assert.Greater(HideInspection.Chance(0.5f, 0.2f, ex.HideBonus), HideInspection.Chance(0.5f, 0.2f, 0f));
        }

        [Test] public void Biases_stay_in_caps_for_every_strategy()
        {
            var ex = new StrategyExecutor();
            foreach (StrategyType t in System.Enum.GetValues(typeof(StrategyType)))
            {
                if (t == StrategyType.None) continue;
                ex.TryApply(Cmd(t, 1f, 1f), 0f);
                Assert.That(ex.HideBonus, Is.InRange(0f, HideInspection.MaxMemoryBonus));
                Assert.That(ex.InvestigationMultiplier, Is.InRange(StrategyExecutor.MinInvestigationMultiplier, StrategyExecutor.MaxInvestigationMultiplier));
                Assert.That(ex.PatrolBias, Is.InRange(0f, 1f));
                Assert.That(ex.SearchBias, Is.InRange(0f, 1f));
            }
        }

        [Test] public void Expiry_restores_baseline()
        {
            var ex = new StrategyExecutor();
            ex.TryApply(Cmd(StrategyType.IncreaseInvestigation, 1f, 1f, 30f), 100f);
            Assert.Greater(ex.InvestigationMultiplier, 1f);
            ex.Tick(129f);
            Assert.IsTrue(ex.HasActive);
            Assert.AreEqual(1f, ex.Remaining(129f), 0.001f);
            ex.Tick(130f);
            Assert.IsFalse(ex.HasActive);
            Assert.AreEqual(1f, ex.InvestigationMultiplier);
            Assert.AreEqual(0f, ex.HideBonus);
        }

        [Test] public void Relax_lowers_investigation()
        {
            var ex = new StrategyExecutor();
            ex.TryApply(Cmd(StrategyType.RelaxPressure, 1f, 1f), 0f);
            Assert.Less(ex.InvestigationMultiplier, 1f);
        }

        [Test] public void Executor_exposes_no_transform_or_speed()
        {
            foreach (var p in typeof(StrategyExecutor).GetProperties())
            {
                StringAssert.DoesNotContain("Speed", p.Name);
                StringAssert.DoesNotContain("Position", p.Name);
                StringAssert.DoesNotContain("Target", p.Name);
                Assert.AreNotEqual(typeof(UnityEngine.Vector3), p.PropertyType);
                Assert.AreNotEqual(typeof(UnityEngine.Transform), p.PropertyType);
            }
        }
    }
}
