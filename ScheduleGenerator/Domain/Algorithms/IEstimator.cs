namespace Domain.Algorithms
{
    public interface IEstimator
    {
        double Estimate(Schedule schedule, Meeting meetingToAdd);
        double Estimate(Schedule schedule);
    }
}