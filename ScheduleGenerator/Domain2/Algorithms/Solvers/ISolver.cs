using CommonInfrastructure;

namespace Domain2.Algorithms.Solvers;

public interface ISolver
{
    IEnumerable<(State schedule, double score)> GetSolutions(State problem, Countdown countdown);
}