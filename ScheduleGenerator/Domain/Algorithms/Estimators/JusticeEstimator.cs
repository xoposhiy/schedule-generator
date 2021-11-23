using Domain.Algorithms.Estimators.GroupsEstimators;
using Infrastructure;

namespace Domain.Algorithms.Estimators
{
    public class JusticeEstimator : IEstimator
    {
        private readonly GroupEstimator[] subEstimators;

        public JusticeEstimator(GroupEstimator[] estimators)
        {
            subEstimators = estimators;
        }
        
        public double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            throw new System.NotImplementedException();
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            // 1. Justice per estimator
            // 2. Summary justice
            foreach (var meetingGroup in schedule.Groups)
            foreach (var estimator in subEstimators)
            {
                var penalty = estimator.GetPenaltyByGroup(meetingGroup, schedule);
                logger?.Log($"{meetingGroup} has {penalty}", -penalty);
            }

            throw new System.NotImplementedException();
        }
    }
}