using CommonInfrastructure;

namespace Domain2.Algorithms.Solvers;

public class RepeaterSolver : ISolver
{
    private ISolver innerSolver;
    
    public RepeaterSolver(ISolver innerSolver)
    {
        this.innerSolver = innerSolver;
    }
    
    public IEnumerable<(State schedule, double score)> GetSolutions(State problem, Countdown countdown)
    {
        (State, double) bestSolution = (null, Double.MinValue);
        while (!countdown.IsFinished())
        {
            var solution = innerSolver.GetSolutions(problem, countdown).Last();
            if (solution.score > bestSolution.Item2)
            {
                bestSolution = solution;
            }
            yield return solution;
        }
        yield return bestSolution;
    }
}