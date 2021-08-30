using System.Linq;
using Domain.Conversions;
using Domain.ScheduleLib;

namespace Domain.Estimators
{
    public class CombinedEstimator : IEstimator
    {
        private readonly IEstimator[] childEstimators;
        
        public double Weight { get; }

        public CombinedEstimator(params IEstimator[] childEstimators)
        {
            Weight = 1;
            this.childEstimators = childEstimators;
        }
        

        public double Estimate(Schedule schedule, Meeting meetingToAdd)
        {
            return childEstimators.Sum(e => e.Estimate(schedule, meetingToAdd));
        }

        public double Estimate(Schedule schedule)
        {
            return childEstimators.Sum(e => e.Estimate(schedule) * e.Weight);
        }
    }
}