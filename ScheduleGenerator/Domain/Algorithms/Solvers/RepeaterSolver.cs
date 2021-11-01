using System;
using System.Diagnostics;

namespace Domain.Algorithms.Solvers
{
    public class RepeaterSolver : ISolver
    {
        private readonly ISolver solver;

        public RepeaterSolver(ISolver solver)
        {
            this.solver = solver;
        }

        public Solution GetSolution(TimeSpan timeBudget)
        {
            var sw = Stopwatch.StartNew();
            Solution bestSolution = solver.GetSolution(timeBudget - sw.Elapsed);
            var repeats = 1;
            while (sw.Elapsed < timeBudget)
            {
                repeats++;
                var solution = solver.GetSolution(timeBudget - sw.Elapsed);
                if (solution.Schedule.NotUsedMeetings.Count > bestSolution.Schedule.NotUsedMeetings.Count) continue;
                if (!(solution.Score > bestSolution.Score)) continue;
                bestSolution = solution;
            }

            sw.Stop();

            Console.WriteLine(GetFinalMessage(sw, repeats, bestSolution));

            return bestSolution;
        }

        private static string GetFinalMessage(Stopwatch sw, int repeats, Solution bestSolution)
        {
            var (schedule, score) = bestSolution;
            var lines = new[]
            {
                "",
                $"Repeater: {sw.Elapsed}",
                $"Total repeats: {repeats}",
                $"Mean elapsed: {sw.Elapsed / repeats}",
                "",
                $"Not placed meetings: {schedule.NotUsedMeetings.Count}",
                $"Placed meetings: {schedule.Meetings.Count}",
                $"Score: {score}",
                ""
            };
            return string.Join("\n", lines);
        }
    }
}