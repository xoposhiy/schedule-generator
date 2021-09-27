using System;
using System.Linq;
using Domain.ScheduleLib;
using NUnit.Framework;

namespace Testing.ScheduleLibTests
{
    [TestFixture]
    public class Schedule_OnlineOfflineMeetingsTests
    {
        [Test]
        public void TestOnlineLocation()
        {
            var requisition = new Requisition(new[]
            {
                new RequisitionItem(ObjectMother.CalculusLecture, "ФИИТ-101", "пн 1-6 пара",
                    ObjectMother.CalculusTeacher, 1, isOnline: true),
                new RequisitionItem(ObjectMother.CalculusSeminar, "ФИИТ-101", "пн 1-6 пара",
                    ObjectMother.CalculusTeacher),
            });

            var schedule = new Schedule(requisition, ObjectMother.ClassRooms);
            foreach (var meeting in schedule.GetMeetingsToAdd())
            {
                Console.Error.WriteLine(meeting);
            }

            var lecturePossibleLocations = schedule.GetMeetingsToAdd()
                .Where(m => m.MeetingType == MeetingType.Lecture)
                .Select(m => m.Location);
            Assert.That(lecturePossibleLocations, Has.All.EqualTo(Meeting.OnlineLocationName));
        }

        [Test]
        public void TestOnlineOfflineGap()
        {
            var requisition = new Requisition(new[]
            {
                new RequisitionItem(ObjectMother.CalculusLecture, "ФИИТ-101", "пн 1-2 пара",
                    ObjectMother.CalculusTeacher, 1, isOnline: true),
                new RequisitionItem(ObjectMother.CalculusSeminar, "ФИИТ-101", "пн 1-6 пара",
                    ObjectMother.CalculusTeacher),
            });
            var schedule = new Schedule(requisition, ObjectMother.ClassRooms);

            var lecture = schedule
                .GetMeetingsToAdd()
                .Where(m => m.MeetingType == MeetingType.Lecture)
                .First(m => m.MeetingTime!.TimeSlotIndex == 1);
            schedule.AddMeeting(lecture);
            Console.Error.WriteLine(lecture);

            foreach (var meeting in schedule.GetMeetingsToAdd().Where(m => m.MeetingType == MeetingType.Seminar))
            {
                Console.Error.WriteLine(meeting);
                Assert.Greater(meeting.MeetingTime!.TimeSlotIndex - lecture.MeetingTime!.TimeSlotIndex, 1);
            }
        }
    }
}