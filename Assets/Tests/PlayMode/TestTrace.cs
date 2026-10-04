using NUnit.Framework;
using NUnit.Framework.Interfaces;

[assembly: NocturneAnnex.Tests.PlayMode.TraceTests]

namespace NocturneAnnex.Tests.PlayMode
{
    /// <summary>Writes one line per test start and end to Builds/trace.txt, so a hung run can be traced to the test that never finished.</summary>
    [System.AttributeUsage(System.AttributeTargets.Assembly)]
    public class TraceTestsAttribute : System.Attribute, ITestAction
    {
        public ActionTargets Targets => ActionTargets.Test;

        public void BeforeTest(ITest test) => Write("START " + test.FullName);

        public void AfterTest(ITest test) => Write("END   " + test.FullName);

        static void Write(string line) =>
            System.IO.File.AppendAllText("Builds/trace.txt", System.DateTime.Now.ToString("HH:mm:ss.fff") + " " + line + System.Environment.NewLine);
    }
}
