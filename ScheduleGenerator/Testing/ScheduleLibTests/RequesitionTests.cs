using NUnit.Framework;
using ScheduleLib;

namespace ScheduleLibTests
{
    [TestFixture]
    public class RequesitionTests
    {
        [Test]
        public void TestGroupMaskInit()
        {
            var priority = new GroupRequisition("a + b + c + d");
            Assert.AreEqual(new[] { "a", "b", "c", "d" }, priority.Groups);
        }
    }
}
