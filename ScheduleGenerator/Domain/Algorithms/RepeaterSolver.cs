using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Domain.Algorithms
{
    public class RepeaterSolver : ISolver
    {
        public readonly ISolver Solver;
        
        public RepeaterSolver(ISolver solver)
        {
            Solver = solver;
        }
        
        public Solution GetSolution(TimeSpan timeBudget)
        {
            var sw = Stopwatch.StartNew();
            Solution bestSolution = null;
            while (sw.Elapsed < timeBudget)
            {
                var solution = Solver.GetSolution(timeBudget - sw.Elapsed);
                if (bestSolution is null || solution.Score > bestSolution.Score)
                    bestSolution = solution;
            }

            return bestSolution;
        }
    }
}