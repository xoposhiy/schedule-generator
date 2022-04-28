namespace Domain2.Algorithms.Estimators;

public class RandomEstimator : IEstimator
{
    private readonly Random random = new();

    public double EstimateMeeting(State state, Meeting2 meeting)
    {
        return random.NextDouble();
    }
}