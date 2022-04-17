namespace Domain2.Algorithms;

public class RandomEstimator : IEstimator
{
    private readonly Random random = new();

    public double EstimateMeeting(State state, Meeting2 meeting)
    {
        return random.NextDouble();
    }
}