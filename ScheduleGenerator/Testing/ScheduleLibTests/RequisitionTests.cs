using System;
using System.Diagnostics;
using System.Linq;
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
                new RequisitionItem(OopLab, "ФИИТ-103-2", "вт-пт, 1-2 пара\nвт-пт, 5-6 пара", OopTeacher),
                new RequisitionItem(OopSeminar, "ФИИТ-103-2", "вт-пт, 1-2 пара\nвт-пт, 5-6 пара", OopTeacher)
            });

            var schedule = new Schedule(requisition, ClassRooms);
            var meetingToAdd = schedule.GetMeetingsToAdd().First();
            
            schedule.AddMeeting(meetingToAdd);
            Console.WriteLine(meetingToAdd);
            
            Assert.AreEqual(0, schedule.NotUsedMeetings.Count);
            Assert.AreEqual(2, schedule.Meetings.Count);
            var first = schedule.Meetings.First();
            var second = schedule.Meetings.Last();
            Assert.AreNotEqual(first, second);
            Assert.AreEqual(first.MeetingTime!.Day, second.MeetingTime!.Day);
            var timeDelta = first.MeetingTime.TimeSlotIndex - second.MeetingTime.TimeSlotIndex;
            Assert.Less(Math.Abs(timeDelta), 2);
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