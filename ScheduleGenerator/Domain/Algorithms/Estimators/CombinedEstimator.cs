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

        public double Estimate(Schedule schedule)
        {
            return childEstimators.Sum(e => e.Item1.Estimate(schedule) * e.Item2);
        }
    }
}