using System;
using System.Linq;
using Domain.ScheduleLib;
using NUnit.Framework;
using static Testing.ScheduleLibTests.ObjectMother;

namespace Testing.ScheduleLibTests
{
    [TestFixture]
    public class Schedule_GetMeetingToAddTests
    {
        [Test]
        public void SetLocationGroupAndTimeToRequiredValues()
        {
            var requisition = new Requisition(new[]
            {
                new RequisitionItem(OsLecture, "ФИИТ-101+ФИИТ-102", "вт, 3 пара", OsTeacher)
            });

            var schedule = new Schedule(requisition, ClassRooms);

            var meeting = schedule.GetMeetingsToAdd().Single();
            Console.Error.WriteLine(meeting);

            Assert.That(meeting.Location, Is.EqualTo("632"));
            Assert.That(meeting.Groups, Is.EqualTo(new[] { Group1, Group2 }));
            Assert.That(meeting.MeetingTime, Is.EqualTo(new MeetingTime(DayOfWeek.Tuesday, 3)));
        }
    }
}