using System.Collections.Generic;
using System.Linq;
using Domain.Algorithms.Estimators.GroupsEstimators;
using Domain.MeetingsParts;
using Infrastructure;

namespace Domain.Algorithms.Estimators
{
    public class JusticeEstimator : IEstimator
    {
        private readonly GroupEstimator[] subEstimators;

        public JusticeEstimator(params GroupEstimator[] estimators)
        {
            subEstimators = estimators;
        }

        public double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            throw new System.NotImplementedException();
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var totalInjustice = 0d;
            foreach (var estimator in subEstimators)
            {
                var penalties = new Dictionary<MeetingGroup, double>();
                using var subLogger = logger?.GetChild(estimator.Name, childTopN: schedule.Groups.Count);
                foreach (var meetingGroup in schedule.Groups)
                {
                    var scoreByGroup = estimator.GetScoreByGroup(meetingGroup, schedule);
                    penalties[meetingGroup] = scoreByGroup;
                    subLogger?.Log($"{meetingGroup} has {scoreByGroup:F3} score", scoreByGroup);
                }

                foreach (var byGroupSet in penalties.GroupBy(p => p.Key.GetGroupSet()))
                {
                    var groupSetPenalties = byGroupSet.Select(p => p.Value).ToList();
                    var min = groupSetPenalties.Min();
                    var max = groupSetPenalties.Max();
                    // var justice = max - min;
                    var justice = groupSetPenalties.Sum(p => max - p);
                    logger?.Log($"{estimator.Name} has {-justice} injustice in {byGroupSet.Key}", -justice);
                    totalInjustice -= justice;
                }
            }

            return totalInjustice;
        }
    }
}