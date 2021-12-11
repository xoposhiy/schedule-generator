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
            var schedule = new Schedule(CalculusRequisition, ClassroomsRequisitions);

            var lecturePossibleLocations = schedule.GetMeetingsToAdd()
                .Where(m => m.MeetingType == MeetingType.Lecture)
                .Select(m => m.Classroom);
            Assert.That(lecturePossibleLocations, Has.All.EqualTo(null));
        }

        [Test]
        public void TestOnlineOfflineSpace()
        {
            var schedule = new Schedule(CalculusRequisition, ClassroomsRequisitions);

            var lecture = schedule
                .GetMeetingsToAdd()
                .Where(m => m.MeetingType == MeetingType.Lecture)
                .First(m => m.MeetingTime!.TimeSlot == 1);
            schedule.AddMeeting(lecture);

            foreach (var meeting in schedule.GetMeetingsToAdd().Where(m => m.MeetingType == MeetingType.Seminar))
            {
                Assert.Greater(meeting.MeetingTime!.TimeSlot - lecture.MeetingTime!.TimeSlot, 1);
            }
        }
    }
}