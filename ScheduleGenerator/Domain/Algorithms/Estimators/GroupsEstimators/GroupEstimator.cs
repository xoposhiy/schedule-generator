using Domain.MeetingsParts;
using Infrastructure;

namespace Domain.Algorithms.Estimators.GroupsEstimators
{
    public abstract class GroupEstimator : IEstimator
    {
        public string Name => GetType().Name;
        public abstract double GetMaxPenalty(Schedule schedule);
        public abstract double GetScoreByGroup(MeetingGroup group, Schedule schedule, ILogger? logger = null);
        public abstract double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd);

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var score = 0d;

            foreach (var group in schedule.Groups)
                score += GetScoreByGroup(group, schedule, logger);

            return score;
        }
    }
}