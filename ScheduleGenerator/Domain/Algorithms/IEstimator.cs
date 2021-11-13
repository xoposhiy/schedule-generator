using Infrastructure;

namespace Domain.Algorithms
{
    public interface IEstimator
    {
        double EstimateMeetingToAdd(Schedule schedule, Meeting meetingToAdd);
        double Estimate(Schedule schedule, ILogger? logger = null);
    }
}