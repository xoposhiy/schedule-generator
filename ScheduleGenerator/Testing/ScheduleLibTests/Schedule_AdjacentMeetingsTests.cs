using System;
using System.Linq;
using Domain;
using NUnit.Framework;
using static Testing.ScheduleLibTests.ObjectMother;

namespace Testing.ScheduleLibTests
{
    [TestFixture]
    public class Schedule_AdjacentMeetingsTests
    {
        [Test]
        public void AddBothMeetings_WhenAddingMeetingHasAdjacentRequirement()
        {
            var requisitionWithAdjacentRequirement = new Requisition(new[]
            {
                new RequisitionItem(OopLab, "ФИИТ-103-2", "вт-пт, 1-2 пара\nвт-пт, 5-6 пара", OopTeacher1),
                new RequisitionItem(OopSeminar, "ФИИТ-103-2", "вт-пт, 1-2 пара\nвт-пт, 5-6 пара", OopTeacher1)
            });

            var schedule = new Schedule(requisitionWithAdjacentRequirement, ClassRooms);
            var seminarWithAdjacentRequirement =
                schedule.GetMeetingsToAdd().First(m => m.MeetingType == MeetingType.Seminar);

            schedule.AddMeeting(seminarWithAdjacentRequirement);
            var actualMeetings = schedule.Meetings.ToList();

            Assert.AreEqual(0, schedule.NotUsedMeetings.Count);
            Assert.AreEqual(2, actualMeetings.Count);
            AssertAdjacentTimeSlots(actualMeetings[0], actualMeetings[1]);
        }

        [Test]
        public void AddBothMeetings_WhenAddingMeetingWantedToBeAdjacentByOtherMeeting()
        {
            var requisitionWithAdjacentRequirement = new Requisition(new[]
            {
                new RequisitionItem(OopLab, "ФИИТ-103-2", "вт-пт, 1-2 пара\nвт-пт, 5-6 пара", OopTeacher1),
                new RequisitionItem(OopSeminar, "ФИИТ-103-2", "вт-пт, 1-2 пара\nвт-пт, 5-6 пара", OopTeacher1)
            });

            var schedule = new Schedule(requisitionWithAdjacentRequirement, ClassRooms);
            var lab = schedule.GetMeetingsToAdd().First(m => m.MeetingType == MeetingType.ComputerLab);

            schedule.AddMeeting(lab);
            var actualMeetings = schedule.Meetings.ToList();

            Assert.AreEqual(0, schedule.NotUsedMeetings.Count);
            Assert.AreEqual(2, actualMeetings.Count);
            AssertAdjacentTimeSlots(actualMeetings[0], actualMeetings[1]);
        }

        [Test]
        public void AddMeeting_SetsMeetingTimeAcceptableToBothMeetings()
        {
            var requisitionWithAdjacentRequirement = new Requisition(new[]
            {
                new RequisitionItem(OopLab, "ФИИТ-102", "пн 1-6 пара\nвт 1-2 пара\nср 1-6 пара",
                    OopTeacher1),
                new RequisitionItem(OopSeminar, "ФИИТ-102", "вт 5-6 пара\nср 3 пара\nчт 1-6 пара",
                    OopTeacher1)
            });

            var schedule = new Schedule(requisitionWithAdjacentRequirement, ClassRooms);

            var daysOfWeeks = schedule.GetMeetingsToAdd().Select(m => m.MeetingTime!.Day);
            Assert.That(daysOfWeeks, Has.All.EqualTo(DayOfWeek.Wednesday), "сдвоенная пара влазит только в среду");

            var meeting = schedule.GetMeetingsToAdd().First();
            schedule.AddMeeting(meeting);

            var actualMeetings = schedule.Meetings.ToList();
            AssertAdjacentTimeSlots(actualMeetings[0], actualMeetings[1]);
        }

        [Test]
        public void RemoveMeeting_RemovesBothAdjacentMeetings()
        {
            var requisitionWithAdjacentRequirement = new Requisition(new[]
            {
                new RequisitionItem(OopLab, "ФИИТ-102", "пн 1-6 пара\nвт 1-2 пара\nср 1-6 пара",
                    OopTeacher1),
                new RequisitionItem(OopSeminar, "ФИИТ-102", "вт 5-6 пара\nср 3 пара\nчт 1-6 пара",
                    OopTeacher1)
            });

            var schedule = new Schedule(requisitionWithAdjacentRequirement, ClassRooms);

            var meeting = schedule.GetMeetingsToAdd().First();
            schedule.AddMeeting(meeting);
            schedule.RemoveMeeting(meeting);

            Assert.That(schedule.Meetings, Is.Empty);
            Assert.That(schedule.NotUsedMeetings, Has.Count.EqualTo(2));
        }

        private void AssertAdjacentTimeSlots(Meeting meeting1, Meeting meeting2)
        {
            Assert.That(meeting1.MeetingTime!.Day, Is.EqualTo(meeting2.MeetingTime!.Day));
            var timeDelta = meeting1.MeetingTime!.TimeSlotIndex - meeting2.MeetingTime!.TimeSlotIndex;
            Assert.That(timeDelta, Is.EqualTo(1).Or.EqualTo(-1));
        }
    }
}