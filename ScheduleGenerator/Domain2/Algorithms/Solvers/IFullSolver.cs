using CommonInfrastructure;

namespace Domain2.Algorithms.Solvers;

public abstract class FullSolver : ISolver, IStepSolver
{
    //TODO add time limit
    public virtual IEnumerable<(State schedule, double score)> GetSolutions(State problem, Countdown countdown)
    {
        var copy = problem.Copy();
        var name = this.GetType().Name;
        var cumulativeScore = 0d;
        while (copy.NotPlacedMeetings.Count != 0)
        {
            var solution = GetNextSteps(copy).Last();
            cumulativeScore += solution.Score;
            copy.PlaceMeeting(solution.Meeting);
            Console.WriteLine($"{name} Placing {solution}");
        }

        yield return (copy, cumulativeScore);
    }

    public abstract IEnumerable<SolutionStep> GetNextSteps(State state);
}