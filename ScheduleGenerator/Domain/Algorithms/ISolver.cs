using System;

namespace Domain.Algorithms
{
    public interface ISolver
    {
        /// <returns>Решение</returns>
        Solution GetSolution(TimeSpan timeBudget);

        Solution Solve(Schedule schedule);
    }

    public record Solution(Schedule Schedule, double Score);
}