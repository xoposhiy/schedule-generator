using Domain.ScheduleLib;

namespace Domain.Algorithms
{
    public interface IScheduleGenerator
    {
        public Schedule MakeSchedule(LearningPlan learningPlan, Requisition requisition);
    }
}
