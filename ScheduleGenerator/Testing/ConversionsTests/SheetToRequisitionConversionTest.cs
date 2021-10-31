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
    }
}