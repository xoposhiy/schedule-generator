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
                new RequisitionItem(OopLab, "ФИИТ-103-2", "вт-пт, 1-2 пара\nвт-пт, 5-6 пара", OopTeacher1),
                new RequisitionItem(OopSeminar, "ФИИТ-103-2", "вт-пт, 1-2 пара\nвт-пт, 5-6 пара", OopTeacher1)
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
            Assert.AreEqual(Math.Abs(timeDelta), 1);
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
        
        [Test]
        public void TestPairedRequisitionWhenOnlyOneDayPossible()
        {
            var requisition = new Requisition(new[]
            {
                new RequisitionItem(OopLab, "ФИИТ-102", "пн 1-6 пара\nвт 1-2 пара\nср 1-6 пара",
                    OopTeacher1),
                new RequisitionItem(OopSeminar, "ФИИТ-102", "вт 5-6 пара\nср 3 пара\nчт 1-6 пара",
                OopTeacher1)
            });

            var schedule = new Schedule(requisition, ClassRooms);
            
            Assert.True(schedule.GetMeetingsToAdd().All(m=>m.MeetingTime.Day == DayOfWeek.Wednesday), "сдвоенная пара влазит только в среду");
        }

        [Test]
        public void TestPairedRequisitionIsAdjacent()
        {
            var requisition = new Requisition(new[]
            {
                new RequisitionItem(OopLab, "ФИИТ-102", "пн 1-6 пара\nвт 1-2 пара\nср 1-6 пара",
                    OopTeacher1),
                new RequisitionItem(OopSeminar, "ФИИТ-102", "вт 5-6 пара\nср 3 пара\nчт 1-6 пара",
                    OopTeacher1)
            });

            var schedule = new Schedule(requisition, ClassRooms);

            schedule.AddMeeting(schedule.GetMeetingsToAdd()
                .First(m => m.MeetingTime == new MeetingTime(DayOfWeek.Wednesday, 3)));
            foreach (var meeting in schedule.Meetings)
            {
                Console.Error.WriteLine(meeting);
            }

            Assert.False(schedule.GetMeetingsToAdd().Any());
            Assert.AreEqual(2, schedule.Meetings.Count);
            Assert.True(schedule.Meetings.All(m => m.MeetingTime.Day == DayOfWeek.Wednesday));
            
            var timeDelta = schedule.Meetings.First().MeetingTime.TimeSlotIndex -
                            schedule.Meetings.Last().MeetingTime.TimeSlotIndex;
            Assert.AreEqual(Math.Abs(timeDelta), 1);
        }
        
        [Test]
        public void TestOnlineLocation()
        {
            var requisition = new Requisition(new[]
            {
                new RequisitionItem(CalculusLecture, "ФИИТ-101", "пн 1-6 пара",
                    CalculusTeacher, 1, WeekType.All, true),
                new RequisitionItem(CalculusSeminar, "ФИИТ-101", "пн 1-6 пара",
                    CalculusTeacher, 1, WeekType.All, false),
            });

            var schedule = new Schedule(requisition, ClassRooms);
            foreach (var meeting in schedule.GetMeetingsToAdd())
            {
                Console.Error.WriteLine(meeting);
            }
            Assert.True(schedule.GetMeetingsToAdd().Any(m=>m.Location!="632"));
            //TODO: check that some meetings are located specifically in online
        }
    }
}