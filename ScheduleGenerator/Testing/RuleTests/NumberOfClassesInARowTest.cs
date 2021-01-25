using System;
using NUnit.Framework;

using Domain.ScheduleLib;
using Domain.Rules;


namespace Testing.RuleTests
{
    class NumberOfClassesInARowTest
    {
        [Test]
        public void EvaluateCorrect()
        {
            var rule = new NumberOfClassesInARow();
            var meetingToAdd = MeetingCreator.CreateMeeting("Math 623 Fil 0 4 0 0 FT-202#0");
            var actualPenalty = rule.Evaluate(
                MeetingCreator.StandartLearningPlan, MeetingCreator.StandartRequestion, MeetingCreator.StandartSchedule, meetingToAdd);
            Assert.AreEqual(0, actualPenalty);
        }

        [Test]
        public void EvaluateIncorrect()
        {
            var schedule = new Schedule(new[] {
                MeetingCreator.CreateMeeting("Math 623 Fil 0 0 0 0 FT-202#0"),
                MeetingCreator.CreateMeeting("OOP 623 Fil 0 1 0 0 FT-202#0"),
                MeetingCreator.CreateMeeting("DM 622 Str 0 2 0 0 FT-202#0"),
                MeetingCreator.CreateMeeting("Math 622 Str 0 3 1 0 FT-202#0"),
                MeetingCreator.CreateMeeting("Pyth 622 Str 0 4 0 0 FT-202#0"),
            });
            var rule = new NumberOfClassesInARow();
            var meetingToAdd = MeetingCreator.CreateMeeting("Pyth 622 Str 0 5 0 0 FT-202#0");
            var actualPenalty = rule.Evaluate(
                MeetingCreator.StandartLearningPlan, MeetingCreator.StandartRequestion, schedule, meetingToAdd);
            Assert.AreEqual(rule.UnitPenalty, actualPenalty);
        }
    }
}
