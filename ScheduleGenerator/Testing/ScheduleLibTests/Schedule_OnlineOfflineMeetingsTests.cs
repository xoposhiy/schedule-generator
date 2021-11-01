using System;
using System.Linq;
using Domain;
using Domain.Enums;
using NUnit.Framework;
using static Testing.ObjectMother;

namespace Testing.ScheduleLibTests
{
    [TestFixture]
    public class Schedule_OnlineOfflineMeetingsTests
    {
        [Test]
        public void TestOnlineLocation()
        {
            var schedule = new Schedule(CalculusRequisition, ClassRooms);

            var lecturePossibleLocations = schedule.GetMeetingsToAdd()
                .Where(m => m.MeetingType == MeetingType.Lecture)
                .Select(m => m.Classroom);
            Assert.That(lecturePossibleLocations, Has.All.EqualTo(null));
        }

        [Test]
        public void TestOnlineOfflineSpace()
        {
            var schedule = new Schedule(CalculusRequisition, ClassRooms);

            var lecture = schedule
                .GetMeetingsToAdd()
                .Where(m => m.MeetingType == MeetingType.Lecture)
                .First(m => m.MeetingTime!.TimeSlot == 1);
            schedule.AddMeeting(lecture);
            Console.Error.WriteLine(lecture);

            foreach (var meeting in schedule.GetMeetingsToAdd().Where(m => m.MeetingType == MeetingType.Seminar))
            {
                Console.Error.WriteLine(meeting);
                Assert.Greater(meeting.MeetingTime!.TimeSlot - lecture.MeetingTime!.TimeSlot, 1);
            }
        }
    }
}