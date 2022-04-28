namespace Domain2.Algorithms.Solvers;

public interface ISolver
{
    IEnumerable<State> GetSolutions(State state);
}