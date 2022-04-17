namespace Domain2.Algorithms;

public interface ISolver
{
    IEnumerable<Solution> GetSolutions(State state);
}