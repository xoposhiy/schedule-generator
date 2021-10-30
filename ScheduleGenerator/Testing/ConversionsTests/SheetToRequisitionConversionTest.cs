using System;
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

        //TODO make actual test
        [Test]
        public void TimeRequisitionsReadTest()
        {
            var requisitionData = SheetTableReader.ReadRowsFromSheet(Repository, InputRequirementsTestSheetName, 1, 0, 7);
            foreach (var requisitionRow in requisitionData)
            {
                var meetingTimesRaw = requisitionRow[5];
                var weekTypeRaw = requisitionRow[6];
                try
                {
                    var meetingTimeRequisitions =
                        SheetToRequisitionConverter.ParseMeetingTimeRequisitions(meetingTimesRaw);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine(meetingTimesRaw);
                }
                var _ = 1;

            }
        }
    }
}