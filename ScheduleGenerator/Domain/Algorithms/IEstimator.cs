using System.Collections.Generic;

namespace Domain.Algorithms
{
    public interface IEstimator
    {
        double Estimate(Schedule schedule, Meeting meetingToAdd);
        double Estimate(Schedule schedule, List<string>? logger = null);
    }
}