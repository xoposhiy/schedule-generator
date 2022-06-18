using CommonInfrastructure;
using Domain2.Algorithms.Estimators;

namespace Domain2.Algorithms.Solvers;

public class GreedySolver : FullSolver
{
    private readonly IMeetingEstimator estimator;
    public readonly int randomTopN = 1;
    private Random random = new Random();

    public GreedySolver(IMeetingEstimator estimator, int randomTopN = 1)
    {
        this.estimator = estimator;
        this.randomTopN = randomTopN;
    }

    public override IEnumerable<(State schedule, double score)> GetSolutions(State problem, Countdown countdown)
    {
        var name = GetType().Name;
        var cumulativeScore = 0d;
        while (problem.NotPlacedMeetings.Count != 0)
        {
            var solutions = GetNextSteps(problem).ToList();
            var solution =
                solutions[
                    Math.Min(Math.Max(solutions.Count - randomTopN, 0) + random.Next(randomTopN), solutions.Count - 1)];
            cumulativeScore += solution.Score;
            problem = problem.AddMeeting(solution.Meeting);
            //Console.WriteLine($"{name} Placing {solution}");
        }

        yield return (problem, cumulativeScore);
    }

    public override IEnumerable<SolutionStep> GetNextSteps(State problem)
    {
        var bestScore = double.MinValue;
        foreach (var meeting in problem.GetPossibleVariants())
        {
            var score = estimator.EstimateMeeting(problem, meeting);
            if (score < bestScore) continue;
            bestScore = score;
            yield return new(meeting, score);
        }
    }
}