using System.Collections.Generic;
using System.Linq;

namespace Domain.Algorithms.Estimators
{
    public class CombinedEstimator : IEstimator

    {
        private readonly (IEstimator, double)[] childEstimators;

        public CombinedEstimator(params (IEstimator, double)[] childEstimators)
        {
            this.childEstimators = childEstimators;
        }


        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            return childEstimators.Sum(e => e.Item1.Estimate(schedule, meetingToAdd));
        }

        public double Estimate(Schedule schedule, List<string>? logger = null)
        {
            var score = 0d;
            foreach (var (estimator, weight) in childEstimators)
            {
                logger?.Add($"{estimator.GetType().Name}:");
                var estimatedScore = weight * estimator.Estimate(schedule, logger);
                logger?.Add($"Total {estimatedScore}");
                score += estimatedScore;
            }

            return score;
        }
    }
}