namespace Domain2.Algorithms;

public abstract class FullSolver : ISolver, IStepSolver
{
    public virtual IEnumerable<State> GetSolutions(State problem)
    {
        var copy = problem.Copy();
        while (copy.NotPlacedMeetings.Count != 0)
        {
            var solution = GetNextSteps(copy).Last();
            copy.PlaceMeeting(solution.Meeting);
            Console.WriteLine($"Place {solution}");
        }

        yield return copy;
    }

    public abstract IEnumerable<SolutionStep> GetNextSteps(State state);
}