using System;
using System.Diagnostics;
using static Infrastructure.LoggerExtension;

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
                if (IsSolutionBetter(solution, bestSolution))
                {
                    bestSolution = solution;
                    improvementsCount++;
                    bestIteration = iteration;
                    var message = GetImprovementMessage(improvementsCount, iteration, solution);
                    WriteLog(message);
                }
            }

            sw.Stop();

            var finalMessage = GetFinalMessage(sw, iteration, bestSolution, bestIteration, improvementsCount);
            WriteLog(finalMessage);
            return bestSolution;
        }

        private static bool IsSolutionBetter(Solution solution, Solution bestSolution)
        {
            var (schedule, score) = solution;
            var (bestSchedule, bestScore) = bestSolution;
            return schedule.NotUsedMeetings.Count < bestSchedule.NotUsedMeetings.Count
                   || schedule.NotUsedMeetings.Count == bestSchedule.NotUsedMeetings.Count &&
                   score > bestScore;
        }

        private static string GetImprovementMessage(int improvements, int iteration, Solution solution)
        {
            var (schedule, score) = solution;
            return $"{improvements} improvement in {iteration} iteration. " +
                   $"Not Placed: {schedule.NotUsedMeetings.Count} score: {score}";
        }

        private static string GetFinalMessage(Stopwatch sw, int repeats, Solution bestSolution, int bestIteration,
            int improvementsCount)
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