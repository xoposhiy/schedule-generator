using System.Linq;
using Infrastructure;

namespace Domain.Algorithms.Estimators
{
    public class CombinedEstimator : IEstimator

    {
        private readonly (IEstimator, double)[] childEstimators;

        public CombinedEstimator(params (IEstimator, double)[] childEstimators)
        {
            this.childEstimators = childEstimators;
        }


        public double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd)
        {
            return childEstimators.Sum(e => e.Item1.EstimateMeetingToAdd(schedule, meetingToAdd) * e.Item2);
        }

        public double Estimate(Schedule schedule, ILogger? logger = null)
        {
            var score = 0d;
            foreach (var (estimator, weight) in childEstimators)
            {
                using var childLogger = logger?.GetChild(estimator.GetType().Name, weight);
                var estimatedScore = weight * estimator.Estimate(schedule, childLogger);
                score += estimatedScore;
            }

            return score;
        }
    }
}