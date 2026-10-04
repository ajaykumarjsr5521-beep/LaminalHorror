using NUnit.Framework;
using NocturneAnnex.Controls;

namespace NocturneAnnex.Tests.EditMode
{
    public class TouchControlsVisibilityTests
    {
        [TestCase(false, false, false)]   // Windows without a touchscreen
        [TestCase(false, true, true)]     // Windows touch laptop
        [TestCase(true, false, true)]     // phone, even before the first touch is seen
        [TestCase(true, true, true)]
        public void ShouldShow_FollowsPlatformAndTouchscreen(bool mobile, bool touchscreen, bool expected) =>
            Assert.AreEqual(expected, TouchControlsVisibility.ShouldShow(mobile, touchscreen));
    }
}
