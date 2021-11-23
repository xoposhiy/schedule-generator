using Domain.MeetingsParts;
using Infrastructure;

namespace Domain.Algorithms.Estimators.GroupsEstimators
{
    public abstract class GroupEstimator : IEstimator
    {
        public abstract double GetMaxPenalty(Schedule schedule);
        public abstract double GetPenaltyByGroup(MeetingGroup group, Schedule schedule, ILogger? logger = null);
        public abstract double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd);

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var penalty = 0d;
            var maxPenalty = GetMaxPenalty(schedule);

            foreach (var group in schedule.Groups)
                penalty += GetPenaltyByGroup(group, schedule, logger);

            return -penalty / maxPenalty;
            ;
        }
    }
}