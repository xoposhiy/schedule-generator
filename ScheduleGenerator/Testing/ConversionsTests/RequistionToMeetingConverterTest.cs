using NUnit.Framework;

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;


using Domain.ScheduleLib;
using Domain.Conversions;


namespace ConversionsTests
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
                new Teacher("Pavel Egorov"));
            var meetings = RequistionToMeetingConverter.ConvertRequistionToMeetingWithoutTime(requisition);
            Assert.AreEqual(1, meetings.Count);
            Assert.AreEqual(2, meetings[0].Count);
            var actualMeeting = meetings[0].First();
            Assert.AreEqual("OOP", actualMeeting.Discipline.Name);
            Assert.AreEqual(1, actualMeeting.Groups.Length);
            Assert.AreEqual("FT-202", actualMeeting.Groups[0].GroupName);
            Assert.AreEqual(GroupPart.FullGroup, actualMeeting.Groups[0].GroupPart);
            Assert.AreEqual("150", actualMeeting.Location);
        }
    }
}
