using NUnit.Framework;
using NocturneAnnex.Core;

namespace NocturneAnnex.Tests.EditMode
{
    public class BuildInfoTests
    {
        [Test]
        public void ProductName_IsNotEmpty() => Assert.IsNotEmpty(BuildInfo.ProductName);

        [Test]
        public void SaveSchemaVersion_IsNumeric() => Assert.IsTrue(int.TryParse(BuildInfo.SaveSchemaVersion, out _));
    }
}
