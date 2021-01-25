using System;
using System.Collections.Generic;
using Domain.ScheduleLib;
using Domain.Algorithms;
using Domain.Rules;
using NuGet.Frameworks;
using NUnit.Framework;

namespace Testing.AlgorithmTests
{
    class GreedyGeneratorTest
    {
        [Test]
        public void Test()
        {
            var learningPlanItem =
                new LearningPlanItem("A", new Discipline("X"), MeetingType.Lecture, GroupSize.FullGroup, 1);
            var learningPlan = new LearningPlan(new LearningPlanItem[] {learningPlanItem});
            var groupsChoices = new GroupsChoice[]
                {new GroupsChoice(new MeetingGroup[] {new MeetingGroup("A", GroupPart.FullGroup)})};
            var groupRequisition = new GroupRequisition[]{new GroupRequisition(groupsChoices)};
            var meetingTimeRequisitions = new MeetingTimeRequisition[]
                {new MeetingTimeRequisition(new MeetingTime[] {new MeetingTime(DayOfWeek.Friday, 5)})};
            var requisition = new Requisition(new RequisitionItem[]
            {
                new RequisitionItem(learningPlanItem, groupRequisition, "", 1, meetingTimeRequisitions, new Teacher("1"), WeekType.Any)
            });

            var generator = new GreedyScheduleGenerator();
            var evaluator = new MeetingEvaluator(new IRule[0]);
            var schedule = generator.MakeSchedule(learningPlan, evaluator, requisition);

            Assert.True(schedule.Meetings.Length == 1);
            var actualMeeting = schedule.Meetings[0];

            var expectedMeeting = new Meeting(new Discipline("X"), MeetingType.Lecture,
                    new MeetingGroup[] {new MeetingGroup("A", GroupPart.FullGroup)})
            {
                MeetingTime = new MeetingTime(DayOfWeek.Friday, 5),
                Teacher = new Teacher("1"),
                Location = "",
                WeekType = WeekType.Any,
            };
            Assert.AreEqual(expectedMeeting.MeetingTime, actualMeeting.MeetingTime);
            Assert.AreEqual(expectedMeeting.Discipline, actualMeeting.Discipline);
            Assert.AreEqual(expectedMeeting.Groups, actualMeeting.Groups);
            Assert.AreEqual(expectedMeeting.Location, actualMeeting.Location);
            Assert.AreEqual(expectedMeeting.MeetingType, actualMeeting.MeetingType);
            Assert.AreEqual(expectedMeeting.Teacher, actualMeeting.Teacher);
            Assert.AreEqual(expectedMeeting.WeekType, actualMeeting.WeekType);
        }
    }
}
