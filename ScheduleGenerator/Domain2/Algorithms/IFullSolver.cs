namespace Domain2.Algorithms;

public abstract class FullSolver : ISolver, IStepSolver
{
    //TODO add score to ISolver
    //TODO add time limit
    public virtual IEnumerable<State> GetSolutions(State problem)
    {
        var copy = problem.Copy();
        var name = this.GetType().Name;
        while (copy.NotPlacedMeetings.Count != 0)
        {
            var solution = GetNextSteps(copy).Last();
            copy.PlaceMeeting(solution.Meeting);
            Console.WriteLine($"{name} Placing {solution}");
        }

        yield return copy;
    }

    public abstract IEnumerable<SolutionStep> GetNextSteps(State state);
}