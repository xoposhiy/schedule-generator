namespace Domain2.Algorithms;

public interface IStepSolver
{
    IEnumerable<SolutionStep> GetNextSteps(State state);
}