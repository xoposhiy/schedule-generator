using System;
using System.Diagnostics;

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
            Solution bestSolution = Solver.GetSolution(timeBudget - sw.Elapsed);
            var repeats = 1;
            while (sw.Elapsed < timeBudget)
            {
                var solution = Solver.GetSolution(timeBudget - sw.Elapsed);
                if (solution.Score > bestSolution.Score)
                    bestSolution = solution;
                repeats++;
            }

            Console.WriteLine();
            Console.WriteLine($"Repeater {sw.Elapsed}");
            Console.WriteLine($"Total repeats {repeats}");
            Console.WriteLine();
            Console.WriteLine($"Not placed meetings: {bestSolution.Schedule.NotUsedMeetings.Count}");
            Console.WriteLine($"Placed meetings: {bestSolution.Schedule.Meetings.Count}");
            Console.WriteLine();

            return bestSolution;
        }
    }
}