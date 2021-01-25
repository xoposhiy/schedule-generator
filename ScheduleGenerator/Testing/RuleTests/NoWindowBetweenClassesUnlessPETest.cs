using System;
using NUnit.Framework;
using Domain.Rules;

namespace Testing.RuleTests
{
    class NoWindowBetweenClassesUnlessPETest
    {
        [Test]
        public void EvaluateCorrect()
        {
            var rule = new NoWindowBetweenClassesUnlessPE();
            var meetingToAdd = MeetingCreator.CreateMeeting("Math 623 Fil 0 4 0 0 FT-202#0");
            var actualPenalty = rule.Evaluate(
                MeetingCreator.StandartLearningPlan, MeetingCreator.StandartRequestion, MeetingCreator.StandartSchedule, meetingToAdd);
            Assert.AreEqual(0, actualPenalty);
        }

        [Test]
        public void EvaluateIncorrect()
        {
            var rule = new NoWindowBetweenClassesUnlessPE();
            var meetingToAdd = MeetingCreator.CreateMeeting("Math 623 Fil 0 5 0 0 FT-202#0");
            var actualPenalty = rule.Evaluate(
                MeetingCreator.StandartLearningPlan, MeetingCreator.StandartRequestion, MeetingCreator.StandartSchedule, meetingToAdd);
            Assert.AreEqual(rule.UnitPenalty, actualPenalty);
        }

    }
}
