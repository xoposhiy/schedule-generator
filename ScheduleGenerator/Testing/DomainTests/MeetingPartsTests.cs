using NUnit.Framework;
using static Testing.ObjectMother;

namespace Testing.DomainTests
{
    [TestFixture]
    public class MeetingPartsTests
    {
        [Test]
        public void PlanItemToStringTest()
        {
            var planItemString = CalculusLecture.ToString();
            Assert.AreEqual("ФИИТ-1, МатАн, Lecture, FullGroup, 1,5, 0", planItemString);
        }

        [Test]
        public void RequisitionToString()
        {
            var requisitionString = NecessaryMeetings.Items[0].ToString();
            Assert.AreEqual("ФИИТ-202 FullGroup ФИИТ-201 FullGroup, Физрук", requisitionString);
        }

        [Test]
        public void GroupRequisitionToString()
        {
            var groupRequisition = NecessaryMeetings.Items[0].GroupPriorities[0].ToString();
            Assert.AreEqual("ФИИТ-202 FullGroup ФИИТ-201 FullGroup", groupRequisition);
        }
    }
}