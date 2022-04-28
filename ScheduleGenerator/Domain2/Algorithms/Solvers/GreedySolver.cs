using Domain2.Algorithms.Estimators;

namespace Domain2.Algorithms.Solvers;

public class GreedySolver : FullSolver
{
    private readonly IEstimator estimator;

    public GreedySolver(IEstimator estimator)
    {
        this.estimator = estimator;
    }

    public override IEnumerable<SolutionStep> GetNextSteps(State problem)
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