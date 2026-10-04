using NUnit.Framework;
using NocturneAnnex.UI;

namespace NocturneAnnex.Tests.EditMode
{
    public class KeypadFormatterTests
    {
        [TestCase("", 4, "_ _ _ _")]
        [TestCase("07", 4, "0 7 _ _")]
        [TestCase("0719", 4, "0 7 1 9")]
        [TestCase(null, 3, "_ _ _")]
        [TestCase("12", 2, "1 2")]
        [TestCase("123456", 4, "1 2 3 4")]   // never shows more digits than slots
        [TestCase("", 0, "")]
        public void Format_ShowsDigitsThenEmptySlots(string entry, int length, string expected) =>
            Assert.AreEqual(expected, KeypadFormatter.Format(entry, length));
    }
}
