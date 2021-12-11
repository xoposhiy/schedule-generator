using System;
using System.Linq;
using Domain;
using Domain.MeetingsParts;
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
            var schedule = new Schedule(OsRequisition, ClassRoomsWithSpecs);

            var meeting = schedule.GetMeetingsToAdd().Single();

            Assert.That(meeting.Classroom, Is.EqualTo("622"));
            Assert.That(meeting.GroupsChoice!.Groups, Is.EqualTo(new[] {Group1, Group2}));
            Assert.That(meeting.MeetingTime, Is.EqualTo(new MeetingTime(DayOfWeek.Tuesday, 3)));
        }
    }
}