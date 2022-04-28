namespace Domain2.Algorithms;

public interface ISolver
{
    IEnumerable<State> GetSolutions(State state);
}