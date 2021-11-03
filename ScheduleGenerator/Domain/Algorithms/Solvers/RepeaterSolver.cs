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
            var iteration = 1;
            var improvementsCount = 0;
            var bestIteration = 0;
            while (sw.Elapsed < timeBudget)
            {
                iteration++;
                var solution = solver.GetSolution(timeBudget - sw.Elapsed);
                if (solution.Schedule.NotUsedMeetings.Count < bestSolution.Schedule.NotUsedMeetings.Count
                    || solution.Schedule.NotUsedMeetings.Count == bestSolution.Schedule.NotUsedMeetings.Count &&
                    solution.Score > bestSolution.Score)
                {
                    bestSolution = solution;
                    improvementsCount++;
                    bestIteration = iteration;
                    Console.WriteLine($"{improvementsCount} of {iteration}. Not Placed: {solution.Schedule.NotUsedMeetings.Count} score: {solution.Score}");
                }
            }

            sw.Stop();

            Console.WriteLine(GetFinalMessage(sw, iteration, bestSolution, bestIteration, improvementsCount));

            return bestSolution;
        }

        private static string GetFinalMessage(Stopwatch sw, int repeats, Solution bestSolution, int bestIteration, int improvementsCount)
        {
            var (schedule, score) = bestSolution;
            var lines = new[]
            {
                "",
                $"Repeater: {sw.Elapsed}",
                $"Total repeats: {repeats}",
                $"Improvements count: {improvementsCount}",
                $"Best solution iteration: {bestIteration}",
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