using System;
using NUnit.Framework;
using Domain.ScheduleLib;
using Domain.Rules;


namespace Testing.RuleTests
{
    class LecturerHasPracticeWithSameFlowTest
    {
        private static LearningPlanItem standardLearningPlanItem = new LearningPlanItem(
            "FT-202", new Discipline("OOP"), MeetingType.Lecture, GroupSize.HalfGroup, 2);

        private static Requisition standardRequisition = new Requisition(new RequisitionItem[]
        {
            new RequisitionItem(
                standardLearningPlanItem,
                new[]
                {
                    new GroupRequisition(new[] {new GroupsChoice(new[] {new MeetingGroup("FT-202", GroupPart.Part1)})})
                },
                "514",
                2,
                new[] {new MeetingTimeRequisition(new[] {new MeetingTime(DayOfWeek.Tuesday, 2)})},
                new Teacher("Eg"),
                WeekType.Any)
        });
        private static Schedule standardSchedule = new Schedule(new[] {
                MeetingCreator.CreateMeeting("Math 623 Fil 0 3 0 0 FT-202#0 FT-201#0 KN-201#2"),
                MeetingCreator.CreateMeeting("DM 622 Str 0 0 0 0 FT-202#0 KN-201#0"),
                MeetingCreator.CreateMeeting("OOP 514 Eg 0 2 0 0 FT-202#1")
            });
        private static LearningPlan standardLearningPlan = new LearningPlan(
            new[]{
                new LearningPlanItem("FT-202", new Discipline("OOP"), MeetingType.Lecture, GroupSize.HalfGroup, 2),
                new LearningPlanItem("FT-202", new Discipline("OOP"), MeetingType.Seminar, GroupSize.HalfGroup, 2)
            });

        [Test]
        public void EvaluateWhenNoPracticesInPlan()
        {
            var learningPlanItems = new[] { new LearningPlanItem("FT-202", new Discipline("OOP"), MeetingType.Lecture, GroupSize.HalfGroup, 2) };
            var learningPlan = new LearningPlan(learningPlanItems);
            var testSchedule = new Schedule(new Meeting[] { });
            var meetingToAdd = MeetingCreator.CreateMeeting("OOP 514 Eg 0 2 0 1 FT-202#1");

            var lecturerHasPracticeWithSameFlowRule = new LecturerHasPracticeWithSameFlow();
            var actualPenalty = lecturerHasPracticeWithSameFlowRule.Evaluate(learningPlan, standardRequisition, testSchedule, meetingToAdd);
            Assert.AreEqual(0, actualPenalty);
        }

        [Test]
        public void EvaluateCorrect()
        {
            var meetingToAdd = MeetingCreator.CreateMeeting("OOP 514 Eg 0 2 0 1 FT-202#1");

            var lecturerHasPracticeWithSameFlowRule = new LecturerHasPracticeWithSameFlow();
            var actualPenalty = lecturerHasPracticeWithSameFlowRule.Evaluate(standardLearningPlan, standardRequisition, standardSchedule, meetingToAdd);
            Assert.AreEqual(0, actualPenalty);
        }

        [Test]
        public void EvaluateNotPractice()
        {
            var meetingToAdd = MeetingCreator.CreateMeeting("OOP 514 Eg 0 2 0 0 FT-202#1");

            var lecturerHasPracticeWithSameFlowRule = new LecturerHasPracticeWithSameFlow();
            var actualPenalty = lecturerHasPracticeWithSameFlowRule.Evaluate(standardLearningPlan, standardRequisition, standardSchedule, meetingToAdd);
            Assert.AreEqual(0, actualPenalty);
        }

        [Test]
        public void EvaluateIncorrect()
        {
            var meetingToAdd = MeetingCreator.CreateMeeting("OOP 514 Eg 0 2 0 2 KN-201#1");

            var lecturerHasPracticeWithSameFlowRule = new LecturerHasPracticeWithSameFlow();
            var actualPenalty = lecturerHasPracticeWithSameFlowRule.Evaluate(standardLearningPlan, standardRequisition, standardSchedule, meetingToAdd);
            Assert.AreEqual(lecturerHasPracticeWithSameFlowRule.UnitPenalty, actualPenalty);
        }
    }
}
