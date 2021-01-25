using System;
using NUnit.Framework;
using Domain.Rules;

namespace Testing.RuleTests
{
    class NoMoreThanOneMeetingAtTimeForLocationRuleTest
    {
        [Test]
        public void EvaluateCorrect()
        {
            var rule = new NoMoreThanOneMeetingAtTimeForLocationRule();
            var meetingToAdd = MeetingCreator.CreateMeeting("OOP 514 Eg 0 2 0 1 FT-202#1");
            var actualPenalty = rule.Evaluate(
                MeetingCreator.StandartLearningPlan, MeetingCreator.StandartRequestion, MeetingCreator.StandartSchedule, meetingToAdd);
            Assert.AreEqual(0, actualPenalty);
        }

        [Test]
        public void EvaluateIncorrect()
        {
            var rule = new NoMoreThanOneMeetingAtTimeForLocationRule();
            var meetingToAdd = MeetingCreator.CreateMeeting("DM 623 Fil 0 3 0 0 FT-203#0");
            var actualPenalty = rule.Evaluate(
                MeetingCreator.StandartLearningPlan, MeetingCreator.StandartRequestion, MeetingCreator.StandartSchedule, meetingToAdd);
            Assert.AreEqual(rule.UnitPenalty, actualPenalty);
        }
    }
}
