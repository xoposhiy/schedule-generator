using System;
using System.Linq;
using Domain;
using NUnit.Framework;
using static Testing.ObjectMother;

namespace Testing.ScheduleLibTests
{
    [TestFixture]
    public class Schedule_GetMeetingToAddTests
    {
        [Test]
        public void SetGroupClassroomAndTimeToRequiredValues()
        {
            var schedule = new Schedule(OsRequisition, ClassRooms);

            var meeting = schedule.GetMeetingsToAdd().Single();

            Assert.That(meeting.Classroom, Is.EqualTo("632"));
            Assert.That(meeting.Groups, Is.EqualTo(new[] {Group1, Group2}));
            Assert.That(meeting.MeetingTime, Is.EqualTo(new MeetingTime(DayOfWeek.Tuesday, 3)));
        }
    }
}