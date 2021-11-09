using System;
using System.Linq;
using Domain.Conversions;
using Infrastructure.SheetPatterns;
using NUnit.Framework;
using static Infrastructure.SheetConstants;


namespace Testing.ConversionsTests
{
    [TestFixture]
    internal class SheetToRequisitionConversionTest
    {
        [Test]
        public void RequisitionsReadAmountTest()
        {
            var (requisitionItems, learningPlan, classrooms) = SheetToRequisitionConverter.ConvertToRequisitions(
                Repository,
                InputRequirementsSheetName,
                LearningPlanSheetName,
                ClassroomsSheetName);

            Assert.AreEqual(63, requisitionItems.Count);
            Assert.AreEqual(23, learningPlan.Items.Length);
            Assert.AreEqual(25, classrooms.Count);
        }
        
        [TestCase("пн,вт: 1,4 пары",
            "Monday 1\nMonday 4\nTuesday 1\nTuesday 4")]
        [TestCase("вт: 1-5 пара\nпт: 3-6 пара",
            "Tuesday 1\nTuesday 2\nTuesday 3\nTuesday 4\nTuesday 5\n\nFriday 3\nFriday 4\nFriday 5\nFriday 6")]
        [TestCase("",
            "Monday 1\nMonday 2\nMonday 3\nMonday 4\nMonday 5\nMonday 6\nTuesday 1\nTuesday 2\nTuesday 3\nTuesday 4\nTuesday 5\nTuesday 6\nWednesday 1\nWednesday 2\nWednesday 3\nWednesday 4\nWednesday 5\nWednesday 6\nThursday 1\nThursday 2\nThursday 3\nThursday 4\nThursday 5\nThursday 6\nFriday 1\nFriday 2\nFriday 3\nFriday 4\nFriday 5\nFriday 6\nSaturday 1\nSaturday 2\nSaturday 3\nSaturday 4\nSaturday 5\nSaturday 6")]
        [TestCase("пн: 1-3 пара \nпт: 4,6 пара",
            "Monday 1\nMonday 2\nMonday 3\n\nFriday 4\nFriday 6")]
        [TestCase("пн,пт: 3-4 пра; вт:2 арап",
            "Monday 3\nMonday 4\nFriday 3\nFriday 4\nTuesday 2")]
        [TestCase("пн, ср, пт: 2-5 пара",
            "Monday 2\nMonday 3\nMonday 4\nMonday 5\nWednesday 2\nWednesday 3\nWednesday 4\nWednesday 5\nFriday 2\nFriday 3\nFriday 4\nFriday 5")]
        [TestCase("пн-пт: 3,4 пара",
            "Monday 3\nMonday 4\nTuesday 3\nTuesday 4\nWednesday 3\nWednesday 4\nThursday 3\nThursday 4\nFriday 3\nFriday 4")]
        [TestCase("вт,ср,пт: 1-4 пара",
            "Tuesday 1\nTuesday 2\nTuesday 3\nTuesday 4\nWednesday 1\nWednesday 2\nWednesday 3\nWednesday 4\nFriday 1\nFriday 2\nFriday 3\nFriday 4")]
        [TestCase("пн, ср, пт-сб: 2-5 абвгдеёж",
            "Monday 2\nMonday 3\nMonday 4\nMonday 5\nWednesday 2\nWednesday 3\nWednesday 4\nWednesday 5\nFriday 2\nFriday 3\nFriday 4\nFriday 5\nSaturday 2\nSaturday 3\nSaturday 4\nSaturday 5")]
        [TestCase("пн: 1-3 пара\nпт: 4-6 пара",
            "Monday 1\nMonday 2\nMonday 3\n\nFriday 4\nFriday 5\nFriday 6")]
        public void TimeRequisitionsParseTest(string rawTimeRequisition, string expected)
        {
            var meetingTimeRequisitions = SheetToRequisitionConverter
                .ParseMeetingTimeRequisitions(rawTimeRequisition)
                .Select(r =>
                    string.Join('\n',
                        r.MeetingTimeChoices
                                .Select(m => $"{m.Day.ToString()} {m.TimeSlot.ToString()}")));
            var actual = string.Join("\n\n", meetingTimeRequisitions);
            //Console.WriteLine(actual.Replace("\n", "\\n"));
            Assert.AreEqual(expected, actual);
        }

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
                () => SheetToRequisitionConverter.ParseMeetingTimeRequisitions(rawTimeRequisition));
        }
    }
}