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

        [Test]
        public void WrongTimeRequisitionFormatShouldNotWork()
        {
            var requisitionData = SheetTableReader
                .ReadRowsFromSheet(Repository, InputRequirementsTestSheetName, 1, 0, 7)
                .Skip(10);
            foreach (var requisitionRow in requisitionData)
            {
                var meetingTimesRaw = requisitionRow[5];
                Assert.Throws(Is.InstanceOf<Exception>(),
                    () => SheetToRequisitionConverter.ParseMeetingTimeRequisitions(meetingTimesRaw));
            }
        }
    }
}