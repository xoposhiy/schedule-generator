using System;
using NUnit.Framework;
using Domain.Rules;

namespace Testing.RuleTests
{
    public class NoMoreThanOneMeetingAtTimeForGroupRuleTests
    {
        [Test]
        public void EvaluateCorrect()
        {
            var rule = new NoMoreThanOneMeetingAtTimeForGroupRule();
            var meetingToAdd = MeetingCreator.CreateMeeting("OOP 514 Eg 0 2 0 1 FT-202#1");
            var actualPenalty = rule.Evaluate(
                MeetingCreator.StandartLearningPlan, MeetingCreator.StandartRequestion, MeetingCreator.StandartSchedule, meetingToAdd);
            Assert.AreEqual(0, actualPenalty);
        }

        [Test]
        public void EvaluateIncorrect()
        {
            var rule = new NoMoreThanOneMeetingAtTimeForGroupRule();
            var meetingToAdd = MeetingCreator.CreateMeeting("OOP 622 Str 0 0 0 0 FT-201#0");
            var actualPenalty = rule.Evaluate(
                MeetingCreator.StandartLearningPlan, MeetingCreator.StandartRequestion, MeetingCreator.StandartSchedule, meetingToAdd);
            Assert.AreEqual(rule.UnitPenalty, actualPenalty);
        }
    }
}
