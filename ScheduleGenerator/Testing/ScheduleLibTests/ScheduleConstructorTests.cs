using System;
using Domain;
using Domain.MeetingsParts;
using NUnit.Framework;
using static Testing.ObjectMother;

namespace Testing.ScheduleLibTests
{
    [TestFixture]
    public class ScheduleConstructorTests
    {
        [Test]
        public void LinkBasicMeetingsThrowsWhenCannotFindPair()
        {
            var brokenRequisition = new Requisition(new[]
            {
                new RequisitionItem(OopSeminar, "ФИИТ-101", "пн: 6 пара", OopTeacher2)
            });

            // ReSharper disable once ObjectCreationAsStatement
            Assert.Throws(Is.InstanceOf<ArgumentException>(),
                () => new Schedule(brokenRequisition, ClassroomsRequisitions));
        }
    }
}