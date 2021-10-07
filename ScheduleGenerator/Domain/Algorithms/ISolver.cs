using System;
using System.Collections.Generic;

namespace Domain.Algorithms
{
    public interface ISolver
    {
        /// <returns>Последовательность все более хороших решений</returns>
        IEnumerable<Solution> GetSolution(TimeSpan timeBudget);
    }

    public record Solution(Schedule Schedule, double Score);
}