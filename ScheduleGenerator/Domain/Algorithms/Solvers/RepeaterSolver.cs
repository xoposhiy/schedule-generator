using System;
using System.Diagnostics;
using static Infrastructure.LoggerExtension;
using static Domain.DomainExtensions;

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
            var justiceEstimator = GetDefaultJusticeEstimator();
            var sw = Stopwatch.StartNew();

            Solution bestSolution = solver.GetSolution(timeBudget - sw.Elapsed);
            // 1. score + 2 * justice
            // // 1. score + 0.5 * justice
            // 2. (score, justice)
            var bestJustice = justiceEstimator.Estimate(bestSolution.Schedule);

            var scoreSum = bestSolution.Score;
            var iteration = 1;
            var improvementsCount = 0;
            var bestIteration = 0;
            while (sw.Elapsed < timeBudget)
            {
                iteration++;
                var solution = solver.GetSolution(timeBudget - sw.Elapsed);
                var justice = justiceEstimator.Estimate(solution.Schedule);
                // score += 00 * justice;
                // var solution = new Solution(schedule, score);
                scoreSum += solution.Score;
                if (IsSolutionBetter(solution, bestSolution, bestJustice, justice))
                {
                    WriteLog($"justice: {justice}");
                    bestSolution = solution;
                    improvementsCount++;
                    bestIteration = iteration;
                    var message = GetImprovementMessage(improvementsCount, iteration, solution);
                    WriteLog(message);
                    // WriteLog($"Score + Justice = {solution.Score + justice}");
                }
            }

            sw.Stop();

            var finalMessage = GetFinalMessage(sw, iteration, bestSolution, bestIteration, improvementsCount, scoreSum);
            WriteLog(finalMessage);
            return bestSolution;
        }

        private static bool IsSolutionBetter(Solution solution, Solution bestSolution, double bestJustice,
            double justice)
        {
            var (schedule, score) = solution;
            var (bestSchedule, bestScore) = bestSolution;
            if (schedule.NotUsedMeetings.Count > bestSchedule.NotUsedMeetings.Count) return false;
            if (schedule.NotUsedMeetings.Count < bestSchedule.NotUsedMeetings.Count)
            {
                WriteLog(
                    $"Improvement reason: Not placed meetings count {bestSchedule.NotUsedMeetings.Count} -> {schedule.NotUsedMeetings.Count}");
                return true;
            }

            if (score < bestScore) return false;
            if (score > bestScore)
            {
                WriteLog($"Improvement reason: Score better {bestScore} -> {score}");
                return true;
            }

            if (justice > bestJustice)
            {
                WriteLog($"Improvement reason: Justice better {bestJustice} -> {justice}");
                return true;
            }

            return false;
        }

        private static string GetImprovementMessage(int improvements, int iteration, Solution solution)
        {
            var (schedule, score) = solution;
            return $"{improvements} improvement in {iteration} iteration. " +
                   $"Not Placed: {schedule.NotUsedMeetings.Count} score: {score}";
        }

        private static string GetFinalMessage(Stopwatch sw, int repeats, Solution bestSolution, int bestIteration,
            int improvementsCount, double scoreSum)
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
                $"Mean Score: {scoreSum / repeats}",
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