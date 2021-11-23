using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
                foreach (var meetingGroup in schedule.Groups)
                {
                    var penalty = estimator.GetPenaltyByGroup(meetingGroup, schedule);
                    penalties[meetingGroup] = penalty;
                    //logger?.Log($"{meetingGroup} has {penalty} penalty with {estimator}", -penalty);
                }

                var justice = penalties.Max(p => p.Value) -
                              penalties.Min(p => p.Value);
                LoggerExtension.WriteLog($"{estimator} has {-justice} injustice");
                totalInjustice -= justice;
            }

            return totalInjustice;
        }
    }
}