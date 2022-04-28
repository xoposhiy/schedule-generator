namespace Domain2.Algorithms.Estimators;

public class RandomEstimator : IFullEstimator
{
    private readonly Random random = new();

    public double EstimateMeeting(State state, Meeting2 meeting)
    {
        return random.NextDouble();
    }

    public double EstimateSchedule(State state)
    {
        return random.NextDouble();
    }

    public double EstimateStudentSchedule(IEnumerable<Meeting2> meeting)
    {
        return random.NextDouble();
    }
}