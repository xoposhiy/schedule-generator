﻿using System.Collections.Generic;
using System.Linq;
using Domain;
using NUnit.Framework;
using static Testing.ObjectMother;

namespace Testing.ScheduleLibTests
{
    [TestFixture]
    public class Schedule_RemoveMeetingTests
    {
        [Test]
        public void TestDeterminateRemove()
        {
            var schedule = new Schedule(CalculusRequisition, ClassroomsRequisitions);
            var addedMeetings = new List<Meeting>();

            for (var i = 0; i < 3; i++)
            {
                var meeting = schedule.GetMeetingsToAdd().First();
                schedule.AddMeeting(meeting, true);
                addedMeetings.Add(meeting);
            }

            schedule.RemoveMeeting(addedMeetings[1], true);
            var missingMeeting = schedule.GetMeetingsToAdd().First();
            Assert.AreEqual(addedMeetings[1].ToString(), missingMeeting.ToString());
        }
    }
}