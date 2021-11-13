using System;
using System.Linq;
using NUnit.Framework;
using static Domain.Conversions.SheetToRequisitionConverter;
using static Infrastructure.SheetConstants;


namespace Testing.ConversionsTests
{
    [TestFixture]
    internal class SheetToRequisitionConversionTest
    {
        [Test]
        public void RequisitionsReadAmountTest()
        {
            var (requisitionItems, learningPlan, classrooms) = ConvertToRequisitions(
                Repository,
                InputRequirementsSheetName,
                LearningPlanSheetName,
                ClassroomsSheetName);

            Assert.AreEqual(63, requisitionItems.Count);
            Assert.AreEqual(23, learningPlan.Items.Length);
            Assert.AreEqual(25, classrooms.Count);
        }

        
        [TestCase("пн, ср, пт-сб: 2-5 абвгдеёж",
            "Mo2, Mo3, Mo4, Mo5, We2, We3, We4, We5, Fr2, Fr3, Fr4, Fr5, Sa2, Sa3, Sa4, Sa5")]
        [TestCase("пн: 1-3 пара\nпт: 4-6 пара", "Mo1, Mo2, Mo3;Fr4, Fr5, Fr6")]
        [TestCase("пн:1\nвт:5", "Mo1;Tu5")]
        [TestCase("пн:5;вт:3", "Mo5, Tu3")]
        [TestCase("пт-сб: 5-6", "Fr5, Fr6, Sa5, Sa6")]
        [TestCase("пн: 1-2,4-5", "Mo1, Mo2, Mo4, Mo5")]
        [TestCase("чт: 1,2,3", "Th1, Th2, Th3")]
        [TestCase("пн,вт,ср:1 парам", "Mo1, Tu1, We1")]
        [TestCase("ср: 2, 3-5", "We2, We3, We4, We5")]
        [TestCase("чт: 2-4", "Th2, Th3, Th4")]
        [TestCase("сб: 2, 3", "Sa2, Sa3")]
        [TestCase("пн, ср-чт: 6", "Mo6, We6, Th6")]
        [TestCase("чт-сб: 4 абгдеёж", "Th4, Fr4, Sa4")]
        [TestCase("пн, вт: 3 пра", "Mo3, Tu3")]
        [TestCase("пн:1 пара", "Mo1")]
        [TestCase("",
            "Mo1, Mo2, Mo3, Mo4, Mo5, Mo6, Tu1, Tu2, Tu3, Tu4, Tu5, Tu6, We1, We2, We3, We4, We5, We6, Th1, Th2, Th3, Th4, Th5, Th6, Fr1, Fr2, Fr3, Fr4, Fr5, Fr6, Sa1, Sa2, Sa3, Sa4, Sa5, Sa6")]
        public void TimeRequisitionsParseTest(string rawTimeRequisition, string expected)
        {
            var meetingTimeRequisitions = ParseMeetingTimeRequisitions(rawTimeRequisition)
                .Select(r => r.MeetingTimeChoices)
                .Select(t =>
                    string.Join(", ", t.Select(m => $"{m.Day.ToString()[..2]}{m.TimeSlot}")));
            var actual = string.Join(";", meetingTimeRequisitions);
            Console.WriteLine(actual);
            Assert.AreEqual(expected, actual);
        }

        
        [TestCase("вт: 7")]
        [TestCase("пн: 0")]
        [TestCase("erdtcyvbnm")]
        [TestCase("пг, сб: 2 пара")]
        [TestCase("1-2 пара")]
        [TestCase("вт, 1-5 пара\nпт, 3-6 пара")]
        [TestCase("пн-пт, 3-5 пара")]
        [TestCase("вт-пт 2-6 пара")]
        [TestCase("пн-пт; 3-4 пара")]
        [TestCase("пн-пт; 3-4 пара")]
        [TestCase("пара")]
        [TestCase("пн-чт")]
        public void WrongTimeRequisitionFormatShouldNotWork(string rawTimeRequisition)
        {
            Assert.Throws(Is.InstanceOf<Exception>(),
                () => ParseMeetingTimeRequisitions(rawTimeRequisition));
        }
    }
}