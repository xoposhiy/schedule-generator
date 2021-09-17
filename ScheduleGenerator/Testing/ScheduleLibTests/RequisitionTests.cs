using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        public void TestConnectedMeetings()
        {
            var requisition = new Requisition(new[]
            {
                new RequisitionItem(OopSeminar, "ФИИТ-103-2", "вт-пт, 1-2 пара\nвт-пт, 5-6 пара", OopTeacher)
            });

            var schedule = new Schedule(requisition, ClassRooms);
            var meetingsToAdd = schedule.GetMeetingsToAdd();

            //TODO: check everything is working
        }

        [Test]
        public void TestSingleSolution()
        {
            var requisition = new Requisition(new[]
            {
                new RequisitionItem(CalculusLecture, "ФИИТ-101 + ФИИТ-102", "пн, 1-3 пара\nпт, 4-6 пара",
                    CalculusTeacher)
            });

            var schedule = new Schedule(requisition, ClassRooms);

            var meetingsToAdd = schedule.GetMeetingsToAdd();

            var expectedGroups = new[] {Group1, Group2};

            foreach (var meeting in meetingsToAdd)
            {
                Console.Error.WriteLine(meeting);
                Assert.AreEqual("632", meeting.Location);
                Assert.AreEqual(expectedGroups, meeting.Groups);
            }
        }
    }
}