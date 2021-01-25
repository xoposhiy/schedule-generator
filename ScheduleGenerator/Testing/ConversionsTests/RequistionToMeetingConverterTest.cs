﻿using System;
using System.Linq;
using Domain.Conversions;
using Domain.ScheduleLib;
using NUnit.Framework;

namespace Testing.ConversionsTests
{
    class RequistionToMeetingConverterTest
    {
        [Test]
        public void ConvertRequistionToMeetingWithoutTimeCorrect()
        {
            var learningPlanItem = new LearningPlanItem("FT-202", new Discipline("OOP"), MeetingType.Seminar, GroupSize.FullGroup, 2);
            var requisition = new Requisition(
                learningPlanItem,
                new[] { new GroupRequisition(new[] { new GroupsChoice(new[] { new MeetingGroup("FT-202", GroupPart.FullGroup) }) }) },
                "150",
                2,
                new[] { new MeetingTimeRequesition(new[] { new MeetingTime(DayOfWeek.Tuesday, 2) }) },
                new Teacher("Pavel Egorov"),
                WeekType.Any);
            var meetings = RequistionToMeetingConverter.ConvertRequistionToMeetingWithoutTime(requisition);
            Assert.AreEqual(2, meetings.Count);
            var actualMeeting = meetings.First();
            Assert.AreEqual("OOP", actualMeeting.Discipline.Name);
        }
    }
}
