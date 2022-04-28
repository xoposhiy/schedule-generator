namespace Domain2.Algorithms.Solvers;

public interface IStepSolver
{
    IEnumerable<SolutionStep> GetNextSteps(State state);
}