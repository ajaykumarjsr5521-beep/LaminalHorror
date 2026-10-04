using System;
using NUnit.Framework;
using NocturneAnnex.Puzzle;

namespace NocturneAnnex.Tests.EditMode
{
    public class CodeLockModelTests
    {
        static void Type(CodeLockModel m, string digits)
        {
            foreach (char c in digits) m.EnterDigit(c);
        }

        [Test]
        public void CorrectCode_Solves()
        {
            var m = new CodeLockModel("0719");
            Type(m, "0719");
            Assert.AreEqual(SubmitResult.Correct, m.Submit());
            Assert.IsTrue(m.IsSolved);
        }

        [Test]
        public void WrongCode_IsRejected_AndClearsEntry()
        {
            var m = new CodeLockModel("0719");
            Type(m, "1234");
            Assert.AreEqual(SubmitResult.Wrong, m.Submit());
            Assert.IsFalse(m.IsSolved);
            Assert.AreEqual("", m.Entry);
        }

        [Test]
        public void WrongCode_NeverLocksPlayerOut_ManyAttemptsThenCorrect()
        {
            var m = new CodeLockModel("0719");
            for (int i = 0; i < 100; i++)
            {
                Type(m, "0000");
                Assert.AreEqual(SubmitResult.Wrong, m.Submit());
            }
            Type(m, "0719");
            Assert.AreEqual(SubmitResult.Correct, m.Submit());
        }

        [Test]
        public void ShortEntry_IsIncomplete_AndKeptForEditing()
        {
            var m = new CodeLockModel("0719");
            Type(m, "07");
            Assert.AreEqual(SubmitResult.Incomplete, m.Submit());
            Assert.AreEqual("07", m.Entry);
        }

        [Test]
        public void Entry_StopsAtCodeLength_AndIgnoresNonDigits()
        {
            var m = new CodeLockModel("12");
            Assert.IsTrue(m.EnterDigit('1'));
            Assert.IsFalse(m.EnterDigit('x'));
            Assert.IsTrue(m.EnterDigit('2'));
            Assert.IsFalse(m.EnterDigit('3'));
            Assert.AreEqual("12", m.Entry);
        }

        [Test]
        public void Backspace_RemovesLastDigit_AndIsSafeWhenEmpty()
        {
            var m = new CodeLockModel("1234");
            m.Backspace();
            Type(m, "12");
            m.Backspace();
            Assert.AreEqual("1", m.Entry);
        }

        [Test]
        public void Solved_StaysSolved_AndIgnoresFurtherInput()
        {
            var m = new CodeLockModel("1234");
            Type(m, "1234");
            m.Submit();
            Assert.IsFalse(m.EnterDigit('9'));
            m.Clear();
            Assert.AreEqual(SubmitResult.Correct, m.Submit());
            Assert.IsTrue(m.IsSolved);
        }

        [Test]
        public void RestoreSolved_RoundTrips()
        {
            var m = new CodeLockModel("1234");
            m.RestoreSolved(true);
            Assert.IsTrue(m.IsSolved);
            m.RestoreSolved(false);
            Assert.IsFalse(m.IsSolved);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("12a4")]
        public void InvalidConfiguration_IsRejectedExplicitly(string code)
        {
            Assert.IsNotNull(CodeLockModel.ValidateCode(code));
            Assert.Throws<ArgumentException>(() => new CodeLockModel(code));
        }

        [Test]
        public void ValidCode_PassesValidation() =>
            Assert.IsNull(CodeLockModel.ValidateCode("0719"));
    }
}
