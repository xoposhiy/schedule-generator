using System;
using System.Linq;
using Domain;
using NUnit.Framework;
using static Testing.ScheduleLibTests.ObjectMother;

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
                .Select(m => m.Location);
            Assert.That(lecturePossibleLocations, Has.All.EqualTo(Meeting.OnlineLocationName));
        }

        [Test]
        public void TestOnlineOfflineSpace()
        {
            var schedule = new Schedule(CalculusRequisition, ClassRooms);

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