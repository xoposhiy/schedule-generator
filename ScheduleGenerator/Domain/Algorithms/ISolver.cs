using System;

namespace Domain.Algorithms
{
    public interface ISolver
    {
        /// <returns>Решение</returns>
        Solution GetSolution(TimeSpan timeBudget);

        Solution Solve(Schedule schedule, TimeSpan timeBudget);
    }

    public record Solution(Schedule Schedule, double Score);
}