using CommonInfrastructure;

namespace Domain2.Algorithms.Solvers;

public abstract class FullSolver : ISolver, IStepSolver
{
    public virtual IEnumerable<(State schedule, double score)> GetSolutions(State problem, Countdown countdown)
    {
        var name = GetType().Name;
        var cumulativeScore = 0d;
        while (problem.NotPlacedMeetings.Count != 0)
        {
            var solution = GetNextSteps(problem).Last();
            cumulativeScore += solution.Score;
            problem = problem.AddMeeting(solution.Meeting);
            Console.WriteLine($"{name} Placing {solution}");
        }

        yield return (problem, cumulativeScore);
    }

    public abstract IEnumerable<SolutionStep> GetNextSteps(State state);
}