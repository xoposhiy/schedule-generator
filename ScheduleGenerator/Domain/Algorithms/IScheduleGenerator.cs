using Domain.Rules;
using Domain.ScheduleLib;

namespace Domain.Algorithms
{
    public interface IScheduleGenerator
    {
        public Schedule MakeSchedule(LearningPlan learningPlan, MeetingEvaluator evaluator, Requisition requisition);
    }
}
