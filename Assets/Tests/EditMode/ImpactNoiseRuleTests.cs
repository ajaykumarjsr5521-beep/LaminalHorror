using NUnit.Framework;
using NocturneAnnex.Entity;

namespace NocturneAnnex.Tests.EditMode
{
    public class ImpactNoiseRuleTests
    {
        [Test]
        public void SlowTapMakesNoNoise() =>
            Assert.IsFalse(ImpactNoiseRule.TryGetKind(1.0f, true, 10f, out _));

        [Test]
        public void FallMakesDropAndThrowMakesThrownImpact()
        {
            Assert.IsTrue(ImpactNoiseRule.TryGetKind(3f, false, 10f, out var fall));
            Assert.AreEqual(NoiseKind.Drop, fall);
            Assert.IsTrue(ImpactNoiseRule.TryGetKind(3f, true, 10f, out var thrown));
            Assert.AreEqual(NoiseKind.ThrownImpact, thrown);
        }

        [Test]
        public void BounceInsideTheGapIsIgnored()
        {
            Assert.IsFalse(ImpactNoiseRule.TryGetKind(5f, true, 0.2f, out _));
            Assert.IsTrue(ImpactNoiseRule.TryGetKind(5f, true, 0.5f, out _));
        }
    }
}
