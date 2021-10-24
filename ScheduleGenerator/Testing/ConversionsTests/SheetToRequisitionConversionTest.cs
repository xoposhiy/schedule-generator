using Domain.Conversions;
using NUnit.Framework;
using static Infrastructure.SheetConstants;

namespace Testing.ConversionsTests
{
    [TestFixture]
    internal class SheetToRequisitionConversionTest
    {
        [Test]
        public void RequisitionsReadTest()
        {
            var repo = Repository;

            var (requisitionItems, learningPlan, dictionary) = SheetToRequisitionConverter.ConvertToRequisitions(
                repo,
                InputRequirementsSheetName,
                LearningPlanSheetName,
                ClassroomsSheetName);

            Assert.AreEqual(63, requisitionItems.Count);
            Assert.AreEqual(24, learningPlan.Items.Length);
            Assert.AreEqual(24, dictionary.Count);
        }
    }
}