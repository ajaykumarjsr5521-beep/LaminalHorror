using System;
using System.Collections.Generic;
using NUnit.Framework;
using NocturneAnnex.Liminal;

namespace NocturneAnnex.Tests.EditMode
{
    public class RuleSystemTests
    {
        readonly HashSet<string> _flags = new HashSet<string>();
        bool Flag(string f) => _flags.Contains(f);

        [SetUp]
        public void SetUp() => _flags.Clear();   // NUnit reuses one fixture instance for all tests

        static RuleSystem Make()
        {
            var s = new RuleSystem();
            s.Add(new Rule("dont_turn", "TurnedAround"));
            s.Add(new Rule("dont_enter_elevator", "EnteredElevator"));
            return s;
        }

        [Test]
        public void Violation_FiresOnce_NotEveryFrame()
        {
            var s = Make();
            Assert.IsEmpty(s.Evaluate(Flag));
            _flags.Add("TurnedAround");
            CollectionAssert.AreEqual(new[] { "dont_turn" }, s.Evaluate(Flag));
            Assert.IsEmpty(s.Evaluate(Flag), "still true: already reported");
            Assert.IsEmpty(s.Evaluate(Flag));
        }

        [Test]
        public void Violation_FiresAgain_OnlyAfterTheFlagWentFalse()
        {
            var s = Make();
            _flags.Add("TurnedAround");
            s.Evaluate(Flag);
            _flags.Remove("TurnedAround");
            s.Evaluate(Flag);
            _flags.Add("TurnedAround");
            CollectionAssert.AreEqual(new[] { "dont_turn" }, s.Evaluate(Flag));
        }

        [Test]
        public void TwoRulesBrokenTogether_AreBothReported_AndRaiseTheEvent()
        {
            var s = Make();
            var seen = new List<string>();
            s.Violated += seen.Add;
            _flags.Add("TurnedAround"); _flags.Add("EnteredElevator");
            Assert.AreEqual(2, s.Evaluate(Flag).Count);
            CollectionAssert.AreEquivalent(new[] { "dont_turn", "dont_enter_elevator" }, seen);
        }

        [Test]
        public void Mutation_NeedsAnAnnouncement_AndReplacesTheRule()
        {
            var s = Make();
            Assert.Throws<InvalidOperationException>(() => s.Mutate("dont_turn", new Rule("dont_stop", "Stopped"), announced: false));
            string from = null, to = null;
            s.Mutated += (a, b) => { from = a; to = b; };
            s.Mutate("dont_turn", new Rule("dont_stop", "Stopped"), announced: true);
            Assert.AreEqual("dont_turn", from);
            Assert.AreEqual("dont_stop", to);
            _flags.Add("TurnedAround");
            Assert.IsEmpty(s.Evaluate(Flag), "the old rule no longer applies");
            _flags.Add("Stopped");
            CollectionAssert.AreEqual(new[] { "dont_stop" }, s.Evaluate(Flag));
        }

        [Test]
        public void BadRules_Throw()
        {
            Assert.Throws<ArgumentException>(() => new Rule("", "x"));
            Assert.Throws<ArgumentException>(() => new Rule("x", ""));
            var s = Make();
            Assert.Throws<ArgumentException>(() => s.Add(new Rule("dont_turn", "Other")));
            Assert.Throws<ArgumentException>(() => s.Mutate("missing", new Rule("a", "b"), true));
            Assert.Throws<ArgumentException>(() => s.Mutate("dont_turn", new Rule("dont_enter_elevator", "z"), true));
        }
    }
}
