using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Algorithms;
using Domain.Estimators;
using Domain.ScheduleLib;
using NUnit.Framework;
using static Testing.ScheduleLibTests.ObjectMother;

namespace Testing.ScheduleLibTests
{
    [TestFixture]
    public class RequisitionTests
    {
        [Test]
        public void TestGroupMaskInit()
        {
            var requisition = new Requisition(new[]
            {
                new RequisitionItem(OopLab, "ФИИТ-103-2", "вт-пт, 1-2 пара\nвт-пт, 5-6 пара", OopTeacher)
            });

            var schedule = new Schedule(requisition, ClassRooms);
            var meetingsToAdd = schedule.GetMeetingsToAdd();

            //TODO: check everything is working
        }
    }
}