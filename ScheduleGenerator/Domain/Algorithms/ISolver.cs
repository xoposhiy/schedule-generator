using System;
using System.Collections.Generic;

namespace Domain.Algorithms
{
    public interface ISolver
    {
        /// <returns>Решение</returns>
        Solution GetSolution(TimeSpan timeBudget);
    }

    public record Solution(Schedule Schedule, double Score);
}