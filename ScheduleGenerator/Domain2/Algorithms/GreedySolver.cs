namespace Domain2.Algorithms;

public class GreedySolver : ISolver
{
    private readonly IEstimator estimator;

    public GreedySolver(IEstimator estimator)
    {
        this.estimator = estimator;
    }

    public IEnumerable<Solution> GetSolutions(State problem)
    {
        var bestScore = double.MinValue;
        foreach (var meeting in problem.GetAllPossibleVariants())
        {
            var score = estimator.EstimateMeeting(problem, meeting);
            if (score < bestScore) continue;
            bestScore = score;
            yield return new(meeting, score);
        }
    }
}