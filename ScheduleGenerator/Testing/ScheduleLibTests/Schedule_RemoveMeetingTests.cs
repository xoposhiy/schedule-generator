using System;
using System.Collections.Generic;
using System.Linq;
using Domain.ScheduleLib;
using NUnit.Framework;

namespace Testing.ScheduleLibTests
{
    [TestFixture]
    public class Schedule_RemoveMeetingTests
    {
        [Test]
        public void TestDeterminateRemove()
        {
            
            var schedule = new Schedule(ObjectMother.CalculusRequisition, ObjectMother.ClassRooms);
            var addedMeetings = new List<Meeting>();

            for (var i = 0; i < 3; i++)
            {
                var meeting = schedule.GetMeetingsToAdd().First();
                Console.Error.WriteLine(meeting);
                schedule.AddMeeting(meeting);
                addedMeetings.Add(meeting);
            }
            schedule.RemoveMeeting(addedMeetings[1]);
            var missingMeeting = schedule.GetMeetingsToAdd().First();
            Assert.AreEqual(addedMeetings[1].ToString(), missingMeeting.ToString());
        }
    }
}