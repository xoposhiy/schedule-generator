using System;
using Domain.ScheduleLib;
using System.Collections.Generic;


namespace Domain.Rules
{
    public class MeetingEvaluator
    {
        private IEnumerable<IRule> rules;
        public MeetingEvaluator(IEnumerable<IRule> rules)
        {
            this.rules = rules;
        }

        public double Evaluate(LearningPlan learningPlan, Requisition requisition, Schedule schedule, Meeting meetingToAdd)
        {
            var totalPenalty = 0d;
            foreach (var rule in rules)
            {
                totalPenalty += rule.Evaluate(learningPlan, requisition, schedule, meetingToAdd);
            }

            return totalPenalty;
        }
    }
}
